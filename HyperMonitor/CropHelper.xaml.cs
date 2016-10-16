using System;
using System.Collections.Generic;
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


        public CropHelper(int act)
        {
            action = (ACT)act;
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
            if(Math.Abs(nowx-downx)<10|| Math.Abs(nowy - downy) < 10)
            {
                upx = nowx = downx + 10;
                upy = nowy = downy + 10;
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
                frame.Width = nowx - downx;
            }
            if (e.GetPosition(this).Y > downy)
            {
                nowy = (int)e.GetPosition(this).Y;
                frame.Height = nowy - downy;
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
