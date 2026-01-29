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
            var canvas = context.Canvas;
            if (canvas == null)
                return;

            int x = context.AbsoluteX;
            int y = context.AbsoluteY;
            int w = Width;
            int h = Height;
            int t = Thickness;

            // Determine color
            var skColor = Color == 'W' ? SKColors.White : SKColors.Black;

            // ZPL Spec for Line: If Height <= Thickness, it's a solid line (filled box)
            bool isLine = Height <= t || Width <= t;

            if (isLine)
            {
                // Draw as filled rectangle
                using (var paint = context.CreatePaint(skColor, false))
                {
                    canvas.DrawRect(x, y, w, h, paint);
                }
                return;
            }

            if (Rounding > 0)
            {
                // Draw rounded rectangle
                int radius = Rounding * (System.Math.Min(w, h) / 16); // This 1/16 factor is an approximation
                var rect = new SKRoundRect(new SKRect(x, y, x + w, y + h), radius);
                
                // If thickness is substantial, stroke might overlap.
                // Standard GB draws an outline with thickness T.
                // If it's a filled box (thickness very large), ZPL usually handles it.
                // Spec says: "When height > thickness: Render as rectangle outline"
                
                using (var paint = context.CreatePaint(skColor, true, t))
                {
                    // Stroke is centered on the path. We want the border INWARD? 
                    // ZPL usually draws border inward or centered. 
                    // Let's assume centered on the rect edge, but that expands size.
                    // Correct approach for Stroke inside: inset by t/2.
                    float offset = t / 2.0f;
                    var strokeRect = new SKRoundRect(new SKRect(x + offset, y + offset, x + w - offset, y + h - offset), radius);
                    canvas.DrawRoundRect(strokeRect, paint);
                }
            }
            else
            {
                // Rectangular Box (Outline)
                float offset = t / 2.0f;
                using (var paint = context.CreatePaint(skColor, true, t))
                {
                    canvas.DrawRect(x + offset, y + offset, w - t, h - t, paint);
                }
            }
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
            var canvas = context.Canvas;
            if (canvas == null)
                return;

            int x = context.AbsoluteX;
            int y = context.AbsoluteY;

            var skColor = Color == 'W' ? SKColors.White : SKColors.Black;
            
            using (var paint = context.CreatePaint(skColor, true, Thickness))
            {
                if (Orientation == 'L')
                {
                    // Left-leaning diagonal (top-right to bottom-left)
                    canvas.DrawLine(x + Width, y, x, y + Height, paint);
                }
                else
                {
                    // Right-leaning diagonal (top-left to bottom-right)
                    canvas.DrawLine(x, y, x + Width, y + Height, paint);
                }
            }
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
            var canvas = context.Canvas;
            if (canvas == null)
                return;

            int x = context.AbsoluteX;
            int y = context.AbsoluteY;

            var skColor = Color == 'W' ? SKColors.White : SKColors.Black;
            float radius = Diameter / 2.0f;
            float centerX = x + radius;
            float centerY = y + radius;
            
            if (Thickness >= Diameter / 2)
            {
                // Filled circle
                using (var paint = context.CreatePaint(skColor, false))
                {
                    canvas.DrawCircle(centerX, centerY, radius, paint);
                }
            }
            else
            {
                // Circle border
                using (var paint = context.CreatePaint(skColor, true, Thickness))
                {
                    canvas.DrawCircle(centerX, centerY, radius - Thickness / 2.0f, paint);
                }
            }
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
            var canvas = context.Canvas;
            if (canvas == null)
                return;

            int x = context.AbsoluteX;
            int y = context.AbsoluteY;

            var skColor = Color == 'W' ? SKColors.White : SKColors.Black;
            
            // Positioning: Use current ^FO coordinates as top-left of bounding rect
            // But we need to account for stroke width if drawing border.
            // If Shape is 'B' (Border/Oval), drawn with stroke.
            
            // Logic:
            // If Thickness is high enough, it's filled? ZPL Manual says ^GE has no fill logic usually, unless thickness is high.
            // But Shape 'B' is explicit.
            
            if (Shape == 'B') 
            {
                 // Shape B: Draw oval stroke ONLY (no fill check, per user spec)
                 float offset = Thickness / 2.0f;
                 var strokeRect = new SKRect(x + offset, y + offset, x + Width - offset, y + Height - offset);
                 using (var paint = context.CreatePaint(skColor, true, Thickness))
                 {
                     canvas.DrawOval(strokeRect, paint);
                 }
            }
            else
            {
                // Default behavior (Standard GE)
                // If thickness >= min dim / 2, fill it.
                if (Thickness >= System.Math.Min(Width, Height) / 2)
                {
                    // Filled ellipse
                    var rect = new SKRect(x, y, x + Width, y + Height);
                    using (var paint = context.CreatePaint(skColor, false))
                    {
                        canvas.DrawOval(rect, paint);
                    }
                }
                else
                {
                    // Ellipse border
                    float offset = Thickness / 2.0f;
                    var strokeRect = new SKRect(x + offset, y + offset, x + Width - offset, y + Height - offset);
                    using (var paint = context.CreatePaint(skColor, true, Thickness))
                    {
                        canvas.DrawOval(strokeRect, paint);
                    }
                }
            }
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
