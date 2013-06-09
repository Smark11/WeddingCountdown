using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System;
using LockScreenManager;
using System.Windows.Threading;

namespace WeddingAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            try
            { 
#if DEBUG_AGENT
              ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(30));
#endif

              Debug.WriteLine("Starting to change lock screen");
                
                
              Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        LockScreenChanger lockScreenManager = new LockScreenChanger();
                        //Step 2: ConverImageToHaveDate
                        lockScreenManager.ConvertSavedImageToHaveDate();

                        //Step 3: SetUpLockScreenWith New Image
                        lockScreenManager.SetUpLockScreen();
                    });

              Debug.WriteLine("Lock Screen changed");
            }
            catch (Exception ex)
            {

            }

            Debug.WriteLine("Periodic Task Starting Again: " + task.Name);
            NotifyComplete();
        }
    }
}