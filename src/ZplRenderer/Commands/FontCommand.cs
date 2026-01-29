using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using ZplRenderer.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// ^A - Font Selection command. Selects font and sets size.
    /// Format: ^Afo,h,w
    /// </summary>
    public class FontCommand : ZplCommand
    {
        public override string CommandCode => "A";

        /// <summary>Font name identifier (0-9, A-Z).</summary>
        public string FontName { get; private set; } = "0";

        /// <summary>Font orientation.</summary>
        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;

        /// <summary>Font height in dots.</summary>
        public int Height { get; private set; } = 30;

        /// <summary>Font width in dots (0 = proportional).</summary>
        public int Width { get; private set; } = 0;

        public override void Execute(RenderContext context)
        {
            context.UpdateFont(FontName, Height, Width, Orientation);
        }

        public override void Parse(string parameters)
        {
            if (string.IsNullOrEmpty(parameters))
                return;

            int commaIndex = parameters.IndexOf(',');
            string fontPart = commaIndex >= 0 ? parameters.Substring(0, commaIndex) : parameters;
            string remainingParams = commaIndex >= 0 ? parameters.Substring(commaIndex + 1) : "";

            if (fontPart.Length >= 1)
                FontName = fontPart[0].ToString().ToUpperInvariant();
            
            if (fontPart.Length >= 2)
                Orientation = RenderContext.ParseOrientation(fontPart[1]);

            var parts = SplitParameters(remainingParams);
            Height = parts.Length > 0 ? ParseInt(parts[0], 30) : 30;
            Width = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
        }
    }

    /// <summary>
    /// ^CF - Change Default Font command.
    /// Format: ^CFf,h,w
    /// </summary>
    public class ChangeDefaultFontCommand : ZplCommand
    {
        public override string CommandCode => "CF";

        public string FontName { get; private set; } = "0";
        public int Height { get; private set; } = 0;
        public int Width { get; private set; } = 0;

        public override void Execute(RenderContext context)
        {
            // Use current orientation as CF doesn't change it
            context.UpdateFont(FontName, Height, Width, context.FieldOrientation);
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                FontName = parts[0];
            
            Height = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            Width = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
        }
    }

    /// <summary>
    /// ^A@ - Use Scalable Font by Name.
    /// Format: ^A@o,h,w,d:f.x
    /// </summary>
    public class ScalableFontCommand : ZplCommand
    {
        public override string CommandCode => "A@";

        public FieldOrientation Orientation { get; private set; } = FieldOrientation.Normal;
        public int Height { get; private set; } = 0;
        public int Width { get; private set; } = 0;
        public string FontName { get; private set; } = "";

        public override void Execute(RenderContext context)
        {
            context.UpdateFont(FontName, Height, Width, Orientation);
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                Orientation = RenderContext.ParseOrientation(parts[0][0]);
            
            Height = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            Width = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
            
            // Font name is the last parameter description (d:f.x)
            if (parts.Length > 3)
            {
                // Might contain drive letter d:
                var fontParam = parts[3];
                int colonIndex = fontParam.IndexOf(':');
                if (colonIndex >= 0)
                    FontName = fontParam.Substring(colonIndex + 1);
                else
                    FontName = fontParam;
            }
        }
    }

    /// <summary>
    /// ^CI - Change International Font (Encoding).
    /// Format: ^CIa
    /// </summary>
    public class ChangeInternationalFontCommand : ZplCommand
    {
        public override string CommandCode => "CI";
        public int MapID { get; private set; } = 0;

        public override void Execute(RenderContext context)
        {
            // Just store it for now, implementation relies on .NET string handling
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            MapID = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
        }
    }

    /// <summary>
    /// Renders the current field data as text.
    /// Handles ^FB (wrapping), ^FT (baseline position), ^FR (reverse print).
    /// </summary>
    public class TextRenderCommand : ZplCommand
    {
        public override string CommandCode => "TEXT_RENDER";

        public override void Execute(RenderContext context)
        {
            if (string.IsNullOrEmpty(context.FieldData))
                return;

            var canvas = context.Canvas;
            if (canvas == null)
                return;

            float x = context.ScaledX;
            float y = context.ScaledY;

            canvas.Save();

            try
            {
                // Apply rotation
                if (context.FieldOrientation != FieldOrientation.Normal)
                {
                    canvas.Translate(x, y);
                    canvas.RotateDegrees((int)context.FieldOrientation);
                    x = 0;
                    y = 0;
                }

                // Prepare Paint
                using (var paint = context.CreateTextPaint(context.IsReversePrint ? SKColors.White : SKColors.Black))
                {
                    // Calculate Drawing Baseline
                    // ^FT: y is the baseline.
                    // ^FO: y is the top-left corner.
                    float baselineY;
                    if (context.IsBaselinePosition)
                    {
                        baselineY = y;
                    }
                    else
                    {
                        // ^FO: Shift down by Ascent (which is negative) to get to baseline
                        // Logic: Drawn Top = Y. Baseline is Y + Height of Ascent.
                        baselineY = y - paint.FontMetrics.Ascent;
                    }

                    // Handle Field Block (Wrapping)
                    if (context.FieldBlockWidth > 0)
                    {
                        DrawWrappedText(canvas, context.FieldData, x, baselineY, paint, context);
                    }
                    else
                    {
                        // Single line rendering
                        DrawSingleLine(canvas, context.FieldData, x, baselineY, paint, context.IsReversePrint);
                    }
                }
            }
            finally
            {
                canvas.Restore();
            }
        }

        private void DrawSingleLine(SKCanvas canvas, string text, float x, float y, SKPaint paint, bool isReverse)
        {
            if (isReverse)
            {
                // Draw black background box
                var bounds = new SKRect();
                paint.MeasureText(text, ref bounds);
                // Adjust bounds: text draws at x,y (baseline). Bounds.Top is relative to baseline (negative).
                // We want the bg to cover the full line height.
                var metrics = paint.FontMetrics;
                var bgRect = new SKRect(x, y + metrics.Ascent, x + bounds.Width + bounds.Left, y + metrics.Descent);
                
                using (var bgPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill })
                {
                    canvas.DrawRect(bgRect, bgPaint);
                }
            }
            
            canvas.DrawText(text, x, y, paint);
        }

        private void DrawWrappedText(SKCanvas canvas, string text, float x, float startY, SKPaint paint, RenderContext context)
        {
            float maxWidth = context.FieldBlockWidth * context.ScaleFactor;
            int maxLines = context.FieldBlockMaxLines;
            char align = context.FieldBlockAlignment; // L, C, R, J

            // Use context.ScaledFontHeight for line spacing to behave like ZPL
            float lineHeight = context.ScaledFontHeight;
            // Add custom line spacing if specified (FieldBlockLineSpacing not yet in context, assuming default for now)
            
            // 1. Wrap Text
            var lines = WordWrap(text, paint, maxWidth);

            // 2. Truncate Lines
            if (maxLines > 0 && lines.Count > maxLines)
            {
                lines = lines.Take(maxLines).ToList();
            }

            // 3. Draw Lines
            float currentY = startY;

            // If ^FO (Top), startY is baseline of FIRST line.
            // If ^FT (Baseline), behavior depends on ZPL specificities for blocks. 
            // Usually ^FT sets the baseline of the LAST line or the block grows up?
            // "When ^FT is used... position is relative to the baseline"
            // For now, assuming standard flow downwards from the adjusted baselineY provided by Execute.

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                float lineWidth = paint.MeasureText(line);
                float drawX = x;

                // Alignment
                switch (align)
                {
                    case 'C': // Center
                        drawX = x + (maxWidth - lineWidth) / 2;
                        break;
                    case 'R': // Right
                        drawX = x + (maxWidth - lineWidth);
                        break;
                    case 'J': // Justify - Fallback to Left for now
                    case 'L': 
                    default: 
                        drawX = x;
                        break;
                }

                if (context.IsReversePrint)
                {
                    float top = currentY + paint.FontMetrics.Ascent;
                    float bottom = currentY + paint.FontMetrics.Descent;
                    var bgRect = new SKRect(drawX, top, drawX + lineWidth, bottom);
                     using (var bgPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill })
                    {
                        canvas.DrawRect(bgRect, bgPaint);
                    }
                }

                canvas.DrawText(line, drawX, currentY, paint);
                currentY += lineHeight;
            }
        }

        private List<string> WordWrap(string text, SKPaint paint, float maxWidth)
        {
            var lines = new List<string>();
            // Handle explicit newlines
            var explicitLines = text.Split(new[] { "\\&", "\n", "\r" }, StringSplitOptions.None); 
            // ZPL uses \& for newline often, or CRLF.

            foreach (var section in explicitLines)
            {
                var words = section.Split(' ');
                var currentLine = "";

                foreach (var word in words)
                {
                    string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    float width = paint.MeasureText(testLine);

                    if (width > maxWidth)
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            lines.Add(currentLine);
                            currentLine = word;
                        }
                        else
                        {
                            // Word itself is too long, force split?
                            lines.Add(word);
                            currentLine = "";
                        }
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }

                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);
            }

            return lines;
        }

        public override void Parse(string parameters)
        {
            // No parameters
        }
    }
}
