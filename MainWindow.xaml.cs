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

namespace ClearClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer = null;
        private Forms.NotifyIcon notifyIcon;
        private SoundPlayer tick1 = null;
        private SoundPlayer tick2 = null;
        private const int MENU1 = 0;
        private const int MENU2 = 1;
        private const int MENU3 = 2;
        private const int MENU4 = 3;
        private const int MENU5 = 4;
        private int cleanCounter = 0;
        private double screen_bottom = SystemParameters.WorkArea.Bottom;
        private double screen_top = SystemParameters.WorkArea.Top;
        private double screen_left = SystemParameters.WorkArea.Left;
        private double screen_right = SystemParameters.WorkArea.Right;
        private bool hourNotify = false;
        private bool tickSound = false;
        private bool smoothMode = false;
        private int currentHour = 0;

        //================================================================================================================
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

        //================================================================================================================
        private void Timer_Tick(object sender, EventArgs e)
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

                //Reinforce our topmost setting
                if (notifyIcon.ContextMenuStrip.Items[MENU1].Image == ClearClock.Properties.Resources.red_x)
                    this.Topmost = false;
                else
                {
                    this.Topmost = true;
                    Application.Current.Dispatcher.Invoke(() => this.Activate());
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

        //================================================================================================================
        private void UpdateClockAngles()
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
            minutes += DateTime.Now.Second / 60.0; //add more precision for smooth movement
            Debug.WriteLine($"> minutes={minutes}");
            hours += minutes / 60.0; //add more precision for smooth movement
            Debug.WriteLine($"> hours={hours}");
            double seconds = (DateTime.Now.Second * 1.0) + (DateTime.Now.Millisecond / 1000.0);
            //double millisec = Math.Round(DateTime.Now.Millisecond / 1000.0, 6, MidpointRounding.AwayFromZero);
            PART_HourLine.RenderTransform = new RotateTransform((hours / 24.0) * 360.0, 0.1, 0.1);
            PART_MinuteLine.RenderTransform = new RotateTransform((minutes / 60.0) * 360.0, 0.1, 0.1);
            PART_SecondLine.RenderTransform = new RotateTransform((seconds / 60.0) * 360.0, 0.1, 0.1);


        }

        //================================================================================================================
        private void Window_Loaded(object sender, RoutedEventArgs e)
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
                MessageBox.Show($"NotifyIcon creation error: {ex.Message}");
            }

            // Move to right-most of screen 1
            if (this.Left < (screen_right - this.Width))
                this.Left = screen_right - this.Width;

            // Move to bottom-most of screen 1
            if (this.Top < (screen_bottom - this.Height))
                this.Top = screen_bottom - this.Height;
        }

        //================================================================================================================
        private void NotifyIcon_Click(object sender, EventArgs e)
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

        //================================================================================================================
        private void OnTopmostClicked(object sender, EventArgs e)
        {
            if (this.Topmost)
            {
                this.Topmost = false;
                notifyIcon.ContextMenuStrip.Items[MENU1].Image = ClearClock.Properties.Resources.red_x;
            }
            else
            {
                this.Topmost = true;
                notifyIcon.ContextMenuStrip.Items[MENU1].Image = ClearClock.Properties.Resources.green_check;
            }
        }

        //================================================================================================================
        private void OnNotifyClicked(object sender, EventArgs e)
        {
            if (hourNotify)
            {
                hourNotify = false;
                notifyIcon.ContextMenuStrip.Items[MENU3].Image = ClearClock.Properties.Resources.red_x;
            }
            else
            {
                hourNotify = true;
                notifyIcon.ContextMenuStrip.Items[MENU3].Image = ClearClock.Properties.Resources.green_check;
            }
        }
        
        //================================================================================================================
        private void OnSoundClicked(object sender, EventArgs e)
        {
            if (tickSound)
            {
                tickSound = false;
                notifyIcon.ContextMenuStrip.Items[MENU4].Image = ClearClock.Properties.Resources.red_x;
            }
            else
            {
                tickSound = true;
                notifyIcon.ContextMenuStrip.Items[MENU4].Image = ClearClock.Properties.Resources.green_check;
            }
        }

        //================================================================================================================
        private void OnSmoothClicked(object sender, EventArgs e)
        {
            if (timer.Interval == new TimeSpan(0, 0, 0, 0, 25))
            {
                smoothMode = false;
                timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
                notifyIcon.ContextMenuStrip.Items[MENU2].Image = ClearClock.Properties.Resources.red_x;
            }
            else
            {
                smoothMode = true;
                timer.Interval = new TimeSpan(0, 0, 0, 0, 25);
                notifyIcon.ContextMenuStrip.Items[MENU2].Image = ClearClock.Properties.Resources.green_check;
            }
        }

        //================================================================================================================
        private void OnExitClicked(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }


        //================================================================================================================
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
            else if (e.RightButton == MouseButtonState.Pressed)
                Application.Current.Shutdown();
        }

        //================================================================================================================
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            // If you try to dispose of the NotifyIcon here it will not
            // work since the GUI resources are disconnected at this point.
            // Make sure you call Dispose() in the Window_Closing event.
        }

        //================================================================================================================
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
        }
    }
}
