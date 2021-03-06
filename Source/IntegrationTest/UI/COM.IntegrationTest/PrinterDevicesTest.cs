﻿using System;
using NUnit.Framework;
using pdfforge.PDFCreator.Core.Printing.Printer;
using pdfforge.PDFCreator.UI.COM;

namespace pdfforge.PDFCreator.IntegrationTest.UI.COM
{
    [TestFixture]
    internal class PrinterDevicesTest
    {
        [SetUp]
        public void SetUp()
        {
            // TODO add own initialization?
            _printerDevices = new Printers(new PrinterHelper());
        }

        [TearDown]
        public void TearDown()
        {
        }

        private Printers _printerDevices;

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void GetPrinterByIndex_IfIndexNegative_ThrowArgumentException()
        {
            _printerDevices.GetPrinterByIndex(-1);
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void GetPrinterByIndex_IfIndexTooBig_ThrowArgumentException()
        {
            var count = _printerDevices.Count;

            _printerDevices.GetPrinterByIndex(count + 1);
        }
    }
}