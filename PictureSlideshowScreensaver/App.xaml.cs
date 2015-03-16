using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Runtime.InteropServices;

namespace PictureSlideshowScreensaver
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private uint fPreviousExecutionState;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                string first = e.Args[0].ToLower().Trim();
                string second = null;

                if (first.Length > 2)
                {
                    second = first.Substring(3).Trim();
                    first = first.Substring(0, 2);
                }
                else if (e.Args.Length > 1)
                {
                    second = e.Args[1];
                }


            // Configuration mode
                if (first == "/c")           
                {
                    new Configuration().Show();
                }

            // Preview mode
                else if (first == "/p")      
                {
                    // No Preview mode implemented!
                    Application.Current.Shutdown();                 
                }

            // Full-screen mode
                else if (first == "/s")      
                {
                    LaunchScreensaver();
                }

            // Undefined argument
                else    
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {

                // Set new state to prevent system sleep
                fPreviousExecutionState = NativeMethods.SetThreadExecutionState(
                    NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
                // No argument, launch screensaver.
                LaunchScreensaver();
                
            }
        }

        

        private void LaunchScreensaver()
        {
            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {

                System.Drawing.Rectangle r = screen.Bounds;
#if DEBUG
                r.Width = r.Width / 2;
                r.Height = r.Height / 2;
#endif

                Screensaver s = new Screensaver(r);
                s.WindowStartupLocation = WindowStartupLocation.Manual;
                s.Left = r.X;
                s.Top = r.Y;
                s.Width = r.Width;
                s.Height = r.Height;

#if DEBUG
                s.Left = r.X + r.Width;
                s.Top = r.Y + r.Height;
#endif

                s.Show();
                
            }
        }


        internal static class NativeMethods
        {
            // Import SetThreadExecutionState Win32 API and necessary flags
            [DllImport("kernel32.dll")]
            public static extern uint SetThreadExecutionState(uint esFlags);
            public const uint ES_CONTINUOUS = 0x80000000;
            public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Restore previous state
            if (NativeMethods.SetThreadExecutionState(fPreviousExecutionState) == 0)
            {
                // No way to recover; already exiting
            }
        }
    }
}
