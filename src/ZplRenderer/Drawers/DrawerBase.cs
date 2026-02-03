using SkiaSharp;
using ZplRenderer.Elements;

namespace ZplRenderer.Drawers
{
    /// <summary>
    /// Context passed to drawers during the rendering phase.
    /// </summary>
    public class DrawerContext
    {
        public SKCanvas Canvas { get; }
        public int DpiX { get; }
        public int DpiY { get; }

        public DrawerContext(SKCanvas canvas, int dpiX, int dpiY)
        {
            Canvas = canvas;
            DpiX = dpiX;
            DpiY = dpiY;
        }

        // Helper method to convert ^FO vs ^FT to absolute SKCanvas coordinates
        public float GetBaselineY(ZplElement element, float fontAscent)
        {
            if (element.OriginType == ElementOriginType.Baseline)
            {
                // ^FT: Y is already the baseline
                return element.Y;
            }
            else
            {
                // ^FO: Y is top-left.
                // In SkiaSharp/standard typography:
                // Baseline = Top + (-Ascent)
                // Ascent is usually negative (distance up from baseline).
                // So Top - Ascent = Baseline (e.g. 10 - (-20) = 30)
                return element.Y - fontAscent;
            }
        }
        
        public SKPoint GetDrawPosition(ZplElement element)
        {
             // For non-text elements, we usually just want the Top-Left.
             // If ^FT is used for shapes (rare but possible), it acts like ^FO (according to docs, ^FT affects text, but let's be safe).
             return new SKPoint(element.X, element.Y);
        }
    }

    /// <summary>
    /// Base interface for rendering a specific type of ZPL element.
    /// </summary>
    public interface IElementDrawer
    {
        bool CanDraw(ZplElement element);
        void Draw(ZplElement element, DrawerContext context);
    }
}
