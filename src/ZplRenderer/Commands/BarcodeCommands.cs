using System;
using SkiaSharp;
using ZplRenderer.Rendering;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// Base class for simple barcode commands, providing helpers.
    /// </summary>
    public abstract class BarcodeBaseCommand : ZplCommand
    {
        protected virtual bool ValidateData(string data)
        {
            return !string.IsNullOrEmpty(data);
        }


    }

    /// <summary>
    /// ^BC - Code 128 Barcode command.
    /// </summary>
    public class BarcodeCode128Command : BarcodeBaseCommand
    {
        public override string CommandCode => "BC";
        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;
        public int Height { get; private set; } = 0;
        public bool PrintInterpretationLine { get; private set; } = true;
        public bool PrintInterpretationAbove { get; private set; } = false;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode // Initialize pending barcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "BC", // Code 128
                 Height = Height > 0 ? Height : context.BarcodeHeight,
                 Orientation = Orientation,
                 ModuleWidth = context.ModuleWidth,
                 PrintInterpretationLine = PrintInterpretationLine,
                 PrintInterpretationLineAbove = PrintInterpretationAbove,
                 // Content to be filled by ^FD
             };
        }

        // DrawBarcode method removed as it is now handled by BarcodeDrawer via ZplElement

        protected override bool ValidateData(string data)
        {
             // Code 128 accepts any ASCII char, virtually. 
             // Just check for empty.
             return !string.IsNullOrEmpty(data);
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Height = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            PrintInterpretationLine = parts.Length > 2 ? ParseYesNo(parts[2], true) : true;
            PrintInterpretationAbove = parts.Length > 3 ? ParseYesNo(parts[3], false) : false;
        }
    }

    /// <summary>
    /// ^B3 - Code 39 Barcode command.
    /// </summary>
    public class Code39Command : BarcodeBaseCommand
    {
        public override string CommandCode => "B3";
        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;
        public int Height { get; private set; } = 0;
        public bool PrintInterpretationLine { get; private set; } = true;
        public bool PrintInterpretationAbove { get; private set; } = false;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "B3", // Code 39
                 Height = Height > 0 ? Height : context.BarcodeHeight,
                 Orientation = Orientation,
                 ModuleWidth = context.ModuleWidth,
                 PrintInterpretationLine = PrintInterpretationLine,
                 PrintInterpretationLineAbove = PrintInterpretationAbove
             };
        }

        // DrawBarcode removed

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            // Part 1 is check digit (ignored)
            Height = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
            PrintInterpretationLine = parts.Length > 3 ? ParseYesNo(parts[3], true) : true;
            PrintInterpretationAbove = parts.Length > 4 ? ParseYesNo(parts[4], false) : false;
        }
    }

    /// <summary>
    /// ^BE - EAN-13 Barcode command.
    /// </summary>
    public class EAN13Command : BarcodeBaseCommand
    {
        public override string CommandCode => "BE";
        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;
        public int Height { get; private set; } = 0;
        public bool PrintInterpretationLine { get; private set; } = true;
        public bool PrintInterpretationAbove { get; private set; } = false;

        public override void Execute(RenderContext context)
        {
             // BarcodeType deve corrispondere alla chiave usata nel BarcodeDrawer switch:
             // "BE" → BarcodeFormat.EAN_13 (era "EAN13", che cadeva nel default → CODE_128)
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "BE", 
                 Height = Height > 0 ? Height : context.BarcodeHeight,
                 Orientation = Orientation,
                 ModuleWidth = context.ModuleWidth,
                 PrintInterpretationLine = PrintInterpretationLine,
                 PrintInterpretationLineAbove = PrintInterpretationAbove
             };
        }

        // DrawBarcode removed

        protected override bool ValidateData(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;
            // EAN-13 requires 12 or 13 digits.
            // If 12, check digit is calculated.
            return System.Text.RegularExpressions.Regex.IsMatch(data, "^[0-9]{12,13}$");
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Height = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            PrintInterpretationLine = parts.Length > 2 ? ParseYesNo(parts[2], true) : true;
            PrintInterpretationAbove = parts.Length > 3 ? ParseYesNo(parts[3], false) : false;
        }
    }

    /// <summary>
    /// ^BA - Code 93 Barcode command.
    /// </summary>
    public class Code93Command : BarcodeBaseCommand
    {
        public override string CommandCode => "BA";
        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;
        public int Height { get; private set; } = 0;
        public bool PrintInterpretationLine { get; private set; } = true;
        public bool PrintInterpretationAbove { get; private set; } = false;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "BA", // Code 93
                 Height = Height > 0 ? Height : context.BarcodeHeight,
                 Orientation = Orientation,
                 ModuleWidth = context.ModuleWidth,
                 PrintInterpretationLine = PrintInterpretationLine,
                 PrintInterpretationLineAbove = PrintInterpretationAbove
             };
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Height = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            PrintInterpretationLine = parts.Length > 2 ? ParseYesNo(parts[2], true) : true;
            PrintInterpretationAbove = parts.Length > 3 ? ParseYesNo(parts[3], false) : false;
        }
    }

    /// <summary>
    /// ^BU - UPC-A Barcode command.
    /// </summary>
    public class UPCACommand : BarcodeBaseCommand
    {
        public override string CommandCode => "BU";
        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;
        public int Height { get; private set; } = 0;
        public bool PrintInterpretationLine { get; private set; } = true;
        public bool PrintInterpretationAbove { get; private set; } = false;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "BU", // UPC-A
                 Height = Height > 0 ? Height : context.BarcodeHeight,
                 Orientation = Orientation,
                 ModuleWidth = context.ModuleWidth,
                 PrintInterpretationLine = PrintInterpretationLine,
                 PrintInterpretationLineAbove = PrintInterpretationAbove
             };
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Height = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            PrintInterpretationLine = parts.Length > 2 ? ParseYesNo(parts[2], true) : true;
            PrintInterpretationAbove = parts.Length > 3 ? ParseYesNo(parts[3], false) : false;
        }
    }

    /// <summary>
    /// ^BQ - QR Code Barcode command.
    /// </summary>
    public class BarcodeQRCommand : BarcodeBaseCommand
    {
        public override string CommandCode => "BQ";
        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;
        public int Model { get; private set; } = 2;
        public int Magnification { get; private set; } = 3;
        public char ErrorCorrection { get; private set; } = 'M';

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "BQ", // QR
                 Orientation = Orientation,
                 ModuleWidth = Magnification,
                 ErrorCorrectionLevel = ErrorCorrection,
             };
        }

        private string ParseQRData(string fieldData)
        {
            if (fieldData.Length > 2 && fieldData[2] == ',') return fieldData.Substring(3);
            return fieldData;
        }

        private ZXing.QrCode.Internal.ErrorCorrectionLevel GetErrorCorrectionLevel()
        {
            switch (char.ToUpperInvariant(ErrorCorrection))
            {
                case 'H': return ZXing.QrCode.Internal.ErrorCorrectionLevel.H;
                case 'Q': return ZXing.QrCode.Internal.ErrorCorrectionLevel.Q;
                case 'L': return ZXing.QrCode.Internal.ErrorCorrectionLevel.L;
                default: return ZXing.QrCode.Internal.ErrorCorrectionLevel.M;
            }
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Model = parts.Length > 1 ? ParseInt(parts[1], 2) : 2;
            Magnification = parts.Length > 2 ? ParseInt(parts[2], 3) : 3;
            if (parts.Length > 3 && parts[3].Length > 0) ErrorCorrection = parts[3].Trim()[0];
            if (Magnification < 1) Magnification = 1;
            if (Magnification > 10) Magnification = 10;
        }
    }

    /// <summary>
    /// ^BX - Data Matrix Barcode command.
    /// </summary>
    public class DataMatrixCommand : BarcodeBaseCommand
    {
        public override string CommandCode => "BX";
        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;
        public int Height { get; private set; } = 30;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "BX", // DataMatrix
                 Height = Height,
                 Orientation = Orientation
             };
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Height = parts.Length > 1 ? ParseInt(parts[1], 10) : 10;
        }
    }

    /// <summary>
    /// ^B7 - PDF417 Barcode command.
    /// </summary>
    public class PDF417Command : BarcodeBaseCommand
    {
        public override string CommandCode => "B7";
        public FieldOrientation Orientation { get; private set; }
        public int Height { get; private set; } = 30;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "B7", // PDF417
                 Height = Height,
                 Orientation = Orientation
             };
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Height = parts.Length > 1 ? ParseInt(parts[1], 10) : 10;
        }
    }

    /// <summary>
    /// ^B0 - Aztec Barcode command.
    /// </summary>
    public class AztecCommand : BarcodeBaseCommand
    {
        public override string CommandCode => "B0";
        public FieldOrientation Orientation { get; private set; }
        public int Magnification { get; private set; } = 2;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "B0", // Aztec
                 ModuleWidth = Magnification,
                 Orientation = Orientation
             };
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Magnification = parts.Length > 1 ? ParseInt(parts[1], 2) : 2;
        }
    }

    /// <summary>
    /// ^BD - MaxiCode Barcode command.
    /// </summary>
    public class MaxiCodeCommand : BarcodeBaseCommand
    {
        public override string CommandCode => "BD";
        public int Mode { get; private set; } = 2;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "BD", // MaxiCode
                 Orientation = FieldOrientation.Normal// MaxiCode is fixed orientation usually
             };
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Mode = parts.Length > 0 ? ParseInt(parts[0], 2) : 2;
        }
    }
    /// <summary>
    /// ^B2 - Interleaved 2 of 5 Barcode.
    /// </summary>
    public class Interleaved2of5Command : BarcodeBaseCommand
    {
        public override string CommandCode => "B2";
        public FieldOrientation Orientation { get; private set; }
        public int Height { get; private set; } = 0;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "B2",
                 Height = Height > 0 ? Height : context.BarcodeHeight,
                 Orientation = Orientation,
                 ModuleWidth = context.ModuleWidth
             };
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
            Height = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
        }
    }

    /// <summary>
    /// ^BK - ANSI Codabar Barcode.
    /// </summary>
    public class CodabarCommand : BarcodeBaseCommand
    {
        public override string CommandCode => "BK";
        public FieldOrientation Orientation { get; private set; }
        public int Height { get; private set; } = 0;

        public override void Execute(RenderContext context)
        {
             context.PendingBarcode = new ZplRenderer.Elements.ZplBarcode
             {
                 X = context.AbsoluteX,
                 Y = context.AbsoluteY,
                 OriginType = context.IsBaselinePosition ? Elements.ElementOriginType.Baseline : Elements.ElementOriginType.TopLeft,
                 BarcodeType = "BK",
                 Height = Height > 0 ? Height : context.BarcodeHeight,
                 Orientation = Orientation
             };
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
             if (parts.Length > 0 && parts[0].Length > 0) Orientation = RenderContext.ParseOrientation(parts[0][0]);
             Height = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
        }
    }
}
