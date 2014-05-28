using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpoolerAccess;

namespace SpoolerAccessTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            var allPrinters = Spooler.EnumLocalPrinters(false);
            Spooler.PauseNewJobsProc(allPrinters);
        }
    }
}
