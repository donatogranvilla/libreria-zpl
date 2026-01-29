using System;
using System.Collections.Generic;
using SkiaSharp;

namespace ZplRenderer.Rendering
{
    /// <summary>
    /// Orientation/rotation values for ZPL fields.
    /// </summary>
    public enum FieldOrientation
    {
        /// <summary>Normal orientation (0 degrees).</summary>
        Normal = 0,
        /// <summary>Rotated 90 degrees clockwise.</summary>
        Rotated90 = 90,
        /// <summary>Inverted (180 degrees).</summary>
        Inverted = 180,
        /// <summary>Rotated 270 degrees (or 90 counter-clockwise).</summary>
        Rotated270 = 270
    }

    /// <summary>
    /// Maintains rendering state during ZPL label processing.
    /// </summary>
    public class RenderContext : IDisposable
    {
        private SKTypeface _currentTypeface;
        private SKFont _currentFont;

        /// <summary>
        /// The SKCanvas object used for drawing.
        /// </summary>
        public SKCanvas Canvas { get; set; }

        /// <summary>
        /// Current field origin X position in dots.
        /// </summary>
        public int CurrentX { get; set; } = 0;

        /// <summary>
        /// Current field origin Y position in dots.
        /// </summary>
        public int CurrentY { get; set; } = 0;

        /// <summary>
        /// Label home X offset in dots.
        /// </summary>
        public int LabelHomeX { get; set; } = 0;

        /// <summary>
        /// Label home Y offset in dots.
        /// </summary>
        public int LabelHomeY { get; set; } = 0;

        /// <summary>
        /// Cache for downloaded graphics (~DG).
        /// Key: Image name (e.g. "LOGO.GRF"), Value: SkiaSharp Bitmap.
        /// </summary>
        public Dictionary<string, SKBitmap> GraphicsCache { get; } = new Dictionary<string, SKBitmap>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Horizontal DPI of the target printer.
        /// </summary>
        public int DpiX { get; set; } = 203;

        /// <summary>
        /// Vertical DPI of the target printer.
        /// </summary>
        public int DpiY { get; set; } = 203;

        /// <summary>
        /// Custom DPI override (null = use DpiX/DpiY).
        /// </summary>
        public int? CustomDpi { get; set; } = null;

        /// <summary>
        /// Effective DPI for rendering.
        /// </summary>
        public int EffectiveDpi => CustomDpi ?? DpiX;

        /// <summary>
        /// Scale factor based on 203 DPI baseline.
        /// </summary>
        public float ScaleFactor => EffectiveDpi / 203.0f;

        /// <summary>
        /// Scaled Absolute X position.
        /// </summary>
        public float ScaledX => (LabelHomeX + CurrentX) * ScaleFactor;

        /// <summary>
        /// Scaled Absolute Y position.
        /// </summary>
        public float ScaledY => (LabelHomeY + CurrentY) * ScaleFactor;

        /// <summary>
        /// Label print width in dots (set by ^PW).
        /// </summary>
        public int? PrintWidth { get; set; }

        /// <summary>
        /// Label length in dots (set by ^LL).
        /// </summary>
        public int? LabelLength { get; set; }

        /// <summary>
        /// Media darkness level (-30 to 30) (set by ^MD).
        /// </summary>
        public int MediaDarkness { get; set; } = 0;

        /// <summary>
        /// Print speed (set by ^PR).
        /// </summary>
        public string PrintSpeed { get; set; } = "A";

        /// <summary>
        /// Current font height in dots.
        /// </summary>
        public int FontHeight { get; set; } = 30;

        /// <summary>
        /// Scaled font height.
        /// </summary>
        public float ScaledFontHeight => FontHeight * ScaleFactor;

        /// <summary>
        /// Current font width in dots (0 = auto).
        /// </summary>
        public int FontWidth { get; set; } = 0;

        /// <summary>
        /// Current field orientation.
        /// </summary>
        public FieldOrientation FieldOrientation { get; set; } = FieldOrientation.Normal;

        /// <summary>
        /// Default barcode module width in dots.
        /// </summary>
        public int ModuleWidth { get; set; } = 2;

        /// <summary>
        /// Default barcode module ratio (wide to narrow).
        /// </summary>
        public float ModuleRatio { get; set; } = 3.0f;

        /// <summary>
        /// Default barcode height in dots.
        /// </summary>
        public int BarcodeHeight { get; set; } = 100;

        /// <summary>
        /// Pending field data (set by ^FD, used by next printable element).
        /// </summary>
        public string FieldData { get; set; }

        /// <summary>
        /// Hexadecimal indicator char set by ^FH. Null if disabled for current field.
        /// </summary>
        public char? HexReferenceIndicator { get; set; } = null;

        /// <summary>
        /// Current ZPL font name (0-9, A-Z).
        /// </summary>
        public string ZplFontName { get; set; } = "0";

        /// <summary>
        /// Field Block width in dots (0 = disabled). Set by ^FB.
        /// </summary>
        public int FieldBlockWidth { get; set; } = 0;

        /// <summary>
        /// Field Block max lines.
        /// </summary>
        public int FieldBlockMaxLines { get; set; } = 1;

        /// <summary>
        /// Field Block text alignment (L, C, R, J).
        /// </summary>
        public char FieldBlockAlignment { get; set; } = 'L';

        /// <summary>
        /// Field Typeset (baseline) positioning mode. Set by ^FT.
        /// </summary>
        public bool IsBaselinePosition { get; set; } = false;

        /// <summary>
        /// Field Reverse Print (white on black). Set by ^FR.
        /// </summary>
        public bool IsReversePrint { get; set; } = false;

        /// <summary>
        /// Action to render the next field (e.g. Barcode).
        /// If null, default Text rendering is used.
        /// </summary>
        public Action<RenderContext> NextFieldRenderAction { get; set; }

        /// <summary>
        /// Gets the absolute X position including label home offset.
        /// </summary>
        public int AbsoluteX => LabelHomeX + CurrentX;

        /// <summary>
        /// Gets the absolute Y position including label home offset.
        /// </summary>
        public int AbsoluteY => LabelHomeY + CurrentY;

        /// <summary>
        /// Current SKFont for text rendering.
        /// </summary>
        public SKFont CurrentFont
        {
            get
            {
                if (_currentFont == null)
                {
                    _currentTypeface = SKTypeface.FromFamilyName("Arial");
                     if (_currentTypeface == null) _currentTypeface = SKTypeface.FromFamilyName("Segoe UI");
                     if (_currentTypeface == null) _currentTypeface = SKTypeface.Default;

                    _currentFont = new SKFont(_currentTypeface, ScaledFontHeight); // 1:1 mapping for ZPL dots
                }
                return _currentFont;
            }
        }

        /// <summary>
        /// Updates the current font based on ZPL font settings.
        /// </summary>
        public void UpdateFont(string fontName, int height, int width, FieldOrientation orientation)
        {
            ZplFontName = fontName;
            FontHeight = height > 0 ? height : FontHeight;
            FieldOrientation = orientation;

            // Get font info for aspect ratio calculation
            var fontInfo = Utils.FontMappings.GetFontInfo(fontName);
            
            // If width not specified, calculate from aspect ratio
            if (width > 0)
            {
                FontWidth = width;
            }
            else if (height > 0)
            {
                FontWidth = (int)(height * fontInfo.AspectRatio);
            }

            _currentTypeface?.Dispose();
            _currentFont?.Dispose();
            _currentTypeface = null;
            _currentFont = null;
            
            // Create typeface with appropriate style
            var style = fontInfo.IsBold ? SKFontStyle.Bold : SKFontStyle.Normal;
            _currentTypeface = SKTypeface.FromFamilyName(fontInfo.FontFamily, style);
            if (_currentTypeface == null) _currentTypeface = SKTypeface.FromFamilyName("Arial");
            if (_currentTypeface == null) _currentTypeface = SKTypeface.Default;
        }

        /// <summary>
        /// Parses orientation character to FieldOrientation enum.
        /// </summary>
        public static FieldOrientation ParseOrientation(char orientationChar)
        {
            switch (char.ToUpperInvariant(orientationChar))
            {
                case 'R': return FieldOrientation.Rotated90;
                case 'I': return FieldOrientation.Inverted;
                case 'B': return FieldOrientation.Rotated270;
                case 'N':
                default: return FieldOrientation.Normal;
            }
        }

        /// <summary>
        /// Creates a paint for drawing with the specified color.
        /// </summary>
        public SKPaint CreatePaint(SKColor color, bool isStroke = false, float strokeWidth = 1)
        {
            return new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = isStroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill,
                StrokeWidth = strokeWidth
            };
        }

        /// <summary>
        /// Creates a text paint for drawing text.
        /// </summary>
        public SKPaint CreateTextPaint(SKColor color)
        {
            var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Typeface = CurrentFont.Typeface,
                TextSize = ScaledFontHeight // Use scaled height directly
            };

            // Ensure visible color if not reverse print
            if (!IsReversePrint && (color == SKColors.Transparent || color.Alpha == 0))
            {
                paint.Color = SKColors.Black;
            }

            return paint;
        }

        public void Dispose()
        {
            _currentFont?.Dispose();
            _currentTypeface?.Dispose();
        }
    }
}
