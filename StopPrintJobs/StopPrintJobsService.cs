using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using SpoolerAccess;

namespace StopPrintJobs
{
    public partial class StopPrintJobsService : ServiceBase
    {
        private Spooler Spool;
        private Thread StopperThread;

        public StopPrintJobsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // decode the list of printers to stop
            var stopUsString = StopPrintJobs.Properties.Settings.Default.PrintersToStop;
            var stopUs = new List<string>(stopUsString.Split(';').Select((s) => s.Trim()));

            // prepare for stoppage
            Spool = new Spooler();

            // launch the thread
            StopperThread = new Thread(() => Spool.PauseNewJobsProc(stopUs));
            StopperThread.Start();
        }

        protected override void OnStop()
        {
            Spool.StopPausingNewJobs();
            StopperThread.Join();
            Spool.Dispose();
            Spool = null;
            StopperThread = null;
        }
    }
}
