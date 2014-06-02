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
using SpoolerAccessPI;

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
                catch (SpoolerAccessPI.InteropHelpers.FatalNativeCodeException exc)
                {
                    TheEventLog.WriteEntry(string.Format(
                        "{0} (function {1} returned code {2})",
                        exc.Message,
                        exc.NativeFunction,
                        exc.ErrorCode
                    ), EventLogEntryType.Error);
                    break;
                }
                catch (SpoolerAccessPI.InteropHelpers.NativeCodeException exc)
                {
                    if (StopPrintJobs.Properties.Settings.Default.LogLevel > 0)
                    {
                        TheEventLog.WriteEntry(string.Format(
                            "{0} (function {1} returned code {2})",
                            exc.Message,
                            exc.NativeFunction,
                            exc.ErrorCode
                        ), EventLogEntryType.Warning);
                    }
                    if (StopPrintJobs.Properties.Settings.Default.StopOnNonfatalError)
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
