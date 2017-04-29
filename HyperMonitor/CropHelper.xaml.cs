using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HyperMonitor
{
    /// <summary>
    /// Interaction logic for CropHelper.xaml
    /// </summary>
    public partial class CropHelper : Window
    {
        int downx;
        int downy;
        int upx;
        int upy;
        int nowx;
        int nowy;
        bool down = false;
        ACT action;
        public enum ACT { AUTO, MANUAL };
        Monitor parentWnd;
        WriteableBitmap bmp;


        public CropHelper(int act)
        {
            action = (ACT)act;
            CaptureScreen();
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            confirm.Visibility = Visibility.Hidden;
            frame.Width = 0;
            frame.Height = 0;
            frame.Margin = new Thickness(e.GetPosition(this).X, e.GetPosition(this).Y, 0, 0);
            downx = (int)e.GetPosition(this).X;
            downy = (int)e.GetPosition(this).Y;
            nowx = (int)e.GetPosition(this).X;
            nowy = (int)e.GetPosition(this).Y;
            down = true;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            down = false;
            upx = nowx;
            upy = nowy;
            if (Math.Abs(nowx - downx) < 10 || Math.Abs(nowy - downy) < 10)
            {
                upx = nowx = downx + 10 - 1;
                upy = nowy = downy + 10 - 1;
                frame.Width = 10;
                frame.Height = 10;
            }
            if ((nowx - downx < 48) || (nowy - downy < 24))
            {
                confirm.Margin = new Thickness(nowx, nowy, 0, 0);
            }
            else
            {
                confirm.Margin = new Thickness(nowx - 48, nowy - 24, 0, 0);
            }
            confirm.Visibility = Visibility.Visible;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!down)
                return;
            if (e.GetPosition(this).X > downx)
            {
                nowx = (int)e.GetPosition(this).X;
                frame.Width = nowx - downx + 1;
            }
            if (e.GetPosition(this).Y > downy)
            {
                nowy = (int)e.GetPosition(this).Y;
                frame.Height = nowy - downy + 1;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            if (e.Key == Key.Enter && !down)
            {
                parentWnd.cropArea.Left = downx;
                parentWnd.cropArea.Top = downy;
                parentWnd.cropArea.Right = upx;
                parentWnd.cropArea.Bottom = upy;
                if (action == ACT.AUTO)
                {
                    parentWnd.MonitorLast(this, new RoutedEventArgs());
                    this.Close();
                }
                else
                {
                    this.Close();
                }
            }
            if (e.Key == Key.A && !down)
            {
                if (!DetermineBoundary())
                {
                    return;
                }
            }
        }

        private void CaptureScreen()
        {
            var scr = System.Windows.Forms.Screen.AllScreens;
            var mscr = scr[0];
            Bitmap scrshot = new Bitmap(mscr.Bounds.Width, mscr.Bounds.Height);
            using (var g = Graphics.FromImage(scrshot))
            {
                g.CopyFromScreen(mscr.Bounds.X, mscr.Bounds.Y, 0, 0, mscr.Bounds.Size);
            }
            using (var ms = new MemoryStream())
            {
                scrshot.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Position = 0;
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.EndInit();
                bmp = new WriteableBitmap(img);
            }
        }

        BitmapSource CropImage(BitmapSource bmp, int left, int top, int right, int bottom)
        {
            return new CroppedBitmap(bmp, new Int32Rect(left, top, right - left + 1, bottom - top + 1));
        }

        private bool DetermineBoundary()
        {
            if ((upx- downx) < 10 || (upy-downy) < 10)
            {
                MessageBox.Show("Please specify a detection area first.");
                return false;
            }
            BitmapSource cbmp = CropImage(bmp, downx, downy, upx, upy);
            int width = cbmp.PixelWidth;
            int height = cbmp.PixelHeight;
            int stride = cbmp.PixelWidth * (cbmp.Format.BitsPerPixel / 8);
            byte[] pixelData = new byte[stride * cbmp.PixelHeight];
            cbmp.CopyPixels(pixelData, stride, 0);
            List<double> sumRow = new List<double>();
            List<double> sumCol = new List<double>();
            for (int i = 1; i < cbmp.PixelHeight - 1; i++)
            {
                double sum = 0;
                for (int j = 1; j < cbmp.PixelWidth - 1; j++)
                {
                    sum += dist3(pixelData[i * stride + j * (cbmp.Format.BitsPerPixel / 8)], pixelData[i * stride + j * (cbmp.Format.BitsPerPixel / 8) + 1], pixelData[i * stride + j * (cbmp.Format.BitsPerPixel / 8) + 2],
                        pixelData[(i + 1) * stride + j * (cbmp.Format.BitsPerPixel / 8)], pixelData[(i + 1) * stride + j * (cbmp.Format.BitsPerPixel / 8) + 1], pixelData[(i + 1) * stride + j * (cbmp.Format.BitsPerPixel / 8) + 2]
                        ) / 255;
                }
                sumRow.Add(sum);
            }
            for (int j = 1; j < cbmp.PixelWidth - 1; j++)
            {
                double sum = 0;
                for (int i = 1; i < cbmp.PixelHeight - 1; i++)
                {
                    sum += dist3(pixelData[i * stride + j * (cbmp.Format.BitsPerPixel / 8)], pixelData[i * stride + j * (cbmp.Format.BitsPerPixel / 8) + 1], pixelData[i * stride + j * (cbmp.Format.BitsPerPixel / 8) + 2],
                        pixelData[i * stride + (j + 1) * (cbmp.Format.BitsPerPixel / 8)], pixelData[i * stride + (j + 1) * (cbmp.Format.BitsPerPixel / 8) + 1], pixelData[i * stride + (j + 1) * (cbmp.Format.BitsPerPixel / 8) + 2]
                        ) / 255;
                }
                sumCol.Add(sum);
            }
            int pos1, pos2;
            FindMax2(sumRow, out pos1, out pos2);
            int maxRow1, maxRow2;
            maxRow1 = pos1;
            maxRow2 = pos2;
            FindMax2(sumCol, out pos1, out pos2);
            int maxCol1, maxCol2;
            maxCol1 = pos1;
            maxCol2 = pos2;

            if ((Math.Abs(maxCol1 - maxCol2) + 1) < 11 || (Math.Abs(maxRow1 - maxRow2) + 1) < 11)
            {
                MessageBox.Show("Boundary detection failed.\nArea too small.");
                return false;
            }
            int prevX, prevY;
            prevX = downx;
            prevY = downy;

            frame.Width = Math.Abs(maxCol1 - maxCol2);
            frame.Height = Math.Abs(maxRow1 - maxRow2);
            frame.Margin = new Thickness(prevX + Math.Min(maxCol1, maxCol2) + 2, prevY + Math.Min(maxRow1, maxRow2) + 2, 0, 0);
            downx = prevX + Math.Min(maxCol1, maxCol2) + 2;
            downy = prevY + Math.Min(maxRow1, maxRow2) + 2;
            upx = prevX + Math.Max(maxCol1, maxCol2) + 1;
            upy = prevY + Math.Max(maxRow1, maxRow2) + 1;
            if ((upx - downx < 48) || (upy - downy < 24))
            {
                confirm.Margin = new Thickness(upx, upy, 0, 0);
            }
            else
            {
                confirm.Margin = new Thickness(upx - 48, upy - 24, 0, 0);
            }
            return true;
        }

        double dist3(double x1, double x2, double x3, double y1, double y2, double y3)
        {
            return Math.Sqrt((x1 - y1) * (x1 - y1) + (x2 - y2) * (x2 - y2) + (x3 - y3) * (x3 - y3));
        }

        void FindMax2(List<double> arr, out int pos1, out int pos2)
        {
            double max1val, max2val;
            max1val = max2val = double.MinValue;
            pos1 = pos2 = 0;
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] > max1val)
                {
                    if (max1val > max2val)
                    {
                        max2val = max1val;
                        pos2 = pos1;
                    }
                    max1val = arr[i];
                    pos1 = i;
                }
                else if (arr[i] > max2val)
                {
                    pos2 = i;
                    max2val = arr[i];
                }
            }
        }

        private void ButtonConfirm(object sender, RoutedEventArgs e)
        {
            parentWnd.cropArea.Left = downx;
            parentWnd.cropArea.Top = downy;
            parentWnd.cropArea.Right = upx;
            parentWnd.cropArea.Bottom = upy;
            if (action == ACT.AUTO)
            {
                parentWnd.MonitorLast(this, new RoutedEventArgs());
                this.Close();
            }
            else
            {
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            parentWnd = Owner as Monitor;
        }
    }
}
