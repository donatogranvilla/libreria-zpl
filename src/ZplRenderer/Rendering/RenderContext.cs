using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using ZplRenderer.Elements;

namespace ZplRenderer.Rendering
{
    /// <summary>
    /// Orientation/rotation values for ZPL fields.
    /// </summary>
    public enum FieldOrientation
    {
        Normal = 0,
        Rotated90 = 90,
        Inverted = 180,
        Rotated270 = 270
    }

    /// <summary>
    /// Print orientation for the entire label (^PO).
    /// </summary>
    public enum PrintOrientation
    {
        Normal,
        Inverted // 180 degrees
    }

    /// <summary>
    /// Maintains parsing state and accumulates elements during ZPL processing.
    /// This context is used during the Command Execution phase to build the ZplElement model.
    /// </summary>
    public class RenderContext : IDisposable
    {
        private SKTypeface _currentTypeface;
        private SKFont _currentFont;

        /// <summary>
        /// The list of elements parsed so far.
        /// </summary>
        public List<ZplElement> Elements { get; } = new List<ZplElement>();

        /// <summary>
        /// Pending barcode configuration. If set, the next ^FD will generate a barcode element using this config.
        /// </summary>
        public ZplElement PendingBarcode { get; set; }

        /// <summary>
        /// Current field origin X position in dots.
        /// </summary>
        public int CurrentX { get; set; } = 0;

        /// <summary>
        /// Current field origin Y position in dots.
        /// </summary>
        public int CurrentY { get; set; } = 0;

        /// <summary>
        /// Label home X offset in dots (^LH).
        /// </summary>
        public int LabelHomeX { get; set; } = 0;

        /// <summary>
        /// Label home Y offset in dots (^LH).
        /// </summary>
        public int LabelHomeY { get; set; } = 0;

        /// <summary>
        /// Label shift X offset in dots (^LS).
        /// Shifts all field positions.
        /// </summary>
        public int LabelShiftX { get; set; } = 0;

        /// <summary>
        /// Label top offset in dots (^LT).
        /// Moves the entire label content vertically.
        /// </summary>
        public int LabelTop { get; set; } = 0;

        /// <summary>
        /// Cache for downloaded graphics (~DG).
        /// Key: Image name, Value: SkiaSharp Bitmap.
        /// </summary>
        public Dictionary<string, SKBitmap> GraphicsCache { get; } = new Dictionary<string, SKBitmap>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Horizontal DPI (state only, used for sizing/scaling logic if needed).
        /// </summary>
        public int DpiX { get; set; } = 203;
        public int DpiY { get; set; } = 203;

        // Scaling factors (keeping 1:1 for ZPL dots to pixels)
        public float ScaleFactor => 1.0f; 

        public int? PrintWidth { get; set; }
        public int? LabelLength { get; set; }
        public int MediaDarkness { get; set; } = 0;
        public string PrintSpeed { get; set; } = "A";

        /// <summary>
        /// Global print orientation (^PO).
        /// </summary>
        public PrintOrientation PrintOrientation { get; set; } = PrintOrientation.Normal;

        /// <summary>
        /// Current code page / encoding map ID (^CI).
        /// Default 0 (USA 1).
        /// </summary>
        public int EncodingId { get; set; } = 0;

        public int FontHeight { get; set; } = 30;
        public int FontWidth { get; set; } = 0;
        public FieldOrientation FieldOrientation { get; set; } = FieldOrientation.Normal;

        // Barcode Defaults
        public int ModuleWidth { get; set; } = 2;
        public float ModuleRatio { get; set; } = 3.0f;
        public int BarcodeHeight { get; set; } = 100;

        // Field Data State
        public string FieldData { get; set; }
        public char? HexReferenceIndicator { get; set; } = null;

        // Font State
        public string ZplFontName { get; set; } = "0";

        // Field Block State (^FB)
        public int FieldBlockWidth { get; set; } = 0;
        public int FieldBlockMaxLines { get; set; } = 1;
        public char FieldBlockAlignment { get; set; } = 'L';

        // Positioning State
        public bool IsBaselinePosition { get; set; } = false; // True for ^FT, False for ^FO
        public bool IsReversePrint { get; set; } = false;

        public int AbsoluteX => LabelHomeX + LabelShiftX + CurrentX;
        public int AbsoluteY => LabelHomeY + LabelTop + CurrentY;

        /// <summary>
        /// Gets the current encoding based on EncodingId (^CI).
        /// </summary>
        public Encoding GetEncoding()
        {
            // Simplified mapping for common ZPL encodings
            switch (EncodingId)
            {
                case 27: // AS/400
                case 28: // Unicode (UTF-8)
                case 29: // Unicode (Big Endian)
                case 30: // Unicode (Little Endian)
                case 13: // Zebra Code Page 850 (Multilingual)
                    return Encoding.UTF8; // Approximating to UTF8 for simplicity where compatible
                case 0: // USA 1 (cp437 typically)
                case 1: // USA 2
                case 2: // UK
                default:
                    // Fallback to UTF8 as modern ZPL usually handles it, 
                    // or ASCII. But C# strings are Unicode. 
                    // When interpreting HEX bytes, we need the right codepage.
                    // For now, default to UTF8 as it covers most "bring to state of the art" needs.
                    return Encoding.UTF8;
            }
        }

        public SKFont CurrentFont
        {
            get
            {
                if (_currentFont == null)
                {
                    _currentTypeface = SKTypeface.FromFamilyName("Arial");
                     if (_currentTypeface == null) _currentTypeface = SKTypeface.FromFamilyName("Segoe UI");
                     if (_currentTypeface == null) _currentTypeface = SKTypeface.Default;

                    _currentFont = new SKFont(_currentTypeface, FontHeight);
                }
                return _currentFont;
            }
        }

        public void UpdateFont(string fontName, int height, int width, FieldOrientation orientation)
        {
            ZplFontName = fontName;
            FontHeight = height > 0 ? height : FontHeight;
            FieldOrientation = orientation;

            // Simplified Font creation logic
             var fontInfo = Utils.FontMappings.GetFontInfo(fontName);
            
            if (width > 0) FontWidth = width;

            // Do NOT dispose current typeface/font here as they are referenced by created Elements!
            // _currentTypeface?.Dispose();
            // _currentFont?.Dispose();
            _currentTypeface = null;
            _currentFont = null;
            
            var style = fontInfo.IsBold ? SKFontStyle.Bold : SKFontStyle.Normal;
            _currentTypeface = SKTypeface.FromFamilyName(fontInfo.FontFamily, style);
            if (_currentTypeface == null) _currentTypeface = SKTypeface.FromFamilyName("Arial"); // Fallback
        }

        public static FieldOrientation ParseOrientation(char orientationChar)
        {
            switch (char.ToUpperInvariant(orientationChar))
            {
                case 'R': return FieldOrientation.Rotated90;  // Rotate 90° clockwise
                case 'I': return FieldOrientation.Inverted;   // Rotate 180°
                case 'B': return FieldOrientation.Rotated270;  // Bottom-to-top (270°)
                case 'N':
                default: return FieldOrientation.Normal;
            }
        }

        public void Dispose()
        {
            _currentFont?.Dispose();
            _currentTypeface?.Dispose();
        }
    }
}
