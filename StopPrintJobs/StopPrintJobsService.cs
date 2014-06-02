// Released into the public domain.
// http://creativecommons.org/publicdomain/zero/1.0/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
#if I_LOVE_C_PLUS_PLUS_CLI
using SpoolerAccess;
#else
using SpoolerAccessPI;
#endif

namespace StopPrintJobs
{
    public partial class StopPrintJobsService : ServiceBase
    {
        private Spooler Spool;
        private Thread StopperThread;
        private bool StopStopping;

        public StopPrintJobsService()
        {
            InitializeComponent();

            if (!EventLog.SourceExists("StopPrintJobs"))
            {
                EventLog.CreateEventSource("StopPrintJobs", "Application");
            }
            TheEventLog.Source = "StopPrintJobs";
            TheEventLog.Log = "Application";

            StopStopping = false;
        }

        private void Proc(List<string> stopThesePrinters)
        {
            for (; ; )
            {
                if (StopStopping)
                {
                    break;
                }

                try
                {
                    Spool.PauseNewJobsProc(stopThesePrinters);

                    // if PauseNewJobsProc returns, it's a clean shutdown
                    break;
                }
                #if I_LOVE_C_PLUS_PLUS_CLI
                catch (SpoolerAccess.Win32Exception exc)
                #else
                catch (SpoolerAccessPI.InteropHelpers.NativeCodeException exc)
                #endif
                {
                    if (StopPrintJobs.Properties.Settings.Default.LogLevel > 0)
                    {
                        TheEventLog.WriteEntry(string.Format(
                            "{0} (function {1} returned code {2})",
                            exc.Message,
                            exc.NativeFunction,
                            exc.ErrorCode
                        ));
                    }
                    if (!StopPrintJobs.Properties.Settings.Default.RestartOnError)
                    {
                        // don't try again
                        break;
                    }
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            StopStopping = false;

            // decode the list of printers to stop
            var stopUsString = StopPrintJobs.Properties.Settings.Default.PrintersToStop;
            var stopUs = new List<string>();
            foreach (var printer in stopUsString.Split(';'))
            {
                var trimmedPrinter = printer.Trim();
                if (trimmedPrinter.Length != 0)
                {
                    stopUs.Add(trimmedPrinter);
                }
            }

            if (stopUs.Count == 0)
            {
                TheEventLog.WriteEntry("No printers to stop! Exiting.");
                this.Stop();
                return;
            }

            // prepare for stoppage
            Spool = new Spooler();

            // launch the thread
            StopperThread = new Thread(() => Proc(stopUs));
            StopperThread.Start();
        }

        protected override void OnStop()
        {
            StopStopping = true;
            if (Spool != null)
            {
                Spool.StopPausingNewJobs();
            }
            if (StopperThread != null)
            {
                StopperThread.Join();
            }
            if (Spool != null)
            {
                Spool.Dispose();
            }
            Spool = null;
            StopperThread = null;
        }
    }
}
