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
using System.Windows.Threading;

namespace HyperMonitor
{
    /// <summary>
    /// Interaction logic for ControlBar.xaml
    /// </summary>
    public partial class ControlBar : Window
    {
        Monitor parentWnd;
        int barWidth = 45;
        int barHeight = 300;
        public double scale = 1;
        DispatcherTimer timer = new DispatcherTimer();

        public ControlBar()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            parentWnd = Owner as Monitor;
            Left = parentWnd.Left - barWidth;
            Top = parentWnd.Top;
            Height = barHeight;
            Width = barWidth;
            parentWnd.LocationChanged += RepositionWindow;
            parentWnd.MouseEnter += ShowControlBar;
            parentWnd.MouseLeave += ScheduleHide;
            MouseEnter += CancelHide;
            MouseLeave += ScheduleHide;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Tick += HideControlBar;
            Hide();
        }

        private void CancelHide(object sender, MouseEventArgs e)
        {
            timer.Stop();
        }

        private void ShowControlBar(object sender, MouseEventArgs e)
        {
            Show();
            timer.Stop();
        }
        private void ScheduleHide(object sender, MouseEventArgs e)
        {
            timer.Stop();
            timer.Start();
        }
        private void HideControlBar(object sender, EventArgs e)
        {
            Hide();
        }
        private void RepositionWindow(object sender, EventArgs e)
        {
            Left = parentWnd.Left - barWidth;
            Top = parentWnd.Top;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            parentWnd.Close();
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            parentWnd.Close();
        }

        private void MinimizeApp(object sender, RoutedEventArgs e)
        {
            parentWnd.WindowState = WindowState.Minimized;
        }

        public void ScaleUp(object sender, RoutedEventArgs e)
        {
            if (scale <= 1.91)
            {
                scale += .1;
                if (parentWnd.ScaleMonitor(scale))
                    UpdateScaleIndicator();
            }
        }

        public void ScaleDown(object sender, RoutedEventArgs e)
        {
            if (scale >= .19)
            {
                scale -= .1;
                if (parentWnd.ScaleMonitor(scale))
                    UpdateScaleIndicator();
            }
        }

        private void RestoreScale(object sender, RoutedEventArgs e)
        {
            scale = 1;
            if (parentWnd.ScaleMonitor(scale))
                UpdateScaleIndicator();
        }
        public void UpdateScaleIndicator()
        {
            ScaleIndicator.Content = ((int)(scale * 100)).ToString() + "%";
        }

        private void BringForward(object sender, RoutedEventArgs e)
        {
            parentWnd.BringForward();
        }

        private void SendBackward(object sender, RoutedEventArgs e)
        {
            parentWnd.SendBackward();
        }
    }
}
