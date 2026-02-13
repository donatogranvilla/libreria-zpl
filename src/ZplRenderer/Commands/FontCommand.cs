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
            context.EncodingId = MapID;
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

}
