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

        private int[,] ParseTemplate()
        {
            int[,] template = new int[9, 9];
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    template[i, j] = 0;

            int r = 0, c = 0;
            foreach (TextBox t in templateTable.Controls)
            {
                if (!int.TryParse(t.Text, out int v))
                    v = 0;

                template[r, c] = v;

                c += 1;
                if (c >= 9)
                {
                    r += 1;
                    if (r >= 9) break;
                    c = 0;
                }
            }

            return template;
        }

        private void cookBtn_Click(object sender, EventArgs e)
        {
            if (srcBox.Image == null) return;

            proBar.Value = 0;

            // parse template
            int[,] template = ParseTemplate();
            proBar.Value = 5;
            Application.DoEvents();

            // parse scale
            if (!double.TryParse(scaleBox.Text, out double scale))
                scale = 1;
            // get bitmap
            const int siz_t = 9; 
            Bitmap bitmap = new Bitmap(srcBox.Image);
            if (bitmap.Width < siz_t || bitmap.Height < siz_t)
            {
                MessageBox.Show("Image is too small!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            proBar.Value = 10;
            Application.DoEvents();

            // convert data
            MultiCook.Image image = new MultiCook.Image(bitmap);
            proBar.Value = 20;
            Application.DoEvents();

            // apply filter
            MultiCook.Image output = image.ApplyFilter(template, scale);
            proBar.Value = 50;
            Application.DoEvents();

            // histogram equalization
            output.HistogramEqualization();
            proBar.Value = 80;
            Application.DoEvents();

            // convert data
            Bitmap result = output.ToBitmap();
            proBar.Value = 100;
            Application.DoEvents();

            dstBox.Image = result;
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
