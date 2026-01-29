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

        protected void DrawRotated(SKCanvas canvas, SKBitmap bitmap, float x, float y, FieldOrientation orientation)
        {
            canvas.Save();
            
            // Transform to origin
            canvas.Translate(x, y);
            
            // Rotate
            if (orientation != FieldOrientation.Normal)
                canvas.RotateDegrees((int)orientation);

            // Check Clipping/Bounds (Simplified visual check logic)
            // In a real scenario, we might want to check if the transformed rect intersects with the canvas clip.
            // For now, we proceed to draw.
            
            // Draw bitmap at (0,0) relative to rotated origin
            canvas.DrawBitmap(bitmap, 0, 0);
            
            canvas.Restore();
        }

        protected void DrawInterpretation(SKCanvas canvas, string text, float x, float y, int h, bool above, RenderContext ctx)
        {
            float textY = above ? y - 20 : y + h + 5;
            using (var paint = ctx.CreateTextPaint(SKColors.Black))
            {
                paint.TextSize = 10 * ctx.ScaleFactor;
                canvas.DrawText(text, x, textY, paint);
            }
        }

        protected void DrawPlaceholder(SKCanvas canvas, float x, float y, float w, float h, RenderContext ctx)
        {
            using (var paint = ctx.CreatePaint(SKColors.Red, true, 2))
            {
                canvas.DrawRect(x, y, w, h, paint);
            }
        }

        /// <summary>
        /// Draws fallback text when barcode encoding fails.
        /// Shows a bordered box with the data text inside.
        /// </summary>
        protected void DrawFallbackText(SKCanvas canvas, string data, float x, float y, float w, float h, RenderContext ctx)
        {
            // Draw border
            using (var borderPaint = ctx.CreatePaint(SKColors.Black, true, 1))
            {
                canvas.DrawRect(x, y, w, h, borderPaint);
            }
            
            // Draw text inside
            using (var textPaint = ctx.CreateTextPaint(SKColors.Black))
            {
                textPaint.TextSize = Math.Min(h * 0.4f, 14 * ctx.ScaleFactor);
                
                // Center text vertically
                var metrics = textPaint.FontMetrics;
                float textY = y + (h / 2) - (metrics.Ascent + metrics.Descent) / 2;
                
                // Truncate if too long
                string displayText = data;
                float textWidth = textPaint.MeasureText(displayText);
                if (textWidth > w - 4)
                {
                    while (displayText.Length > 3 && textPaint.MeasureText(displayText + "...") > w - 4)
                    {
                        displayText = displayText.Substring(0, displayText.Length - 1);
                    }
                    displayText += "...";
                }
                
                canvas.DrawText(displayText, x + 2, textY, textPaint);
            }
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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            // Validation
            if (!ValidateData(context.FieldData))
            {
                // Draw fallback/placeholder if invalid or empty
                 return; 
            }

            var canvas = context.Canvas;
            if (canvas == null) return;
            
            float scale = context.ScaleFactor;
            // Use Height directly (it was scaled 1:1 by removal of 0.75f) - wait, RenderContext logic was fixed.
            // Barcode commands usually take height in dots.
            // We need to scale dots to pixels.
            int h = (int)((Height > 0 ? Height : context.BarcodeHeight) * scale);

            try
            {
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions { Height = h, PureBarcode = !PrintInterpretationLine, Margin = 0 },
                    Renderer = new SKBitmapRenderer()
                };
                writer.Options.Width = (int)((context.FieldData.Length * 11 + 35) * context.ModuleWidth * scale);

                using (var bitmap = writer.Write(context.FieldData))
                {
                    // Alignment: Bars start at Y. 
                    // DrawRotated handles translation to X,Y and then rotation.
                    DrawRotated(canvas, bitmap, context.ScaledX, context.ScaledY, Orientation);
                    
                    if (PrintInterpretationLine && writer.Options.PureBarcode)
                        DrawInterpretation(canvas, context.FieldData, context.ScaledX, context.ScaledY, h, PrintInterpretationAbove, context);
                }
            }
            catch { DrawPlaceholder(canvas, context.ScaledX, context.ScaledY, 100 * scale, h, context); }
        }

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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData)) return;
            var canvas = context.Canvas;
            if (canvas == null) return;
            float scale = context.ScaleFactor;
            int h = (int)((Height > 0 ? Height : context.BarcodeHeight) * scale);

            try
            {
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.CODE_39,
                    Options = new EncodingOptions { Height = h, PureBarcode = !PrintInterpretationLine, Margin = 0 },
                    Renderer = new SKBitmapRenderer()
                };
                writer.Options.Width = (int)((context.FieldData.Length * 16) * context.ModuleWidth * scale);

                using (var bitmap = writer.Write(context.FieldData))
                {
                    DrawRotated(canvas, bitmap, context.ScaledX, context.ScaledY, Orientation);
                    if (PrintInterpretationLine && writer.Options.PureBarcode)
                        DrawInterpretation(canvas, context.FieldData, context.ScaledX, context.ScaledY, h, PrintInterpretationAbove, context);
                }
            }
            catch { DrawPlaceholder(canvas, context.ScaledX, context.ScaledY, 100 * scale, h, context); }
        }

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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            var canvas = context.Canvas;
            if (canvas == null) return;

            // EAN Validation
            if (!ValidateData(context.FieldData))
            {
                 DrawFallbackText(canvas, "INVALID EAN", context.ScaledX, context.ScaledY, 100 * context.ScaleFactor, 50 * context.ScaleFactor, context);
                 return;
            }

            float scale = context.ScaleFactor;
            int h = (int)((Height > 0 ? Height : context.BarcodeHeight) * scale);

            try
            {
                // Support EAN-13, but also generic numeric if needed. 
                // ZPL ^BE typically implies EAN-13.
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.EAN_13,
                    Options = new EncodingOptions { Height = h, PureBarcode = !PrintInterpretationLine, Margin = 0 },
                    Renderer = new SKBitmapRenderer()
                };
                writer.Options.Width = (int)(96 * context.ModuleWidth * scale);

                using (var bitmap = writer.Write(context.FieldData))
                {
                    DrawRotated(canvas, bitmap, context.ScaledX, context.ScaledY, Orientation);
                    
                    // Manual Interpretation Line if requested and PureBarcode used
                    if (PrintInterpretationLine && writer.Options.PureBarcode)
                    {
                        DrawInterpretation(canvas, context.FieldData, context.ScaledX, context.ScaledY, h, PrintInterpretationAbove, context);
                    }
                }
            }
            catch 
            { 
                DrawFallbackText(canvas, context.FieldData, context.ScaledX, context.ScaledY, 100 * scale, h, context); 
            }
        }

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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData)) return;
            var canvas = context.Canvas;
            if (canvas == null) return;
            float scale = context.ScaleFactor;
            int h = (int)((Height > 0 ? Height : context.BarcodeHeight) * scale);

            try
            {
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.CODE_93,
                    Options = new EncodingOptions { Height = h, PureBarcode = !PrintInterpretationLine, Margin = 0 },
                    Renderer = new SKBitmapRenderer()
                };
                writer.Options.Width = (int)((context.FieldData.Length * 9) * context.ModuleWidth * scale);

                using (var bitmap = writer.Write(context.FieldData))
                {
                    DrawRotated(canvas, bitmap, context.ScaledX, context.ScaledY, Orientation);
                    if (PrintInterpretationLine && writer.Options.PureBarcode)
                        DrawInterpretation(canvas, context.FieldData, context.ScaledX, context.ScaledY, h, PrintInterpretationAbove, context);
                }
            }
            catch { DrawPlaceholder(canvas, context.ScaledX, context.ScaledY, 100 * scale, h, context); }
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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData)) return;
            var canvas = context.Canvas;
            if (canvas == null) return;
            float scale = context.ScaleFactor;
            int h = (int)((Height > 0 ? Height : context.BarcodeHeight) * scale);

            try
            {
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.UPC_A,
                    Options = new EncodingOptions { Height = h, PureBarcode = !PrintInterpretationLine, Margin = 0 },
                    Renderer = new SKBitmapRenderer()
                };
                writer.Options.Width = (int)(96 * context.ModuleWidth * scale);

                using (var bitmap = writer.Write(context.FieldData))
                {
                    DrawRotated(canvas, bitmap, context.ScaledX, context.ScaledY, Orientation);
                }
            }
            catch { DrawPlaceholder(canvas, context.ScaledX, context.ScaledY, 100 * scale, h, context); }
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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData)) return;
            var canvas = context.Canvas;
            if (canvas == null) return;
            float scale = context.ScaleFactor;
            float x = context.ScaledX;
            float y = context.ScaledY;

            try
            {
                string qrData = ParseQRData(context.FieldData);
                int size = (int)(10 * Magnification * scale);
                // Ensure minimum size
                if (size < 1) size = 1;

                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new ZXing.QrCode.QrCodeEncodingOptions { ErrorCorrection = GetErrorCorrectionLevel(), Margin = 0, Width = size, Height = size },
                    Renderer = new SKBitmapRenderer()
                };
                using (var qrBitmap = writer.Write(qrData))
                {
                    DrawRotated(canvas, qrBitmap, x, y, Orientation);
                }
            }
            catch { DrawPlaceholder(canvas, x, y, 50 * Magnification * scale, 50 * Magnification * scale, context); }
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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData)) return;
            var canvas = context.Canvas;
            if (canvas == null) return;
            float scale = context.ScaleFactor;
            try
            {
                int dim = (int)(Height * 10 * scale);
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.DATA_MATRIX,
                    Options = new EncodingOptions { Margin = 0, Width = dim, Height = dim },
                    Renderer = new SKBitmapRenderer()
                };
                using (var bitmap = writer.Write(context.FieldData))
                {
                    DrawRotated(canvas, bitmap, context.ScaledX, context.ScaledY, Orientation);
                }
            }
            catch { DrawPlaceholder(canvas, context.ScaledX, context.ScaledY, 50 * scale, 50 * scale, context); }
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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData)) return;
            var canvas = context.Canvas;
            if (canvas == null) return;
            float scale = context.ScaleFactor;
            int h = (int)(Height * scale);
            try
            {
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.PDF_417,
                    Options = new EncodingOptions { Margin = 0, Height = h },
                    Renderer = new SKBitmapRenderer()
                };
                using (var bitmap = writer.Write(context.FieldData))
                {
                    DrawRotated(canvas, bitmap, context.ScaledX, context.ScaledY, Orientation);
                }
            }
            catch { DrawPlaceholder(canvas, context.ScaledX, context.ScaledY, 100 * scale, h, context); }
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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData)) return;
            var canvas = context.Canvas;
            if (canvas == null) return;
            float scale = context.ScaleFactor;
            try
            {
                int dim = (int)(50 * Magnification * scale);
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.AZTEC,
                    Options = new EncodingOptions { Margin = 0, Width = dim, Height = dim },
                    Renderer = new SKBitmapRenderer()
                };
                using (var bitmap = writer.Write(context.FieldData))
                {
                    DrawRotated(canvas, bitmap, context.ScaledX, context.ScaledY, Orientation);
                }
            }
            catch { DrawPlaceholder(canvas, context.ScaledX, context.ScaledY, 50 * scale, 50 * scale, context); }
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
            context.NextFieldRenderAction = DrawBarcode;
        }

        private void DrawBarcode(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData)) return;
            var canvas = context.Canvas;
            if (canvas == null) return;
            float scale = context.ScaleFactor;
            try
            {
                var writer = new BarcodeWriter<SKBitmap> {
                    Format = BarcodeFormat.MAXICODE,
                    Options = new EncodingOptions { Margin = 0 },
                    Renderer = new SKBitmapRenderer()
                };
                using (var bitmap = writer.Write(context.FieldData))
                {
                    float x = context.ScaledX;
                    float y = context.ScaledY;
                    var destRect = SKRect.Create(x, y, bitmap.Width * scale, bitmap.Height * scale);
                    canvas.DrawBitmap(bitmap, destRect);
                }
            }
            catch { DrawPlaceholder(canvas, context.ScaledX, context.ScaledY, 100 * scale, 100 * scale, context); }
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Mode = parts.Length > 0 ? ParseInt(parts[0], 2) : 2;
        }
    }
}
