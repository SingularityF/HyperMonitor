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
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace HyperMonitor
{
    /// <summary>
    /// Interaction logic for Monitor.xaml
    /// </summary>
    public partial class Monitor : Window
    {
        // DWM API -->

        public static readonly int DWM_TNP_RECTDESTINATION = 0x1;
        public static readonly int DWM_TNP_RECTSOURCE = 0x2;

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public Rect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct PSIZE
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }
        [DllImport("dwmapi.dll")]
        public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);
        [DllImport("dwmapi.dll")]
        public static extern int DwmUnregisterThumbnail(IntPtr thumb);
        [DllImport("dwmapi.dll")]
        public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);
        [DllImport("dwmapi.dll")]
        public static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        // <-- END DWM API

        // WIN API -->

        public delegate void WinEventDelegate(IntPtr hWinEventHook, int eventType, IntPtr hWnd, int idObject, int idChild, int dwEventThread, int dwmsEventTime);

        public static readonly int EVENT_SYSTEM_FOREGROUND = 0x0003;
        public static readonly int WINEVENT_OUTOFCONTEXT = 0x0000;
        public static readonly int WINEVENT_SKIPOWNPROCESS = 0x0002;
        public static readonly int WM_HOTKEY = 0x0312;
        public static readonly int SWP_NOMOVE = 0x0002;
        public static readonly int SWP_NOSIZE = 0x0001;
        public static readonly int SWP_SHOWWINDOW = 0x0040;
        public static readonly int SWP_NOACTIVATE = 0x0010;
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);


        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int x;
            public int y;
        }
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point pt);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, int idProcess, int idThread, int dwFlags);
        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int modkey, int key);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);



        // <-- END WIN API

        string[] args;
        string path;
        IntPtr SourcehWnd = IntPtr.Zero;
        IntPtr thumb = IntPtr.Zero;
        public Rect cropArea = new Rect();
        public IntPtr lasthWnd = IntPtr.Zero;
        ControlBar ctrlBar;
        WinEventDelegate procDelegate;
        IntPtr eventhook;
        public enum ACT { AUTO, MANUAL };
        public int hotkeyid = 0;
        bool isslave = false;
        bool targetWndForward = false;



        public Monitor()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            args = Environment.GetCommandLineArgs();
            path = args[0];
            if (args.Length > 1)
            {
                isslave = true;
                ControlPanel.Visibility = Visibility.Hidden;
                Background = Brushes.Black;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                int width = int.Parse(args[4]) - int.Parse(args[2]);
                int height = int.Parse(args[5]) - int.Parse(args[3]);
                cropArea.Left = int.Parse(args[2]);
                cropArea.Top = int.Parse(args[3]);
                cropArea.Right = int.Parse(args[4]);
                cropArea.Bottom = int.Parse(args[5]);
                Width = width + 6;
                Height = height + 6;
                lasthWnd = new IntPtr(int.Parse(args[1], System.Globalization.NumberStyles.AllowHexSpecifier));
                SetupMonitor(lasthWnd);
                NoiseTile.Visibility = Visibility.Visible;
                ctrlBar = new ControlBar();
                ctrlBar.Owner = this;
                ctrlBar.Show();
            }
            else
            {
                procDelegate = new WinEventDelegate(WinEventProc);
                eventhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
                Random rnd = new Random();
                hotkeyid = rnd.Next(10000, 40000);
                while (RegisterHotKey(new WindowInteropHelper(this).Handle, hotkeyid, 0, 0x7A) == false)
                {
                    hotkeyid = rnd.Next(10000, 40000);
                }
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                source.AddHook(new HwndSourceHook(WndProc));
            }
        }

        public void WinEventProc(IntPtr hWinEventHook, int eventType, IntPtr hWnd, int idObject, int idChild, int dwEventThread, int dwmsEventTime)
        {
            SampleWindow(this, new EventArgs());
        }

        private void SampleWindow(object sender, EventArgs e)
        {
            IntPtr hWnd;
            hWnd = GetForegroundWindow();
            if (hWnd != IntPtr.Zero && hWnd != new WindowInteropHelper(this).Handle)
            {
                lasthWnd = hWnd;
                UpdateWindowTitle(hWnd);
            }
        }

        public void UpdateWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);

            if (GetWindowText(hWnd, Buff, nChars) > 0)
            {
                TitleDisplay.Content = Buff.ToString();
            }
        }

        public void SetupMonitor(IntPtr target)
        {
            IntPtr self = new WindowInteropHelper(this).Handle;

            DWM_THUMBNAIL_PROPERTIES prop = new DWM_THUMBNAIL_PROPERTIES();

            prop = new DWM_THUMBNAIL_PROPERTIES();
            prop.rcSource = new Rect(cropArea.Left, cropArea.Top, cropArea.Right, cropArea.Bottom);
            prop.rcDestination = new Rect(3, 3, (int)(cropArea.Right - cropArea.Left + 3), (int)(cropArea.Bottom - cropArea.Top + 3));
            prop.dwFlags = DWM_TNP_RECTDESTINATION | DWM_TNP_RECTSOURCE;

            DwmRegisterThumbnail(self, target, out thumb);
            DwmUpdateThumbnailProperties(thumb, ref prop);

        }

        public void MonitorLast(object sender, RoutedEventArgs e)
        {
            if (!LegitCheck(lasthWnd))
            {
                return;
            }
            if (lasthWnd != IntPtr.Zero)
            {
                Rect rect;
                GetWindowRect(lasthWnd, out rect);
                Process.Start(path, lasthWnd.ToString("X") + " " + (cropArea.Left - rect.Left) + " " + (cropArea.Top - rect.Top) + " " + (cropArea.Right - rect.Left) + " " + (cropArea.Bottom - rect.Top));
            }
        }

        private bool LegitCheck(IntPtr hWnd)
        {
            Rect rect;
            if (!GetWindowRect(hWnd, out rect))
            {
                MessageBox.Show("Illegal handle.");
                return false;
            }
            if ((cropArea.Right - cropArea.Left) < 10 || (cropArea.Bottom - cropArea.Top) < 10)
            {
                MessageBox.Show("Please specify crop area.");
                return false;
            }
            return true;
        }

        public bool ScaleMonitor(double scaleFactor)
        {
            if (thumb != IntPtr.Zero)
            {
                DWM_THUMBNAIL_PROPERTIES prop = new DWM_THUMBNAIL_PROPERTIES();

                prop = new DWM_THUMBNAIL_PROPERTIES();
                prop.rcSource = new Rect(cropArea.Left, cropArea.Top, cropArea.Right, cropArea.Bottom);
                prop.rcDestination = new Rect(3, 3, (int)(scaleFactor * (cropArea.Right - cropArea.Left)) + 3, (int)(scaleFactor * (cropArea.Bottom - cropArea.Top)) + 3);
                prop.dwFlags = DWM_TNP_RECTDESTINATION | DWM_TNP_RECTSOURCE;
                DwmUpdateThumbnailProperties(thumb, ref prop);

                Width = scaleFactor * (cropArea.Right - cropArea.Left) + 6;
                Height = scaleFactor * (cropArea.Bottom - cropArea.Top) + 6;
                return true;
            }
            return false;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
            else
            {
                WindowState = WindowState.Minimized;
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (ctrlBar != null)
                    ctrlBar.ScaleUp(this, new RoutedEventArgs());
            }
            else
            {
                if (ctrlBar != null)
                    ctrlBar.ScaleDown(this, new RoutedEventArgs());
            }
        }

        private void SpecifyCropArea(object sender, RoutedEventArgs e)
        {
            CropHelper helper = new CropHelper((int)ACT.MANUAL);
            helper.Owner = this;
            helper.Show();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                if (wParam.ToInt32() == hotkeyid)
                {
                    CropHelper crophelper = new CropHelper((int)ACT.AUTO);
                    crophelper.Owner = this;
                    crophelper.Show();
                }
            }
            return IntPtr.Zero;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnhookWinEvent(eventhook);
            UnregisterHotKey(new WindowInteropHelper(this).Handle, hotkeyid);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isslave && e.Key == Key.Tab)
            {
                if (!targetWndForward)
                {
                    BringForward();
                }
                else
                {
                    SendBackward();
                }
            }
        }
        public void BringForward()
        {
            SetForegroundWindow(lasthWnd);
            targetWndForward = true;
            ctrlBar.SendBackwardBtn.Visibility = Visibility.Visible;
            ctrlBar.BringForwardBtn.Visibility = Visibility.Collapsed;
        }
        public void SendBackward()
        {
            SetWindowPos(lasthWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            targetWndForward = false;
            ctrlBar.SendBackwardBtn.Visibility = Visibility.Collapsed;
            ctrlBar.BringForwardBtn.Visibility = Visibility.Visible;
        }
    }
}
