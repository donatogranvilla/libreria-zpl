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

            bool is2D = format == BarcodeFormat.QR_CODE || 
                        format == BarcodeFormat.AZTEC || 
                        format == BarcodeFormat.DATA_MATRIX ||
                        format == BarcodeFormat.PDF_417 ||
                        format == BarcodeFormat.MAXICODE;

            int moduleWidth = barcode.ModuleWidth > 0 ? barcode.ModuleWidth : 2;
            int barcodeHeight = barcode.Height > 0 ? barcode.Height : 50;

            // Prepare content — strip ZPL-specific prefixes
            string content = barcode.Content ?? "";

            if (format == BarcodeFormat.QR_CODE)
            {
                // ZPL QR ^FD format: "QA,data" or "MM,data" or "MA,data"
                // First char = error correction override (Q/H/M/L), second char = input mode (A=auto, M=manual)
                // The actual data starts after the comma
                if (content.Length > 2 && content[2] == ',')
                {
                    content = content.Substring(3);
                }
            }
            else if (format == BarcodeFormat.CODE_128)
            {
                // Strip ZPL Code128 subset prefixes like >: (start with C), >9 (start with B), etc.
                content = System.Text.RegularExpressions.Regex.Replace(content, @">\s*[0-9:;]", "");
            }

            // Prepare options
            var options = new ZXing.Common.EncodingOptions
            {
                PureBarcode = !barcode.PrintInterpretationLine,
                Margin = 0
            };

            // QR Code: set error correction level hint  
            if (format == BarcodeFormat.QR_CODE)
            {
                ZXing.QrCode.Internal.ErrorCorrectionLevel ecLevel;
                switch (char.ToUpperInvariant(barcode.ErrorCorrectionLevel))
                {
                    case 'H': ecLevel = ZXing.QrCode.Internal.ErrorCorrectionLevel.H; break;
                    case 'Q': ecLevel = ZXing.QrCode.Internal.ErrorCorrectionLevel.Q; break;
                    case 'L': ecLevel = ZXing.QrCode.Internal.ErrorCorrectionLevel.L; break;
                    default:  ecLevel = ZXing.QrCode.Internal.ErrorCorrectionLevel.M; break;
                }
                options.Hints[ZXing.EncodeHintType.ERROR_CORRECTION] = ecLevel;
            }

            if (is2D)
            {
                // Per barcode 2D: generiamo alla risoluzione minima (1px per modulo)
                // e poi scaliamo con il fattore di magnificazione.
                // Non impostiamo Width/Height — ZXing calcola la dimensione minima.
            }
            else
            {
                // Per barcode lineari: generiamo a risoluzione minima (1px per modulo).
                options.Height = barcodeHeight;
            }

            var writer = new BarcodeWriter<SKBitmap>
            {
                Format = format,
                Options = options,
                Renderer = new SKBitmapRenderer()
            };

            try 
            {
                using (var bitmap = writer.Write(content))
                {
                    if (bitmap == null) return;


                    context.Canvas.Save();
                    
                    float x = barcode.X;
                    float y = barcode.Y;

                    // Per barcode lineari: calcoliamo la dimensione target rispettando ModuleWidth.
                    // ZXing genera con 1px per modulo minimo, poi noi scaliamo con ModuleWidth.
                    float drawWidth, drawHeight;

                    if (is2D)
                    {
                        // Per 2D: scaliamo il bitmap per il fattore di magnificazione.
                        // Il bitmap ZXing ha ~1px per modulo, moltiplichiamo per moduleWidth.
                        drawWidth = bitmap.Width * moduleWidth;
                        drawHeight = bitmap.Height * moduleWidth;
                    }
                    else
                    {
                        // La larghezza del bitmap ZXing rappresenta il numero di moduli.
                        // Scaliamo per il ModuleWidth ZPL specificato da ^BY.
                        drawWidth = bitmap.Width * moduleWidth;
                        drawHeight = barcodeHeight;
                    }

                    // Fix per ^FT (Baseline): il barcode si disegna verso l'alto da Y.
                    if (element.OriginType == ElementOriginType.Baseline)
                    {
                        y -= drawHeight;
                    }

                    context.Canvas.Translate(x, y);

                    // Rotazione barcode con pivot al punto di ancoraggio (x,y).
                    // Dopo Canvas.Translate(anchor) + RotateDegrees + Translate(tx,ty),
                    // local(lx,ly) → screen:
                    //   270° CW: screen = (anchor.x + ly + ty, anchor.y - lx - tx)
                    // Per ZPL B (270°): barcode si espande dal ^FO verso DESTRA (bar height)
                    // e verso il BASSO (lunghezza barcode). Verificato con Labelary.
                    switch (barcode.Orientation)
                    {
                        case Rendering.FieldOrientation.Rotated90:
                            context.Canvas.RotateDegrees(90);
                            // 90° CW: local(lx,ly) → screen(anchor.x - ly - ty, anchor.y + lx + tx)
                            // Barcode estende a SINISTRA e verso il BASSO.
                            break;
                        case Rendering.FieldOrientation.Inverted:
                            context.Canvas.RotateDegrees(180);
                            context.Canvas.Translate(-drawWidth, -drawHeight);
                            break;
                        case Rendering.FieldOrientation.Rotated270:
                            context.Canvas.RotateDegrees(270);
                            // 270° CW: local(lx,ly) → screen(anchor.x + ly, anchor.y - lx)
                            // Senza translate: barcode va verso ALTO (screen -Y). 
                            // Con Translate(-drawWidth, 0): local(0,0) → screen(anchor.x, anchor.y + drawWidth)
                            // → barcode va verso il BASSO dal ^FO. Coerente con Labelary.
                            context.Canvas.Translate(-drawWidth, 0);
                            break;
                        case Rendering.FieldOrientation.Normal:
                        default:
                            break;
                    }

                    // Disegna il bitmap scalato alla dimensione corretta
                    var destRect = SKRect.Create(0, 0, drawWidth, drawHeight);
                    using (var paint = new SKPaint { FilterQuality = SKFilterQuality.None, IsAntialias = false })
                    {
                        context.Canvas.DrawBitmap(bitmap, destRect, paint);
                    }
                    
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
