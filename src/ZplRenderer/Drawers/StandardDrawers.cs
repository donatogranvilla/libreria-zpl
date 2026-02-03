using SkiaSharp;
using System;
using System.Collections.Generic;
using ZplRenderer.Elements;

namespace ZplRenderer.Drawers
{
    public class TextFieldDrawer : IElementDrawer
    {
        public bool CanDraw(ZplElement element) => element is ZplTextField;

        public void Draw(ZplElement element, DrawerContext context)
        {
            if (!(element is ZplTextField textField)) return;

            using (var paint = new SKPaint())
            {
                // Setup Font
                paint.Typeface = textField.Font.Typeface;
                paint.TextSize = textField.Font.Size;
                
                // Font-specific adjustment
                // ZPL Font "0" is CG Triumvirate Bold Condensed. Arial is wider.
                // Apply a narrowing factor for Font "0" if using fallback.
                float fontAdjustment = 1.0f;
                // Currently context is not passed, but we can guess based on Typeface? 
                // Better to just apply heuristic for now or pass font name in ZplTextField.
                // Assuming typical Arial fallback:
                fontAdjustment = 0.70f; 

                paint.TextScaleX = textField.ScaleX * fontAdjustment; 
                paint.IsAntialias = true;
                paint.Color = textField.IsReversePrint ? SKColors.White : SKColors.Black;
                
                // ZPL fonts are often slightly bolder or handled differently.
                // Assuming Font.Typeface is already set up correctly by the Context/Command.

                var lines = new List<string> { textField.Text };
                
                // Handle Field Block (^FB) wrapping
                float xOffset = 0;
                float yOffset = 0;
                float width = 0;
                char align = 'L';

                if (textField.FieldBlock != null)
                {
                    width = textField.FieldBlock.Width;
                    align = textField.FieldBlock.Alignment;
                    // TODO: Implement proper word wrapping based on width
                    // For now, we assume simple lines or just respect the alignment
                }

                // Determine line height (approximate if not provided)
                float lineHeight = paint.FontSpacing;
                // if (textField.Font.LineHeight > 0) ... invalid property

                int lineIndex = 0;
                foreach (var line in lines)
                {
                   float textWidth = paint.MeasureText(line);
                   
                   // Calculate X based on alignment
                   float lineXOffset = 0;
                   if (width > 0)
                   {
                       switch (align)
                       {
                           case 'C': lineXOffset = (width - textWidth) / 2; break;
                           case 'R': lineXOffset = width - textWidth; break;
                           case 'L': 
                           default: lineXOffset = 0; break;
                       }
                   }

                   // Fix Coordinate System (^FT vs ^FO)
                   // SKPaint.DrawText(x,y) uses Baseline by default.
                   // GetBaselineY handles the conversion.
                   // For multi-line, we add lineHeight * lineIndex
                   float drawX = textField.X + lineXOffset;
                   
                   // Initial Y
                   float baseY = context.GetBaselineY(textField, paint.FontMetrics.Ascent);
                   float drawY = baseY + (lineHeight * lineIndex);

                   context.Canvas.DrawText(line, drawX, drawY, paint);
                   lineIndex++;
                }
            }
        }
    }

    public class GraphicBoxDrawer : IElementDrawer
    {
        public bool CanDraw(ZplElement element) => element is ZplGraphicBox;

        public void Draw(ZplElement element, DrawerContext context)
        {
            if (!(element is ZplGraphicBox box)) return;

            var paint = new SKPaint
            {
                Color = box.LineColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = box.BorderThickness,
                IsAntialias = false 
            };
            
            // Check for filled box (Width <= Thickness OR Height <= Thickness)
            if (box.Width <= box.BorderThickness || box.Height <= box.BorderThickness)
            {
                paint.Style = SKPaintStyle.Fill;
                context.Canvas.DrawRect(box.X, box.Y, box.Width, box.Height, paint);
            }
            else
            {
                // Outline
                // Stroke centered. Inset to keep within bounds.
                var rect = SKRect.Create(box.X, box.Y, box.Width, box.Height);
                float halfStroke = box.BorderThickness / 2.0f;
                rect.Inflate(-halfStroke, -halfStroke); 
                context.Canvas.DrawRect(rect, paint);
            }
        }
    }
    
    public class GraphicEllipseDrawer : IElementDrawer
    {
        public bool CanDraw(ZplElement element) => element is ZplGraphicEllipse;

        public void Draw(ZplElement element, DrawerContext context)
        {
            if (!(element is ZplGraphicEllipse ellipse)) return;
            
            var paint = new SKPaint
            {
                Color = ellipse.LineColor,
                Style = SKPaintStyle.Stroke, 
                StrokeWidth = ellipse.BorderThickness,
                IsAntialias = true
            };
            
            // Heuristic for Fill vs Stroke (ZPL rule: if thickness is large enough, it fills)
             if (ellipse.BorderThickness >= System.Math.Min(ellipse.Width, ellipse.Height) / 2)
            {
                paint.Style = SKPaintStyle.Fill;
            }
             
             // Explicit Shape override
             if (ellipse.Shape == 'F') paint.Style = SKPaintStyle.Fill;
             else if (ellipse.Shape == 'B') paint.Style = SKPaintStyle.Stroke;

            var rect = SKRect.Create(ellipse.X, ellipse.Y, ellipse.Width, ellipse.Height);
            
            if (paint.Style == SKPaintStyle.Stroke)
            {
                float halfStroke = ellipse.BorderThickness / 2.0f;
                rect.Inflate(-halfStroke, -halfStroke);
            }
            
            context.Canvas.DrawOval(rect, paint);
        }
    }

    public class GraphicImageDrawer : IElementDrawer
    {
        public bool CanDraw(ZplElement element) => element is ZplGraphicImage;

        public void Draw(ZplElement element, DrawerContext context)
        {
            if (!(element is ZplGraphicImage img)) return;
            if (img.Bitmap == null) return;

            context.Canvas.Save();
            
            // Position
            context.Canvas.Translate(img.X, img.Y);
            
            // Rotation
            if (img.Orientation != Rendering.FieldOrientation.Normal)
            {
                 // TODO: Handle rotation correctly (pivot around center? Or Top-Left?)
                 // ZPL usually rotates around the anchor point.
                 context.Canvas.RotateDegrees((int)img.Orientation);
            }
            
            // Draw
            context.Canvas.DrawBitmap(img.Bitmap, 0, 0);
            
            context.Canvas.Restore();
        }
    }
}
