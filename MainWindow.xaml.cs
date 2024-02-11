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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;
using System.Reflection;
using Forms = System.Windows.Forms;
using System.Media;
using System.IO;

namespace ClearClock
{
    /// <summary>
    /// TODO: Add user's selections to settings manager.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region [Props]
        DispatcherTimer timer = null;
        Forms.NotifyIcon notifyIcon;
        SoundPlayer tick1 = null;
        SoundPlayer tick2 = null;
        const int MENU1 = 0;
        const int MENU2 = 1;
        const int MENU3 = 2;
        const int MENU4 = 3;
        const int MENU5 = 4;
        int cleanCounter = 0;
        double screen_bottom = SystemParameters.WorkArea.Bottom;
        double screen_top = SystemParameters.WorkArea.Top;
        double screen_left = SystemParameters.WorkArea.Left;
        double screen_right = SystemParameters.WorkArea.Right;
        bool hourNotify = false;
        bool tickSound = false;
        bool smoothMode = false;
        int currentHour = 0;
        #endregion

        /// <summary>
        /// Default Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            tick1 = new SoundPlayer(ClearClock.Properties.Resources.tick1q);
            tick2 = new SoundPlayer(ClearClock.Properties.Resources.tick2q);

            UpdateClockAngles();

            currentHour = DateTime.Now.Hour;

            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
                timer.Tick += Timer_Tick;
                timer.Start();
                this.Topmost = true;
            }
        }

        #region [Events]
        /// <summary>
        /// <see cref="DispatcherTimer"/> event.
        /// </summary>
        void Timer_Tick(object sender, EventArgs e)
        {
            UpdateClockAngles();

            if (notifyIcon != null)
                notifyIcon.Text = DateTime.Now.ToLongDateString();

            if ((currentHour != DateTime.Now.Hour) && hourNotify)
            {
                currentHour = DateTime.Now.Hour;
                if (notifyIcon != null)
                    notifyIcon.ShowBalloonTip(5000, "Time Notice", $"The time is now {DateTime.Now.ToLongTimeString()}", Forms.ToolTipIcon.Info);
            }

            if (++cleanCounter > 10000)
            {
                cleanCounter = 0;
                GC.Collect();

                // Reinforce our topmost setting
                if (notifyIcon.ContextMenuStrip.Items[MENU1].Image == ClearClock.Properties.Resources.red_x)
                    this.Topmost = false;
                else
                {
                    this.Topmost = true;
                    // This can steal the focus when the user is engaged in a different application.
                    //Application.Current.Dispatcher.Invoke(() => this.Activate());
                }
            }

            if (tickSound && tick1 != null && tick2 != null && smoothMode == false)
            {
                if (cleanCounter % 2 == 0)
                    tick1.PlaySync();
                else
                    tick2.PlaySync();
            }

        }

        /// <summary>
        /// <see cref="Window"/> event.
        /// </summary>
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.ShowInTaskbar = false; //hide in TaskBar
            try
            {
                notifyIcon = new Forms.NotifyIcon();
                notifyIcon.Icon = ClearClock.Properties.Resources.clock;
                notifyIcon.Text = Assembly.GetExecutingAssembly().GetName().Name;
                notifyIcon.Click += NotifyIcon_Click;
                notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
                notifyIcon.ContextMenuStrip.Items.Add("Stay on top", ClearClock.Properties.Resources.green_check, OnTopmostClicked);  // MENU1
                notifyIcon.ContextMenuStrip.Items.Add("Smooth seconds", ClearClock.Properties.Resources.red_x, OnSmoothClicked);      // MENU2
                notifyIcon.ContextMenuStrip.Items.Add("Hour notifications", ClearClock.Properties.Resources.red_x, OnNotifyClicked); // MENU3
                notifyIcon.ContextMenuStrip.Items.Add("Play tick sound", ClearClock.Properties.Resources.red_x, OnSoundClicked); // MENU4
                notifyIcon.ContextMenuStrip.Items.Add("Exit application", ClearClock.Properties.Resources.exit, OnExitClicked);       // MENU5
                notifyIcon.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"NotifyIcon creation error: {ex.Message}", "ClearClock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            #region [Restore user's desired location]
            if (SettingsManager.WindowWidth != -1)
            {
                this.Top = SettingsManager.WindowTop;
                this.Left = SettingsManager.WindowLeft;
                this.Width = SettingsManager.WindowWidth;
                this.Height = SettingsManager.WindowHeight;

                var maxWidth = ScreenInformation.GetMonitorCount() * SystemParameters.WorkArea.Width;
                var maxHeight = ScreenInformation.GetMonitorCount() * SystemParameters.WorkArea.Height;
                Debug.WriteLine($"[INFO] MaxWidth:{maxWidth}  MaxHeight:{maxHeight}");
                if ((SettingsManager.WindowTop + SettingsManager.WindowHeight) > maxHeight)
                    this.Top = screen_bottom - this.Height;
                if ((SettingsManager.WindowLeft + SettingsManager.WindowWidth) > maxWidth)
                    this.Left = screen_right - this.Width;

                #region [Update menu settings]
                this.Topmost = SettingsManager.StayOnTop;
                notifyIcon.ContextMenuStrip.Items[MENU1].Image = SettingsManager.StayOnTop ? ClearClock.Properties.Resources.green_check : ClearClock.Properties.Resources.red_x;

                smoothMode = SettingsManager.SmoothSeconds;
                notifyIcon.ContextMenuStrip.Items[MENU2].Image = SettingsManager.SmoothSeconds ? ClearClock.Properties.Resources.green_check : ClearClock.Properties.Resources.red_x;
                timer.Interval = SettingsManager.SmoothSeconds ? new TimeSpan(0, 0, 0, 0, 36) : new TimeSpan(0, 0, 0, 0, 1000);

                hourNotify = SettingsManager.Notifications;
                notifyIcon.ContextMenuStrip.Items[MENU3].Image = SettingsManager.Notifications ? ClearClock.Properties.Resources.green_check : ClearClock.Properties.Resources.red_x;

                tickSound = SettingsManager.Sound;
                notifyIcon.ContextMenuStrip.Items[MENU4].Image = SettingsManager.Sound ? ClearClock.Properties.Resources.green_check : ClearClock.Properties.Resources.red_x;
                #endregion
            }
            else // The window params are defaulted to -1, meaning we have not created/loaded a config file yet.
            {
                // Move to bottom-right of screen work area.
                if (this.Left < (screen_right - this.Width))
                    this.Left = screen_right - this.Width;
                if (this.Top < (screen_bottom - this.Height))
                    this.Top = screen_bottom - this.Height;
            }
            #endregion
        }

        /// <summary>
        /// <see cref="Window"/> event.
        /// </summary>
        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Icon.Dispose();
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }

            if (this.WindowState == WindowState.Normal)
            {
                SettingsManager.WindowLeft = this.Left;
                SettingsManager.WindowTop = this.Top;
                SettingsManager.WindowHeight = this.Height;
                SettingsManager.WindowWidth = this.Width;
            }
            SettingsManager.WindowState = (int)this.WindowState;
            SettingsManager.Save(SettingsManager.AppSettings, SettingsManager.Location, SettingsManager.Version);
        }

        /// <summary>
        /// <see cref="Forms.NotifyIcon"/> event.
        /// </summary>
        void NotifyIcon_Click(object sender, EventArgs e)
        {
            //Application.Current.MainWindow.WindowState = WindowState.Normal;
            if (e is Forms.MouseEventArgs)
            {
                if ((e as Forms.MouseEventArgs).Button == Forms.MouseButtons.Left)
                {
                    //if (this.Topmost)
                    //    this.Topmost = false;
                    //else
                    //    this.Topmost = true;
                }
                else if ((e as Forms.MouseEventArgs).Button == Forms.MouseButtons.Right)
                {
                    //Application.Current.Shutdown();
                }
            }
        }

        /// <summary>
        /// <see cref="Forms.NotifyIcon"/> event.
        /// </summary>
        void OnTopmostClicked(object sender, EventArgs e)
        {
            if (this.Topmost)
            {
                this.Topmost = SettingsManager.StayOnTop = false;
                notifyIcon.ContextMenuStrip.Items[MENU1].Image = ClearClock.Properties.Resources.red_x;
            }
            else
            {
                this.Topmost = SettingsManager.StayOnTop = true;
                notifyIcon.ContextMenuStrip.Items[MENU1].Image = ClearClock.Properties.Resources.green_check;
            }
        }

        /// <summary>
        /// <see cref="Forms.NotifyIcon"/> event.
        /// </summary>
        void OnSmoothClicked(object sender, EventArgs e)
        {
            if (timer.Interval == new TimeSpan(0, 0, 0, 0, 36))
            {
                smoothMode = SettingsManager.SmoothSeconds = false;
                timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
                notifyIcon.ContextMenuStrip.Items[MENU2].Image = ClearClock.Properties.Resources.red_x;
            }
            else
            {
                smoothMode = SettingsManager.SmoothSeconds = true;
                timer.Interval = new TimeSpan(0, 0, 0, 0, 36);
                notifyIcon.ContextMenuStrip.Items[MENU2].Image = ClearClock.Properties.Resources.green_check;
            }
        }

        /// <summary>
        /// <see cref="Forms.NotifyIcon"/> event.
        /// </summary>
        void OnNotifyClicked(object sender, EventArgs e)
        {
            if (hourNotify)
            {
                hourNotify = SettingsManager.Notifications = false;
                notifyIcon.ContextMenuStrip.Items[MENU3].Image = ClearClock.Properties.Resources.red_x;
            }
            else
            {
                hourNotify = SettingsManager.Notifications = true;
                notifyIcon.ContextMenuStrip.Items[MENU3].Image = ClearClock.Properties.Resources.green_check;
            }
        }

        /// <summary>
        /// <see cref="Forms.NotifyIcon"/> event.
        /// </summary>
        void OnSoundClicked(object sender, EventArgs e)
        {
            if (tickSound)
            {
                tickSound = SettingsManager.Sound = false;
                notifyIcon.ContextMenuStrip.Items[MENU4].Image = ClearClock.Properties.Resources.red_x;
            }
            else
            {
                tickSound = SettingsManager.Sound = true;
                notifyIcon.ContextMenuStrip.Items[MENU4].Image = ClearClock.Properties.Resources.green_check;
            }
        }

        /// <summary>
        /// <see cref="Window"/> event.
        /// </summary>
        void OnExitClicked(object sender, EventArgs e) => Application.Current.Shutdown();

        /// <summary>
        /// I've replaced this logic with the Window overrides for
        /// OnMouseLeftButtonDown and OnMouseRightButtonUp.
        /// </summary>
        void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                e.Handled = true;
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Handle dragging the clock face.
        /// </summary>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        /// <summary>
        /// Provides an alternate way to exit.
        /// </summary>
        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            Application.Current.Shutdown();
        }
        #endregion

        #region [Helpers]
        /// <summary>
        /// Updates the <see cref="Transform"/> for the <see cref="UIElement"/>.
        /// </summary>
        void UpdateClockAngles()
        {
            /*
             float M_PI   = 3.14159265358979323846
             float M_PI_2 = 1.57079632679489661923
             float secondsAsRadians = (float)DateTime.Now.Second / 60.0 * 2.0 * M_PI - M_PI_2;
             float minutesAsRadians = (float)DateTime.Now.Minute / 60.0 * 2.0 * M_PI - M_PI_2;
             float hoursAsRadians = (float)DateTime.Now.Hour / 12.0 * 2.0 * M_PI - M_PI_2;
            */
            double hours = DateTime.Now.Hour * 2.0;
            double minutes = DateTime.Now.Minute * 1.0;
            minutes += DateTime.Now.Second / 60.0; // add more precision for smooth movement
            //Debug.WriteLine($"[INFO] minutes => {minutes}");
            hours += minutes / 60.0; // add more precision for smooth movement
            //Debug.WriteLine($"[INFO] hours => {hours}");
            double seconds = (DateTime.Now.Second * 1.0) + (DateTime.Now.Millisecond / 1000.0);
            //double millisec = Math.Round(DateTime.Now.Millisecond / 1000.0, 6, MidpointRounding.AwayFromZero);
            PART_HourLine.RenderTransform = new RotateTransform((hours / 24.0) * 360.0, 0.1, 0.1);
            PART_MinuteLine.RenderTransform = new RotateTransform((minutes / 60.0) * 360.0, 0.1, 0.1);
            PART_SecondLine.RenderTransform = new RotateTransform((seconds / 60.0) * 360.0, 0.1, 0.1);
        }

        /// <summary>
        /// Uses <see cref="Dispatcher.Invoke(Action, DispatcherPriority)"/> for thread safety.
        /// </summary>
        public void RunOnUIThread(Action action)
        {
            // The Application.Current may be null when closing the
            // window while a background thread is still running.
            if (action == null || Application.Current == null)
                return;

            Dispatcher dispatcher = Application.Current.Dispatcher;
            if (dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(DispatcherPriority.Normal, (Delegate)(action));
        }

        /// <summary>
        /// Uses <see cref="Dispatcher.BeginInvoke(Delegate, DispatcherPriority, object[])"/> for thread safety.
        /// </summary>
        public void RunOnUIThreadAsync(Action action)
        {
            // The Application.Current may be null when closing the
            // window while a background thread is still running.
            if (action == null || Application.Current == null)
                return;

            Dispatcher dispatcher = Application.Current.Dispatcher;
            if (dispatcher.CheckAccess())
                action();
            else
                dispatcher.BeginInvoke(DispatcherPriority.Normal, (Delegate)(action));
        }
        #endregion
    }
}
