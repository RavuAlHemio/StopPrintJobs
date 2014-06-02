// Released into the public domain.
// http://creativecommons.org/publicdomain/zero/1.0/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
#if I_LOVE_C_PLUS_PLUS_CLI
using SpoolerAccess;
#else
using SpoolerAccessPI;
#endif

namespace SpoolerAccessTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            var allPrinters = Spooler.EnumLocalPrinters(false);
            foreach (var printer in allPrinters)
            {
                Console.WriteLine(printer);
            }
            using (var spooler = new Spooler())
            {
                spooler.PauseNewJobsProc(allPrinters);
            }
        }
    }
}
