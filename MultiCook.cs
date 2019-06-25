using System;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImageCook
{
    public class MultiCook
    {
        public class Rect
        {
            public int X { get; protected set; }
            public int Y { get; protected set; }
            public int Width { get; protected set; }
            public int Height { get; protected set; }

            public Rect(int x, int y, int width, int height)
            {
                X = x; Y = y; Width = width; Height = height;
            }

            public Rect(Rect rect) : this(rect.X, rect.Y, rect.Width, rect.Height) { }

            public Rect Expand(int x, int y)
            {
                Rect rect = new Rect(this);
                rect.X -= x; rect.Width += x;
                rect.Y -= y; rect.Height += y;
                return rect;
            }
        }

        public class Image
        {
            public int Width { get; protected set; }
            public int Height { get; protected set; }

            public double[,] R { get; protected set; }
            public double[,] G { get; protected set; }
            public double[,] B { get; protected set; }
            public double[,] A { get; protected set; }

            public Image(int width, int height)
            {
                Width = width; Height = height;
                R = new double[width, height];
                G = new double[width, height];
                B = new double[width, height];
                A = new double[width, height];
            }

            public Image(Bitmap bitmap, Rect rect) : this(rect.Width, rect.Height)
            {
                Rectangle region = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData bitmapData = bitmap.LockBits(region, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                IntPtr imgPtr = bitmapData.Scan0;

                for (int x = 0; x < Width; x++)
                {
                    if (rect.X + x >= bitmap.Width) continue;
                    for (int y = 0; y < Height; y++)
                    {
                        if (rect.Y + y >= bitmap.Height) continue;

                        A[x, y] = Marshal.ReadByte(imgPtr, (rect.X + x + (rect.Y + y) * bitmap.Width) * 4 + 3);
                        R[x, y] = Marshal.ReadByte(imgPtr, (rect.X + x + (rect.Y + y) * bitmap.Width) * 4 + 2);
                        G[x, y] = Marshal.ReadByte(imgPtr, (rect.X + x + (rect.Y + y) * bitmap.Width) * 4 + 1);
                        B[x, y] = Marshal.ReadByte(imgPtr, (rect.X + x + (rect.Y + y) * bitmap.Width) * 4 + 0);
                    }
                }

                bitmap.UnlockBits(bitmapData);
            }

            public Image(Bitmap bitmap) : this(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height)) { }

            public Image(Image image, Rect rect) : this(rect.Width, rect.Height)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (rect.X + x >= image.Width) continue;
                    for (int y = 0; y < Height; y++)
                    {
                        if (rect.Y + y >= image.Height) continue;
                        R[x, y] = image.R[rect.X + x, rect.Y + y];
                        G[x, y] = image.G[rect.X + x, rect.Y + y];
                        B[x, y] = image.B[rect.X + x, rect.Y + y];
                        A[x, y] = image.A[rect.X + x, rect.Y + y];
                    }
                }
            }

            public Image(Image image) : this(image, new Rect(0, 0, image.Width, image.Height)) { }

            public Image Get(Rect rect)
            {
                return new Image(this, rect);
            }

            public Image Get(int x, int y, int width, int height)
            {
                return Get(new Rect(x, y, width, height));
            }

            public void Set(int u, int v, Image image)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (u + x >= Width) continue;
                    for (int y = 0; y < image.Height; y++)
                    {
                        if (v + y >= Height) continue;
                        R[u + x, v + y] = image.R[x, y];
                        G[u + x, v + y] = image.G[x, y];
                        B[u + x, v + y] = image.B[x, y];
                        A[u + x, v + y] = image.A[x, y];
                    }
                }
            }

            protected double FilterToPixel(double[,] channel, int u, int v, int[,] filter, double scale)
            {
                int fil_w = filter.GetLength(0) / 2;
                int fil_h = filter.GetLength(1) / 2;
                double pixel = 0;
                for (int x = 0; x < filter.GetLength(0); x++)
                    for (int y = 0; y < filter.GetLength(1); y++)
                        pixel += channel[u - fil_w + x, v - fil_h + y] * filter[x, y];
                return pixel / scale;
            }

            public Image ApplyFilter(int[,] filter, double scale)
            {
                Image image = new Image(this);
                int fil_w = filter.GetLength(0) / 2;
                int fil_h = filter.GetLength(1) / 2;
                for (int x = fil_w; x < Width - fil_w; x++)
                    for (int y = fil_h; y < Height - fil_h; y++)
                    {
                        image.R[x, y] = FilterToPixel(R, x, y, filter, scale);
                        image.G[x, y] = FilterToPixel(G, x, y, filter, scale);
                        image.B[x, y] = FilterToPixel(B, x, y, filter, scale);
                    }
                return image;
            }

            public void HistogramEqualization()
            {
                List<double> list = new List<double>();
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                    {
                        list.Add(R[i, j]);
                        list.Add(G[i, j]);
                        list.Add(B[i, j]);
                    }
                list.Sort();
                double max = Math.Max(list[0], list[list.Count - 1]);
                double min = Math.Min(list[0], list[list.Count - 1]);
                double scale = 255 / (max - min);
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                    {
                        R[i, j] = (R[i, j] - min) * scale;
                        G[i, j] = (G[i, j] - min) * scale;
                        B[i, j] = (B[i, j] - min) * scale;
                    }
            }

            public Bitmap ToBitmap()
            {
                Bitmap bitmap = new Bitmap(Width, Height);
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        bitmap.SetPixel(x, y,
                            Color.FromArgb(
                                Math.Max(0, Math.Min(Convert.ToInt32(A[x, y]), 255)),
                                Math.Max(0, Math.Min(Convert.ToInt32(R[x, y]), 255)),
                                Math.Max(0, Math.Min(Convert.ToInt32(G[x, y]), 255)),
                                Math.Max(0, Math.Min(Convert.ToInt32(B[x, y]), 255))
                            )
                        );
                    }
                }
                return bitmap;
            }
        }

        public class TaskPool
        {
            public class Task
            {
                public Rect Origional { get; protected set; }
                public Image Input { get; protected set; }
                public Image Output { get; protected set; }

                public Task(Rect origional, Image input)
                {
                    Origional = new Rect(origional);
                    Input = new Image(input);
                    Output = null;
                }

                public Task(int x, int y, int width, int height, Image input)
                {
                    Origional = new Rect(x, y, width, height);
                    Input = new Image(input);
                    Output = null;
                }

                public void Work(int[,] filter, double scale)
                {
                    Output = Input.ApplyFilter(filter, scale);
                }
            }

            protected Stack<Task> tasks;
            private readonly object _lock = new object();

            public int Count {
                get
                {
                    try
                    {
                        Monitor.Enter(_lock);
                        return tasks.Count;
                    }
                    finally
                    {
                        Monitor.Exit(_lock);
                    }
                }
            }
            
            public TaskPool()
            {
                tasks = new Stack<Task>();
            }

            public void AddTask(Task task)
            {
                Monitor.Enter(_lock);
                tasks.Push(task);
                Monitor.Exit(_lock);
            }

            public bool HasTask()
            {
                try
                {
                    Monitor.Enter(_lock);
                    return tasks.Count != 0;
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }

            public Task PullTask()
            {
                try
                {
                    Monitor.Enter(_lock);
                    if (tasks.Count == 0) return null;
                    return tasks.Pop();
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }
        }
    }
}
