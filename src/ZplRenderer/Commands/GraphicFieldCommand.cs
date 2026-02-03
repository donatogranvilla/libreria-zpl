using System;
using System.Text;
using SkiaSharp;
using ZplRenderer.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// Base class for graphic commands that parse hex data.
    /// </summary>
    public abstract class HexGraphicBaseCommand : ZplCommand
    {
        protected SKBitmap CreateBitmapFromHex(string hexData, int bytesPerRow, int totalBytes)
        {
            int width = bytesPerRow * 8;
            int height = totalBytes / bytesPerRow;

            if (width <= 0 || height <= 0 || string.IsNullOrEmpty(hexData))
                return null;

            var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            string cleanHex = CleanHexData(hexData);
                    
            int bitIndex = 0;
            // Limit loop to available data and dimensions
            int maxBits = cleanHex.Length * 4;
            
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    if (bitIndex >= maxBits) 
                    {
                        // Fill remaining with white? Or transparent?
                        // ZPL usually assumes 0s if data missing (White).
                        bitmap.SetPixel(col, row, SKColors.White);
                        continue;
                    }

                    int hexIndex = bitIndex / 4;
                    int bitOffset = 3 - (bitIndex % 4);
                    
                    int hexValue = GetHexValue(cleanHex[hexIndex]);
                    bool isBlack = ((hexValue >> bitOffset) & 1) == 1;
                    bitmap.SetPixel(col, row, isBlack ? SKColors.Black : SKColors.White);
                    
                    bitIndex++;
                }
            }
            return bitmap;
        }

        protected string CleanHexData(string data)
        {
            var sb = new StringBuilder();
            foreach (char c in data)
            {
                if (IsHexChar(c))
                    sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        private bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') || 
                   (c >= 'A' && c <= 'F') || 
                   (c >= 'a' && c <= 'f');
        }

        private int GetHexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return 0;
        }
    }

    /// <summary>
    /// ^GF - Graphic Field command. Downloads and displays graphic images.
    /// </summary>
    public class GraphicFieldCommand : HexGraphicBaseCommand
    {
        public override string CommandCode => "GF";

        public char CompressionType { get; private set; } = 'A';
        public int BinaryByteCount { get; private set; }
        public int TotalBytes { get; private set; }
        public int BytesPerRow { get; private set; }
        public string HexData { get; private set; }

        public override void Execute(RenderContext context)
        {
            // Create bitmap from hex
            var bitmap = CreateBitmapFromHex(HexData, BytesPerRow, TotalBytes);
            if (bitmap == null) return;

            var graphicElement = new ZplRenderer.Elements.ZplGraphicImage
            {
                X = context.AbsoluteX,
                Y = context.AbsoluteY,
                Bitmap = bitmap,
                Orientation = context.FieldOrientation, 
                ScaleX = 1, // ^GF usually implies 1:1 unless scaled by context? ZPL manual says it's pixel data.
                ScaleY = 1, 
                OriginType = ZplRenderer.Elements.ElementOriginType.TopLeft
            };
            
            // If scale factor was used for DPI, we might need to apply it here?
            // For now, keeping 1:1 pixel mapping.
            
            context.Elements.Add(graphicElement);
        }

        public override void Parse(string parameters)
        {
            if (string.IsNullOrEmpty(parameters)) return;

            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                CompressionType = char.ToUpperInvariant(parts[0].Trim()[0]);

            BinaryByteCount = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            TotalBytes = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
            BytesPerRow = parts.Length > 3 ? ParseInt(parts[3], 0) : 0;
            
            if (parts.Length > 4)
            {
                var dataBuilder = new StringBuilder();
                for (int i = 4; i < parts.Length; i++)
                {
                    if (i > 4) dataBuilder.Append(',');
                    dataBuilder.Append(parts[i]);
                }
                HexData = dataBuilder.ToString();
            }
        }
    }

    /// <summary>
    /// ~DG - Download Graphics. Downloads a graphic to the printer's cache.
    /// Format: ~DGd:o.x,t,w,data
    /// </summary>
    public class DownloadGraphicsCommand : HexGraphicBaseCommand
    {
        public override string CommandCode => "DG";

        public string ImageName { get; private set; }
        public int TotalBytes { get; private set; }
        public int BytesPerRow { get; private set; }
        public string HexData { get; private set; }

        public override void Execute(RenderContext context)
        {
            if (string.IsNullOrEmpty(ImageName)) return;

            try
            {
                var bitmap = CreateBitmapFromHex(HexData, BytesPerRow, TotalBytes);
                if (bitmap != null)
                {
                    context.GraphicsCache[ImageName] = bitmap;
                }
            }
            catch
            {
                // Ignore download errors
            }
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            ImageName = parts.Length > 0 ? parts[0].Trim() : "";
            // Clean ImageName (remove d: or .GRF?) 
            // Usually ZPL references it exactly. 
            // But internal storage usually ignores drive.
            // Let's keep it as is, but maybe normalize key?
            // "R:LOGO.GRF"
            
            TotalBytes = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            BytesPerRow = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
            
            if (parts.Length > 3)
            {
                var dataBuilder = new StringBuilder();
                for (int i = 3; i < parts.Length; i++)
                {
                    if (i > 3) dataBuilder.Append(',');
                    dataBuilder.Append(parts[i]);
                }
                HexData = dataBuilder.ToString();
            }
        }
    }

    /// <summary>
    /// ^XG - Recall Graphic. Recalls a graphic from cache.
    /// Format: ^XGd:o.x,mx,my
    /// </summary>
    public class RecallGraphicCommand : ZplCommand
    {
        public override string CommandCode => "XG";

        public string ImageName { get; private set; }
        public int MagnificationX { get; private set; } = 1;
        public int MagnificationY { get; private set; } = 1;

        public override void Execute(RenderContext context)
        {
            if (string.IsNullOrEmpty(ImageName)) return;

            // Try exact name, then name without drive/extension
            SKBitmap bitmap = null;
            if (!context.GraphicsCache.TryGetValue(ImageName, out bitmap))
            {
                 // Try stripping drive
                 // R:LOGO.GRF -> LOGO.GRF
                 int colon = ImageName.IndexOf(':');
                 string simpleName = colon >= 0 ? ImageName.Substring(colon + 1) : ImageName;
                 context.GraphicsCache.TryGetValue(simpleName, out bitmap);
            }

            if (bitmap == null) return;

            var graphic = new ZplRenderer.Elements.ZplGraphicImage
            {
                X = context.AbsoluteX,
                Y = context.AbsoluteY,
                Bitmap = bitmap,
                ScaleX = MagnificationX,
                ScaleY = MagnificationY,
                Orientation = context.FieldOrientation, // Should probably apply rotation if needed
                OriginType = ZplRenderer.Elements.ElementOriginType.TopLeft // Image usually TopLeft
            };

            context.Elements.Add(graphic);
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            ImageName = parts.Length > 0 ? parts[0].Trim() : "";
            MagnificationX = parts.Length > 1 ? ParseInt(parts[1], 1) : 1;
            MagnificationY = parts.Length > 2 ? ParseInt(parts[2], 1) : 1;
        }
    }

    /// <summary>
    /// ^IM - Image Move. Loads an image from storage.
    /// Format: ^IMd:o.x
    /// </summary>
    public class ImageMoveCommand : ZplCommand
    {
        public override string CommandCode => "IM";

        public string ImageName { get; private set; }

        public override void Execute(RenderContext context)
        {
            if (string.IsNullOrEmpty(ImageName)) return;

             // Try exact name, then name without drive/extension
            SKBitmap bitmap = null;
            if (!context.GraphicsCache.TryGetValue(ImageName, out bitmap))
            {
                 int colon = ImageName.IndexOf(':');
                 string simpleName = colon >= 0 ? ImageName.Substring(colon + 1) : ImageName;
                 context.GraphicsCache.TryGetValue(simpleName, out bitmap);
            }

            if (bitmap == null) return;

            var graphic = new ZplRenderer.Elements.ZplGraphicImage
            {
                X = context.AbsoluteX,
                Y = context.AbsoluteY,
                Bitmap = bitmap,
                ScaleX = 1, 
                ScaleY = 1,
                Orientation = context.FieldOrientation,
                OriginType = ZplRenderer.Elements.ElementOriginType.TopLeft
            };
            
            context.Elements.Add(graphic);
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            ImageName = parts.Length > 0 ? parts[0].Trim() : "";
        }
    }
}
