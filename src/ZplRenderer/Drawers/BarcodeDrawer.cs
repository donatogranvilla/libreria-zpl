using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.SkiaSharp.Rendering;
using ZplRenderer.Elements;

namespace ZplRenderer.Drawers
{
    public class BarcodeDrawer : IElementDrawer
    {
        public bool CanDraw(ZplElement element) => element is ZplBarcode;

        public void Draw(ZplElement element, DrawerContext context)
        {
            if (!(element is ZplBarcode barcode)) return;

            // Map ZPL barcode types to ZXing BarcodeFormat
            var format = BarcodeFormat.CODE_128;
            switch (barcode.BarcodeType)
            {
                case "BC": format = BarcodeFormat.CODE_128; break;
                case "B3": format = BarcodeFormat.CODE_39; break;
                case "BE": format = BarcodeFormat.EAN_13; break;
                case "BA": format = BarcodeFormat.CODE_93; break;
                case "BU": format = BarcodeFormat.UPC_A; break;
                case "BQ": format = BarcodeFormat.QR_CODE; break;
                case "BX": format = BarcodeFormat.DATA_MATRIX; break;
                case "B7": format = BarcodeFormat.PDF_417; break;
                case "B0": format = BarcodeFormat.AZTEC; break;
                case "BD": format = BarcodeFormat.MAXICODE; break;
                case "B2": format = BarcodeFormat.ITF; break;
                case "BK": format = BarcodeFormat.CODABAR; break;
                case "BM": format = BarcodeFormat.MSI; break;
                case "BP": format = BarcodeFormat.PLESSEY; break;
                default: format = BarcodeFormat.CODE_128; break;
            }

            // Prepare options
            var options = new ZXing.Common.EncodingOptions
            {
                Height = barcode.Height > 0 ? barcode.Height : 50, // Fallback height
                PureBarcode = !barcode.PrintInterpretationLine,
                Margin = 0
            };
            
            // For QR/Aztec/DataMatrix, Width/Height might be square dimensions or module size
            if (format == BarcodeFormat.QR_CODE || format == BarcodeFormat.AZTEC || format == BarcodeFormat.DATA_MATRIX)
            {
                int dim = (int)(barcode.ModuleWidth * 10); // Heuristic if not provided
                if (dim < 50) dim = 50;
                options.Width = dim;
                options.Height = dim;
            }
            else
            {
                 // Linear barcodes: Width is calculated by ZXing based on content and Scale?
                 // We setting Width usually forces a stretch. 
                 // If we want strict module width, we might need to rely on Scale or custom Renderer.
                 // For now, let ZXing auto-width or set a max width?
                 // Actually `options.Width` in ZXing often means "Image Width", not "Barcode Width".
                 // If 0, it auto-sizes.
            }

            var writer = new BarcodeWriter<SKBitmap>
            {
                Format = format,
                Options = options,
                Renderer = new SKBitmapRenderer()
            };

            try 
            {
                using (var bitmap = writer.Write(barcode.Content))
                {
                    if (bitmap == null) return;

                    context.Canvas.Save();
                    
                    // Position at X,Y
                    // Handle Rotation
                    // ZPL Rotation is around the anchor point (top-left of the barcode)
                    float x = barcode.X;
                    float y = barcode.Y;

                    // Fix for ^FT (Baseline): If origin is baseline, we must draw UP from Y.
                    // ZXing draws Top-Down from (0,0). So we shift Y up by Height.
                    if (element.OriginType == ElementOriginType.Baseline)
                    {
                        y -= options.Height;
                    }

                    context.Canvas.Translate(x, y);

                    switch (barcode.Orientation)
                    {
                        case Rendering.FieldOrientation.Rotated90:
                            context.Canvas.RotateDegrees(90);
                            // Standard Rot90 draws Left (-X) and Down (+Y).
                            // We want Right (+X) and Down (+Y) to simulate "Top-Left Origin of Bounding Box".
                            // Rotating 90 maps Y-axis to -X. To shift Right (+X), we need to move in -Y direction.
                            context.Canvas.Translate(0, -bitmap.Height);
                            break;
                        case Rendering.FieldOrientation.Inverted:
                            context.Canvas.RotateDegrees(180);
                            context.Canvas.Translate(-bitmap.Width, -bitmap.Height); // Adjust for inversion?
                             // 180 Rotation around Top-Left (0,0) moves content to (-W, -H).
                             // We want to pivot around ... center? 
                             // ZPL ^FInverted means the text is consistent but rotated 180 relative to origin.
                             // Actually ZPL rotation pivot depends on the command.
                             // For simplicity:
                             // Normal: Draw from 0,0 down-right
                             // 90: Draw from 0,0 down-left (Effective X increases, Y increases) -> Rot 90
                             // Let's stick to standard canvas rotation.
                            break;
                        case Rendering.FieldOrientation.Rotated270:
                            context.Canvas.RotateDegrees(270);
                            context.Canvas.Translate(-bitmap.Height, 0); // Approx fix
                            break;
                        case Rendering.FieldOrientation.Normal:
                        default:
                            break;
                    }

                    context.Canvas.DrawBitmap(bitmap, 0, 0);
                    
                    context.Canvas.Restore();
                }
            }
            catch (System.Exception)
            {
                // Draw error placeholder
                using (var paint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke })
                {
                    context.Canvas.DrawRect(barcode.X, barcode.Y, 50, 50, paint);
                }
            }
        }
    }
}
