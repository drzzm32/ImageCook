using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace ImageCook
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    templateTable.Controls.Add(new TextBox
                    {
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                        TextAlign = HorizontalAlignment.Center
                    });

        }

        private void srcBox_DoubleClick(object sender, EventArgs e)
        {
            openDialog.ShowDialog();
        }

        private int[,] ParseTemplate(int size)
        {
            int[,] template = new int[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    template[i, j] = 0;

            int r = 0, c = 0;
            foreach (TextBox t in templateTable.Controls)
            {
                if (!int.TryParse(t.Text, out int v))
                    v = 0;

                template[r, c] = v;

                c += 1;
                if (c >= size)
                {
                    r += 1;
                    if (r >= size) break;
                    c = 0;
                }
            }

            return template;
        }

        private void WorkOnThread(ThreadStart start)
        {
            loopBar.Style = ProgressBarStyle.Marquee;

            Thread thread = new Thread(start);
            thread.Start();
            while (thread.IsAlive)
            {
                Refresh();
                Application.DoEvents();
            }

            loopBar.Style = ProgressBarStyle.Continuous;
            loopBar.Value = loopBar.Maximum;
        }

        private void cookBtn_Click(object sender, EventArgs e)
        {
            const int siz_t = 9, siz_h = siz_t / 2;

            if (srcBox.Image == null) return;

            proBar.Value = 0;
            loopBar.Value = 0;
            cookBtn.Enabled = false;
            Refresh(); Application.DoEvents();

            // parse template
            int[,] template = ParseTemplate(siz_t);
            proBar.Value = 5;
            Refresh(); Application.DoEvents();

            // parse scale
            if (!double.TryParse(scaleBox.Text, out double scale))
                scale = 1;
            // get bitmap
            Bitmap bitmap = new Bitmap(srcBox.Image);
            if (bitmap.Width < siz_t || bitmap.Height < siz_t)
            {
                MessageBox.Show("Image is too small!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            proBar.Value = 10;
            Refresh(); Application.DoEvents();

            // convert data
            MultiCook.Image image = new MultiCook.Image(bitmap);
            proBar.Value = 20;
            Refresh(); Application.DoEvents();

            // apply filter
            MultiCook.Image output = new MultiCook.Image(image);
            int start = Environment.TickCount;
            if (etcBox.Text == "single")
            {
                WorkOnThread(() => output = image.ApplyFilter(template, scale));
            }
            else if (etcBox.Text.StartsWith("multi"))
            {
                if (!int.TryParse(etcBox.Text.Substring(5), out int siz_c))
                    siz_c = 128;
                etcBox.Text = "multi" + siz_c;
                Refresh();
                Application.DoEvents();

                MultiCook.TaskPool inputPool = new MultiCook.TaskPool();
                MultiCook.TaskPool outputPool = new MultiCook.TaskPool();

                // task division
                int wid = image.Width - siz_h, hit = image.Height - siz_h;
                for (int x = siz_h; x < wid; x += siz_c)
                {
                    for (int y = siz_h; y < hit; y += siz_c)
                    {
                        MultiCook.Rect rect = new MultiCook.Rect(x, y, siz_c, siz_c);
                        if (x + siz_c >= wid || y + siz_c >= hit)
                            rect = new MultiCook.Rect(x, y, wid - x, hit - y);
                        MultiCook.Image clip = image.Get(rect.Expand(siz_h, siz_h));
                        MultiCook.TaskPool.Task task = new MultiCook.TaskPool.Task(rect, clip);
                        inputPool.AddTask(task);
                    }
                }

                int sumTasks = inputPool.Count;
                loopBar.Maximum = sumTasks;

                // start threads
                ThreadPool.SetMinThreads(Environment.ProcessorCount, 0);
                for (int i = 0; i < Environment.ProcessorCount; i++)
                    ThreadPool.QueueUserWorkItem((obj) =>
                    {
                        while (inputPool.HasTask())
                        {
                            var task = inputPool.PullTask();
                            if (task != null)
                            {
                                task.Work(template, scale);
                                outputPool.AddTask(task);
                            }
                        }
                    });

                // wait for finish
                while (outputPool.Count < sumTasks)
                {
                    loopBar.Value = outputPool.Count;
                    Refresh();
                    Application.DoEvents();
                }
                loopBar.Value = sumTasks;

                // merge result
                while (outputPool.HasTask())
                {
                    var task = outputPool.PullTask();
                    if (task != null)
                    {
                        output.Set(task.Origional.X - siz_h, task.Origional.Y - siz_h, task.Output);
                    }
                }
            }
            int end = Environment.TickCount;
            proBar.Value = 50;
            Refresh(); Application.DoEvents();

            // histogram equalization
            WorkOnThread(() => output.HistogramEqualization());
            proBar.Value = 80;
            Refresh(); Application.DoEvents();

            // convert data
            Bitmap result = output.ToBitmap();
            proBar.Value = 100;
            Refresh(); Application.DoEvents();

            dstBox.Image = result;
            cookBtn.Enabled = true;
            Refresh(); Application.DoEvents();

            MessageBox.Show("This cook took: " + (end - start) + "ms.", "Time result", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dstBox_DoubleClick(object sender, EventArgs e)
        {
            if (dstBox.Image != null)
            {
                saveDialog.ShowDialog();
            }
        }

        private void openDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Bitmap bitmap;
            try
            {
                bitmap = new Bitmap(openDialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            srcBox.Image = bitmap;
        }

        private void saveDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Bitmap bitmap = new Bitmap(dstBox.Image);
                ImageFormat format = ImageFormat.Bmp;
                switch (saveDialog.FilterIndex)
                {
                    case 1: format = ImageFormat.Jpeg; break;
                    case 2: format = ImageFormat.Bmp; break;
                    case 3: format = ImageFormat.Png; break;
                }
                bitmap.Save(saveDialog.FileName, format);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
    }
}
