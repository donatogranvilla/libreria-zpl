using SkiaSharp;
using System.Collections.Generic;

namespace ZplRenderer.Elements
{
    /// <summary>
    /// Defines how the position coordinates should be interpreted.
    /// </summary>
    public enum ElementOriginType
    {
        /// <summary>
        /// The X,Y coordinates represent the top-left corner of the element.
        /// Corresponds to ^FO (Field Origin).
        /// </summary>
        TopLeft,

        /// <summary>
        /// The X,Y coordinates represent the baseline origin of the text.
        /// Corresponds to ^FT (Field Typeset).
        /// </summary>
        Baseline
    }

    /// <summary>
    /// Base class for all renderable ZPL elements.
    /// </summary>
    public abstract class ZplElement
    {
        /// <summary>X position in dots.</summary>
        public float X { get; set; }

        /// <summary>Y position in dots.</summary>
        public float Y { get; set; }

        /// <summary>How the X,Y coordinates should be interpreted.</summary>
        public ElementOriginType OriginType { get; set; } = ElementOriginType.TopLeft;

        /// <summary>If true, rendering should be inverted (white on black).</summary>
        public bool IsReversePrint { get; set; }
    }

    /// <summary>
    /// Represents a text element to be drawn.
    /// </summary>
    public class ZplTextField : ZplElement
    {
        public string Text { get; set; }
        public SKFont Font { get; set; }
        public float ScaleX { get; set; } = 1.0f;
        public Rendering.FieldOrientation Orientation { get; set; }

        /// <summary>Field Block settings (if applied).</summary>
        public ZplFieldBlock FieldBlock { get; set; }
    }

    /// <summary>
    /// Represents Field Block settings (^FB).
    /// </summary>
    public class ZplFieldBlock
    {
        public int Width { get; set; }
        public int MaxLines { get; set; }
        public char Alignment { get; set; } // L, C, R, J
        public int HangingIndent { get; set; }
        public int LineSpacing { get; set; }
    }

    /// <summary>
    /// Represents a graphic box or line (^GB).
    /// </summary>
    public class ZplGraphicBox : ZplElement
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BorderThickness { get; set; }
        public SKColor LineColor { get; set; } = SKColors.Black;
    }

    /// <summary>
    /// Represents a graphic ellipse (^GE).
    /// </summary>
    public class ZplGraphicEllipse : ZplElement
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BorderThickness { get; set; }
        public SKColor LineColor { get; set; } = SKColors.Black;
        public char Shape { get; set; } = ' '; // F = Fill, B = Border
    }

    /// <summary>
    /// Represents a downloaded graphic or image (^GF, ^XG, ^IM).
    /// </summary>
    public class ZplGraphicImage : ZplElement
    {
        public SKBitmap Bitmap { get; set; }
        public int ScaleX { get; set; } = 1;
        public int ScaleY { get; set; } = 1;
        public Rendering.FieldOrientation Orientation { get; set; } = Rendering.FieldOrientation.Normal;
    }

    /// <summary>
    /// Represents a barcode.
    /// </summary>
    public class ZplBarcode : ZplElement
    {
        public string Content { get; set; }
        public string BarcodeType { get; set; } // "128", "Q", "EAN13", etc.
        public int ModuleWidth { get; set; }
        public float ModuleRatio { get; set; }
        public int Height { get; set; }
        public Rendering.FieldOrientation Orientation { get; set; }
        public bool PrintInterpretationLine { get; set; }
        public bool PrintInterpretationLineAbove { get; set; }
    }
}
