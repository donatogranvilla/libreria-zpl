using System;
using SkiaSharp;
using ZplRenderer.Core;
using ZplRenderer.Rendering;

namespace ZplRenderer
{
    /// <summary>
    /// Main entry point for the ZPL Renderer library.
    /// Parses ZPL code and renders label previews.
    /// </summary>
    public class ZplEngine
    {
        private readonly ZplParser _parser;
        private readonly BitmapRenderer _renderer;

        /// <summary>
        /// Default DPI for rendering (203 is standard for many Zebra printers).
        /// </summary>
        public int DefaultDpi { get; set; } = 203;

        /// <summary>
        /// Default label width in dots.
        /// </summary>
        public int DefaultWidthDots { get; set; } = 812; // ~4 inches at 203 DPI

        /// <summary>
        /// Default label height in dots.
        /// </summary>
        public int DefaultHeightDots { get; set; } = 1218; // ~6 inches at 203 DPI

        /// <summary>
        /// Background color for rendered labels.
        /// </summary>
        public SKColor BackgroundColor
        {
            get => _renderer.BackgroundColor;
            set => _renderer.BackgroundColor = value;
        }

        /// <summary>
        /// Creates a new ZPL Engine instance.
        /// </summary>
        public ZplEngine()
        {
            _parser = new ZplParser();
            _renderer = new BitmapRenderer();
        }

        /// <summary>
        /// Parses and renders ZPL code to a bitmap using default dimensions.
        /// </summary>
        /// <param name="zplCode">The ZPL code to render.</param>
        /// <returns>A bitmap preview of the label.</returns>
        public SKBitmap Render(string zplCode)
        {
            return Render(zplCode, DefaultWidthDots, DefaultHeightDots, DefaultDpi);
        }

        /// <summary>
        /// Parses and renders ZPL code to a bitmap with specified dimensions in dots.
        /// </summary>
        /// <param name="zplCode">The ZPL code to render.</param>
        /// <param name="widthDots">Label width in dots.</param>
        /// <param name="heightDots">Label height in dots.</param>
        /// <param name="dpi">Printer DPI (default 203).</param>
        /// <returns>A bitmap preview of the label.</returns>
        public SKBitmap Render(string zplCode, int widthDots, int heightDots, int dpi = 203)
        {
            var label = _parser.Parse(zplCode);
            return _renderer.Render(label, widthDots, heightDots, dpi);
        }

        /// <summary>
        /// Parses and renders ZPL code to a bitmap with dimensions in millimeters.
        /// </summary>
        /// <param name="zplCode">The ZPL code to render.</param>
        /// <param name="widthMm">Label width in millimeters.</param>
        /// <param name="heightMm">Label height in millimeters.</param>
        /// <param name="dpi">Printer DPI (default 203).</param>
        /// <returns>A bitmap preview of the label.</returns>
        public SKBitmap RenderMm(string zplCode, float widthMm, float heightMm, int dpi = 203)
        {
            var label = _parser.Parse(zplCode);
            return _renderer.RenderMm(label, widthMm, heightMm, dpi);
        }

        /// <summary>
        /// Parses and renders ZPL code to a bitmap with dimensions in inches.
        /// </summary>
        /// <param name="zplCode">The ZPL code to render.</param>
        /// <param name="widthInches">Label width in inches.</param>
        /// <param name="heightInches">Label height in inches.</param>
        /// <param name="dpi">Printer DPI (default 203).</param>
        /// <returns>A bitmap preview of the label.</returns>
        public SKBitmap RenderInches(string zplCode, float widthInches, float heightInches, int dpi = 203)
        {
            var label = _parser.Parse(zplCode);
            return _renderer.RenderInches(label, widthInches, heightInches, dpi);
        }

        /// <summary>
        /// Parses ZPL code without rendering.
        /// </summary>
        /// <param name="zplCode">The ZPL code to parse.</param>
        /// <returns>The parsed label representation.</returns>
        public ZplLabel Parse(string zplCode)
        {
            return _parser.Parse(zplCode);
        }

        /// <summary>
        /// Validates ZPL code and returns any warnings or errors.
        /// </summary>
        /// <param name="zplCode">The ZPL code to validate.</param>
        /// <returns>Collection of validation messages (empty if valid).</returns>
        public System.Collections.Generic.IEnumerable<string> Validate(string zplCode)
        {
            return _parser.Validate(zplCode);
        }

        /// <summary>
        /// Renders ZPL code and saves directly to a file.
        /// </summary>
        /// <param name="zplCode">The ZPL code to render.</param>
        /// <param name="filePath">Path to save the image.</param>
        /// <param name="widthDots">Label width in dots.</param>
        /// <param name="heightDots">Label height in dots.</param>
        /// <param name="dpi">Printer DPI.</param>
        /// <param name="format">Image format (default PNG).</param>
        public void RenderToFile(string zplCode, string filePath, 
            int widthDots, int heightDots, int dpi = 203, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
        {
            using (var bitmap = Render(zplCode, widthDots, heightDots, dpi))
            {
                _renderer.SaveToFile(bitmap, filePath, format);
            }
        }

        /// <summary>
        /// Converts millimeters to dots for a given DPI.
        /// </summary>
        public static int MmToDots(float mm, int dpi = 203)
        {
            return (int)(mm * dpi / 25.4f);
        }

        /// <summary>
        /// Converts inches to dots for a given DPI.
        /// </summary>
        public static int InchesToDots(float inches, int dpi = 203)
        {
            return (int)(inches * dpi);
        }

        /// <summary>
        /// Converts dots to millimeters for a given DPI.
        /// </summary>
        public static float DotsToMm(int dots, int dpi = 203)
        {
            return dots * 25.4f / dpi;
        }
    }
}
