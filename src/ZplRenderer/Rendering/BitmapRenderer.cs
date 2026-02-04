using System;
using SkiaSharp;
using ZplRenderer.Core;

namespace ZplRenderer.Rendering
{
    /// <summary>
    /// Interface for ZPL label renderers.
    /// </summary>
    public interface IZplRenderer
    {
        /// <summary>
        /// Renders a ZPL label to a bitmap.
        /// </summary>
        SKBitmap Render(ZplLabel label, int widthDots, int heightDots, int dpi = 203);

        /// <summary>
        /// Renders a ZPL label to a bitmap with millimeter dimensions.
        /// </summary>
        SKBitmap RenderMm(ZplLabel label, float widthMm, float heightMm, int dpi = 203);
    }

    /// <summary>
    /// Renders ZPL labels to bitmaps using SkiaSharp.
    /// </summary>
    public class BitmapRenderer : IZplRenderer
    {
        /// <summary>
        /// Background color for the label (default white).
        /// </summary>
        public SKColor BackgroundColor { get; set; } = SKColors.White;

        /// <summary>
        /// Renders a ZPL label to a bitmap.
        /// </summary>
        public SKBitmap Render(ZplLabel label, int widthDots, int heightDots, int dpi = 203)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));

            // Use dots directly as pixels for now (1:1 mapping)
            int pixelWidth = Math.Max(widthDots, 1);
            int pixelHeight = Math.Max(heightDots, 1);

            var bitmap = new SKBitmap(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);

            // 1. Build Model (Execute Commands)
            var elements = new System.Collections.Generic.List<ZplRenderer.Elements.ZplElement>();
            PrintOrientation printOrientation = PrintOrientation.Normal;

            using (var context = new RenderContext
            {
                // Canvas is no longer used during command execution
                DpiX = dpi,
                DpiY = dpi,
                LabelHomeX = label.LabelHomeX,
                LabelHomeY = label.LabelHomeY
            })
            {
                foreach (var command in label.Commands)
                {
                    try
                    {
                        command.Execute(context);
                    }
                    catch (Exception)
                    {
                        // Log error
                    }
                }
                elements.AddRange(context.Elements);
                printOrientation = context.PrintOrientation;
            }

            // 2. Render Model (Draw Elements)
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(BackgroundColor);

                if (printOrientation == PrintOrientation.Inverted)
                {
                    canvas.RotateDegrees(180, pixelWidth / 2f, pixelHeight / 2f);
                }
                
                var elementRenderer = new ElementRenderer();
                elementRenderer.Render(elements, canvas, dpi, dpi);
            }

            return bitmap;
        }

        /// <summary>
        /// Renders a ZPL label to a bitmap with millimeter dimensions.
        /// </summary>
        public SKBitmap RenderMm(ZplLabel label, float widthMm, float heightMm, int dpi = 203)
        {
            // Convert mm to dots: dots = mm * dpi / 25.4
            int widthDots = (int)(widthMm * dpi / 25.4f);
            int heightDots = (int)(heightMm * dpi / 25.4f);

            return Render(label, widthDots, heightDots, dpi);
        }

        /// <summary>
        /// Renders a ZPL label to a bitmap with inch dimensions.
        /// </summary>
        public SKBitmap RenderInches(ZplLabel label, float widthInches, float heightInches, int dpi = 203)
        {
            // Convert inches to dots: dots = inches * dpi
            int widthDots = (int)(widthInches * dpi);
            int heightDots = (int)(heightInches * dpi);

            return Render(label, widthDots, heightDots, dpi);
        }

        /// <summary>
        /// Saves a rendered bitmap to a file.
        /// </summary>
        public void SaveToFile(SKBitmap bitmap, string filePath, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(format, quality))
            using (var stream = System.IO.File.OpenWrite(filePath))
            {
                data.SaveTo(stream);
            }
        }
    }
}
