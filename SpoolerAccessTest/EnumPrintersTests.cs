using SpoolerAccess;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SpoolerAccessTest
{
    [TestClass]
    public class EnumPrintersTests
    {
        [TestMethod]
        public void TestEnumPrinters()
        {
            var printers = Spooler.EnumLocalPrinters(false);
            Assert.IsNotNull(printers);
            foreach (var printer in printers)
            {
                Console.Error.WriteLine(printer);
            }
        }
    }
}
