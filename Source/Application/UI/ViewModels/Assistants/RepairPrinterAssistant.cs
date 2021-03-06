﻿using System.Collections.Generic;
using System.Linq;
using SystemInterface.IO;
using SystemWrapper.IO;
using NLog;
using pdfforge.DynamicTranslator;
using pdfforge.Obsidian;
using pdfforge.PDFCreator.Core.Printing.Printer;
using pdfforge.PDFCreator.UI.Interactions;
using pdfforge.PDFCreator.UI.Interactions.Enums;
using pdfforge.PDFCreator.Utilities;

namespace pdfforge.PDFCreator.UI.ViewModels.Assistants
{
    public interface IRepairPrinterAssistant
    {
        bool TryRepairPrinter(IEnumerable<string> printerNames);
        bool IsRepairRequired();
    }

    public class RepairPrinterAssistant : IRepairPrinterAssistant
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IAssemblyHelper _assemblyHelper;
        private readonly IInteractionInvoker _interactionInvoker;
        private readonly IPathSafe _pathSafe = new PathWrapSafe();
        private readonly IPrinterHelper _printerHelper;
        private readonly ITranslator _translator;
        private readonly IShellExecuteHelper _shellExecuteHelper;
        private readonly IFile _file;

        public RepairPrinterAssistant(IInteractionInvoker interactionInvoker, IPrinterHelper printerHelper, ITranslator translator, IShellExecuteHelper shellExecuteHelper, IFile file, IAssemblyHelper assemblyHelper)
        {
            _interactionInvoker = interactionInvoker;
            _printerHelper = printerHelper;
            _translator = translator;
            _shellExecuteHelper = shellExecuteHelper;
            _file = file;
            _assemblyHelper = assemblyHelper;
        }

        public bool TryRepairPrinter(IEnumerable<string> printerNames)
        {
            Logger.Error("It looks like the printers are broken. This needs to be fixed to allow PDFCreator to work properly");

            var title = _translator.GetTranslation("Application", "RepairPrinterNoPrintersInstalled");
            var message = _translator.GetTranslation("Application", "RepairPrinterAskUserUac");

            Logger.Debug("Asking to start repair..");

            var response = ShowMessage(message, title, MessageOptions.YesNo, MessageIcon.Exclamation);
            if (response == MessageResponse.Yes)
            {
                var applicationPath = _assemblyHelper.GetPdfforgeAssemblyDirectory();
                var printerHelperPath = _pathSafe.Combine(applicationPath, "PrinterHelper.exe");

                if (!_file.Exists(printerHelperPath))
                {
                    Logger.Error("PrinterHelper.exe does not exist!");
                    title = _translator.GetTranslation("Application", "Error");
                    message = _translator.GetFormattedTranslation("Application", "SetupFileMissing", _pathSafe.GetFileName(printerHelperPath));

                    ShowMessage(message, title, MessageOptions.OK, MessageIcon.Error);
                    return false;
                }

                Logger.Debug("Reinstalling Printers...");
                var pdfcreatorPath = _pathSafe.Combine(applicationPath, "PDFCreator.exe");

                var printerNameString = GetPrinterNameString(printerNames);

                var installParams = $"/RepairPrinter {printerNameString} /PortApplication \"{pdfcreatorPath}\"";
                var installResult = _shellExecuteHelper.RunAsAdmin(printerHelperPath, installParams);
                Logger.Debug("Done: {0}", installResult);
            }

            Logger.Debug("Now we'll check again, if the printer is installed");
            if (IsRepairRequired())
            {
                Logger.Warn("The printer could not be repaired.");
                return false;
            }

            Logger.Info("The printer was repaired successfully");

            return true;
        }

        public bool IsRepairRequired()
        {
            return !_printerHelper.GetPDFCreatorPrinters().Any();
        }

        private string GetPrinterNameString(IEnumerable<string> printerNames)
        {
            var printers = printerNames.ToList();

            if (!printers.Any())
                printers.Add("PDFCreator");

            return string.Join(" ", printers.Select(printerName => "\"" + printerName + "\""));
        }

        private MessageResponse ShowMessage(string message, string title, MessageOptions buttons, MessageIcon icon)
        {
            var interaction = new MessageInteraction(message, title, buttons, icon);
            _interactionInvoker.Invoke(interaction);
            return interaction.Response;
        }
    }
}