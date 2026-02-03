using SkiaSharp;
using ZplRenderer.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// ^GB - Graphic Box command. Draws a box or line.
    /// Format: ^GBw,h,t,c,r
    /// w = width in dots
    /// h = height in dots  
    /// t = border thickness in dots
    /// c = line color (B=black, W=white)
    /// r = degree of corner rounding (0-8)
    /// </summary>
    public class GraphicBoxCommand : ZplCommand
    {
        public override string CommandCode => "GB";

        /// <summary>Box width in dots.</summary>
        public int Width { get; private set; } = 1;

        /// <summary>Box height in dots.</summary>
        public int Height { get; private set; } = 1;

        /// <summary>Border thickness in dots.</summary>
        public int Thickness { get; private set; } = 1;

        /// <summary>Line color (B=black, W=white).</summary>
        public char Color { get; private set; } = 'B';

        /// <summary>Corner rounding (0-8).</summary>
        public int Rounding { get; private set; } = 0;

        public override void Execute(RenderContext context)
        {
            var box = new ZplRenderer.Elements.ZplGraphicBox
            {
                X = context.AbsoluteX,
                Y = context.AbsoluteY,
                Width = Width,
                Height = Height,
                BorderThickness = Thickness,
                LineColor = Color == 'W' ? SKColors.White : SKColors.Black,
                OriginType = ZplRenderer.Elements.ElementOriginType.TopLeft,
                 // TODO: Rounding support in ZplGraphicBox? For now ignoring rounding as basic box.
            };

            context.Elements.Add(box);
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            
            Width = parts.Length > 0 ? ParseInt(parts[0], 1) : 1;
            Height = parts.Length > 1 ? ParseInt(parts[1], 1) : 1;
            Thickness = parts.Length > 2 ? ParseInt(parts[2], 1) : 1;
            Color = parts.Length > 3 ? ParseChar(parts[3], 'B') : 'B';
            Rounding = parts.Length > 4 ? ParseInt(parts[4], 0) : 0;

            // Minimum values
            if (Width < 1) Width = 1;
            if (Height < 1) Height = 1;
            if (Thickness < 1) Thickness = 1;
        }
    }

    /// <summary>
    /// ^GD - Graphic Diagonal Line command.
    /// Format: ^GDw,h,t,c,o
    /// </summary>
    public class GraphicDiagonalCommand : ZplCommand
    {
        public override string CommandCode => "GD";

        public int Width { get; private set; } = 1;
        public int Height { get; private set; } = 1;
        public int Thickness { get; private set; } = 1;
        public char Color { get; private set; } = 'B';
        public char Orientation { get; private set; } = 'R'; // R=right, L=left

        public override void Execute(RenderContext context)
        {
            // Pending implementation of ZplGraphicDiagonal in Elements
            // For now, skipping or logging unsupported
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            
            Width = parts.Length > 0 ? ParseInt(parts[0], 1) : 1;
            Height = parts.Length > 1 ? ParseInt(parts[1], 1) : 1;
            Thickness = parts.Length > 2 ? ParseInt(parts[2], 1) : 1;
            Color = parts.Length > 3 ? ParseChar(parts[3], 'B') : 'B';
            Orientation = parts.Length > 4 ? ParseChar(parts[4], 'R') : 'R';
        }
    }

    /// <summary>
    /// ^GC - Graphic Circle command.
    /// Format: ^GCd,t,c
    /// </summary>
    public class GraphicCircleCommand : ZplCommand
    {
        public override string CommandCode => "GC";

        public int Diameter { get; private set; } = 1;
        public int Thickness { get; private set; } = 1;
        public char Color { get; private set; } = 'B';

        public override void Execute(RenderContext context)
        {
            // Treat as Ellipse with Same W/H
            var ellipse = new ZplRenderer.Elements.ZplGraphicEllipse
            {
                X = context.AbsoluteX,
                Y = context.AbsoluteY,
                Width = Diameter,
                Height = Diameter,
                BorderThickness = Thickness,
                LineColor = Color == 'W' ? SKColors.White : SKColors.Black,
                OriginType = ZplRenderer.Elements.ElementOriginType.TopLeft,
                Shape = ' ' // Default
            };
            context.Elements.Add(ellipse);
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            
            Diameter = parts.Length > 0 ? ParseInt(parts[0], 1) : 1;
            Thickness = parts.Length > 1 ? ParseInt(parts[1], 1) : 1;
            Color = parts.Length > 2 ? ParseChar(parts[2], 'B') : 'B';
        }
    }

    /// <summary>
    /// ^GE - Graphic Ellipse command.
    /// Format: ^GEw,h,t,c
    /// </summary>
    public class GraphicEllipseCommand : ZplCommand
    {
        public override string CommandCode => "GE";

        public int Width { get; private set; } = 1;
        public int Height { get; private set; } = 1;
        public int Thickness { get; private set; } = 1;
        public char Color { get; private set; } = 'B';
        public char Shape { get; private set; } = ' '; // Added Shape parameter

        public override void Execute(RenderContext context)
        {
             var ellipse = new ZplRenderer.Elements.ZplGraphicEllipse
            {
                X = context.AbsoluteX,
                Y = context.AbsoluteY,
                Width = Width,
                Height = Height,
                BorderThickness = Thickness,
                LineColor = Color == 'W' ? SKColors.White : SKColors.Black,
                OriginType = ZplRenderer.Elements.ElementOriginType.TopLeft,
                Shape = Shape
            };
            context.Elements.Add(ellipse);
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            
            Width = parts.Length > 0 ? ParseInt(parts[0], 1) : 1;
            Height = parts.Length > 1 ? ParseInt(parts[1], 1) : 1;
            Thickness = parts.Length > 2 ? ParseInt(parts[2], 1) : 1;
            Color = parts.Length > 3 ? ParseChar(parts[3], 'B') : 'B';
            // Parse Shape if present (5th parameter)
            if (parts.Length > 4)
            {
                 string s = parts[4].Trim();
                 if (s.Length > 0) Shape = char.ToUpperInvariant(s[0]);
            }

            if (Width < 1) Width = 1;
            if (Height < 1) Height = 1;
            if (Thickness < 1) Thickness = 1;
        }
    }
}
