using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

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

        private void cookBtn_Click(object sender, EventArgs e)
        {
            if (srcBox.Image == null) return;

            proBar.Value = 0;

            int[,] template = new int[9, 9];
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    template[i, j] = 0;

            proBar.Value = 5;

            int r = 0, c = 0, v = 0;
            foreach (TextBox t in templateTable.Controls)
            {
                try
                {
                    v = Convert.ToInt32(t.Text);
                }
                catch (Exception ex)
                {
                    v = 0;
                }
                template[r, c] = v;

                c += 1;
                if (c >= 9)
                {
                    r += 1;
                    if (r >= 9) break;
                    c = 0;
                }
            }

            double scale;
            try
            {
                scale = Convert.ToDouble(scaleBox.Text);
            }
            catch (Exception ex)
            {
                scale = 1;
            }

            const int siz_t = 9, siz_h = siz_t / 2; 
            Bitmap bitmap = new Bitmap(srcBox.Image);

            if (bitmap.Width < siz_t || bitmap.Height < siz_t)
            {
                MessageBox.Show("Image is too small!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            proBar.Value = 10;

            loopBar.Value = 0;
            loopBar.Maximum = bitmap.Height * bitmap.Width;
            double[,,] rawBmp = new double[bitmap.Height, bitmap.Width, 4];
            double[,,] copyBmp = new double[bitmap.Height, bitmap.Width, 4];
            for (int i = 0; i < bitmap.Height; i++)
                for (int j = 0; j < bitmap.Width; j++)
                {
                    copyBmp[i, j, 0] = rawBmp[i, j, 0] = Convert.ToDouble(bitmap.GetPixel(j, i).R);
                    copyBmp[i, j, 1] = rawBmp[i, j, 1] = Convert.ToDouble(bitmap.GetPixel(j, i).G);
                    copyBmp[i, j, 2] = rawBmp[i, j, 2] = Convert.ToDouble(bitmap.GetPixel(j, i).B);
                    copyBmp[i, j, 3] = rawBmp[i, j, 3] = Convert.ToDouble(bitmap.GetPixel(j, i).A);
                    loopBar.PerformStep();
                }

            proBar.Value = 20;

            loopBar.Value = 0;
            loopBar.Maximum = (bitmap.Height - siz_t + 1) * (bitmap.Width - siz_t + 1);
            double pixel;
            for (int h = siz_h; h < bitmap.Height - siz_h; h++)
                for (int w = siz_h; w < bitmap.Width - siz_h; w++)
                {
                    for (int channel = 0; channel < 3; channel++)
                    {
                        pixel = 0;
                        for (r = 0; r < siz_t; r++)
                            for (c = 0; c < siz_t; c++)
                                pixel += copyBmp[h - siz_h + c, w - siz_h + r, channel] * template[r, c];
                        rawBmp[h, w, channel] = pixel / scale;
                    }
                    loopBar.PerformStep();
                }

            proBar.Value = 50;

            
            for (int channel = 0; channel < 3; channel++)
            {
                double max = _max(rawBmp, channel), min = _min(rawBmp, channel);

                loopBar.Value = 0;
                loopBar.Maximum = bitmap.Height * bitmap.Width;
                for (int i = 0; i < bitmap.Height; i++)
                    for (int j = 0; j < bitmap.Width; j++)
                    {
                        if (_sum(template) == 0)
                            rawBmp[i, j, channel] = (rawBmp[i, j, channel] - min) / (max - min) * 255;
                        else
                            rawBmp[i, j, channel] = rawBmp[i, j, channel] > 255 ? 255 : (
                                rawBmp[i, j, channel] < 0 ? 0 : rawBmp[i, j, channel]
                            );
                        loopBar.PerformStep();
                    }

                proBar.Value += 10;
            }

            proBar.Value = 80;

            loopBar.Value = 0;
            loopBar.Maximum = bitmap.Height * bitmap.Width;
            for (int i = 0; i < bitmap.Height; i++)
                for (int j = 0; j < bitmap.Width; j++)
                {
                    bitmap.SetPixel(j, i,
                        Color.FromArgb(
                            Convert.ToInt32(rawBmp[i, j, 3]),
                            Convert.ToInt32(rawBmp[i, j, 0]),
                            Convert.ToInt32(rawBmp[i, j, 1]),
                            Convert.ToInt32(rawBmp[i, j, 2])
                        )
                    );
                    loopBar.PerformStep();
                }

            proBar.Value = 100;

            dstBox.Image = bitmap;
        }

        protected int _sum(int[,] data)
        {
            int sum = 0;
            foreach (int a in data)
                sum += a;
            return sum;
        }

        protected double _max(double[,,] data, int channel)
        {
            loopBar.Value = 0;
            loopBar.Maximum = data.GetUpperBound(0) * data.GetUpperBound(1);
            double max = data[0, 0, channel];
            for (int i = 0; i < data.GetUpperBound(0); i++)
                for (int j = 0; j < data.GetUpperBound(1); j++)
                {
                    if (data[i, j, channel] > max) max = data[i, j, channel];
                    loopBar.PerformStep();
                }
                   
            return max;
        }

        protected double _min(double[,,] data, int channel)
        {
            loopBar.Value = 0;
            loopBar.Maximum = data.GetUpperBound(0) * data.GetUpperBound(1);
            double min = data[0, 0, channel];
            for (int i = 0; i < data.GetUpperBound(0); i++)
                for (int j = 0; j < data.GetUpperBound(1); j++)
                {
                    if (data[i, j, channel] < min) min = data[i, j, channel];
                    loopBar.PerformStep();
                }
            return min;
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
