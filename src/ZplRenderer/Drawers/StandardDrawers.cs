using SkiaSharp;
using System;
using System.Collections.Generic;
using ZplRenderer.Elements;
using ZplRenderer.Utils;

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

                // Correzione dimensione font: ZPL height = altezza cella carattere (cap + descent),
                // SkiaSharp TextSize = altezza em-box (include ascent, descent e leading).
                // Ricalibriamo TextSize in modo che l'altezza visibile corrisponda ai dots ZPL.
                float wantedCellHeight = textField.Font.Size;
                float actualCellHeight = (-paint.FontMetrics.Ascent) + paint.FontMetrics.Descent;
                if (actualCellHeight > 0)
                {
                    paint.TextSize = wantedCellHeight * (wantedCellHeight / actualCellHeight);
                }

                
                // Calcolo AspectRatio dal FontMappings in base al nome del font ZPL.
                // Sostituisce il vecchio fontAdjustment = 0.70f hardcoded che era impreciso
                // per tutti i font tranne Font "0".
                var fontInfo = FontMappings.GetFontInfo(textField.ZplFontName);
                float fontAspectRatio = fontInfo.AspectRatio;

                // In ZPL, la larghezza naturale del font (AspectRatio) viene SEMPRE applicata.
                // Il parametro w di ^A è un moltiplicatore aggiuntivo: ScaleX = (w/h) × AspectRatio.
                // Verificato con Labelary: ^A0N,70,70 → aspetto 0.6 (come w=0);
                //                          ^A0N,70,35 → aspetto 0.3 (0.5 × 0.6).
                float effectiveScaleX = textField.ScaleX * fontAspectRatio;

                paint.TextScaleX = effectiveScaleX;
                paint.IsAntialias = true;
                paint.Color = textField.IsReversePrint ? SKColors.White : SKColors.Black;

                // Preparazione righe di testo con supporto word wrapping (^FB)
                var lines = new List<string>();
                float blockWidth = 0;
                char align = 'L';
                int maxLines = 1;

                if (textField.FieldBlock != null)
                {
                    blockWidth = textField.FieldBlock.Width;
                    align = textField.FieldBlock.Alignment;
                    maxLines = textField.FieldBlock.MaxLines;

                    // Implementazione word wrapping: spezza il testo in righe
                    // che non superano la larghezza del blocco
                    lines = WrapText(textField.Text, paint, blockWidth, maxLines);
                }
                else
                {
                    lines.Add(textField.Text);
                }

                // Altezza di linea per il layout multilinea
                float lineHeight = paint.FontSpacing;

                // === ROTAZIONE TESTO ===
                // ZPL supporta 4 orientamenti tramite ^A (N, R, I, B).
                // Il pivot di rotazione è sempre il punto di ancoraggio (X,Y) dell'elemento.
                context.Canvas.Save();

                float anchorX = textField.X;
                float anchorY = textField.Y;
                
                // Trasla al punto di ancoraggio per applicare la rotazione attorno ad esso
                context.Canvas.Translate(anchorX, anchorY);

                switch (textField.Orientation)
                {
                    case Rendering.FieldOrientation.Rotated90:
                        // R = 90° orario. In ZPL il testo si sviluppa dal basso verso l'alto
                        context.Canvas.RotateDegrees(90);
                        break;
                    case Rendering.FieldOrientation.Inverted:
                        // I = 180°. Il testo è capovolto (specchiato orizzontalmente e verticalmente)
                        context.Canvas.RotateDegrees(180);
                        break;
                    case Rendering.FieldOrientation.Rotated270:
                        // B = 270° (bottom-to-top). Il testo si sviluppa dal basso verso l'alto
                        context.Canvas.RotateDegrees(270);
                        break;
                    case Rendering.FieldOrientation.Normal:
                    default:
                        // N = nessuna rotazione
                        break;
                }

                // Le coordinate di disegno sono ora relative al punto di ancoraggio (0,0)
                int lineIndex = 0;
                foreach (var line in lines)
                {
                    float textWidth = paint.MeasureText(line);

                    // Calcolo offset X per allineamento (^FB alignment: L, C, R, J)
                    float lineXOffset = 0;
                    if (blockWidth > 0)
                    {
                        switch (align)
                        {
                            case 'C': lineXOffset = (blockWidth - textWidth) / 2; break;
                            case 'R': lineXOffset = blockWidth - textWidth; break;
                            case 'L':
                            default: lineXOffset = 0; break;
                        }
                    }

                    // Calcolo Y: coordina baseline per SkiaSharp DrawText
                    // Con la traslazione, le coordinate locali sono (0,0) = punto di ancoraggio
                    float localX = lineXOffset;
                    float localY;

                    if (textField.OriginType == ElementOriginType.Baseline)
                    {
                        // ^FT: Y è già la baseline, ma abbiamo traslato di anchorY
                        // quindi localmente la baseline è a 0
                        localY = lineHeight * lineIndex;
                    }
                    else
                    {
                        // ^FO: Y è il top-left. Baseline = top + (-ascent)
                        localY = -paint.FontMetrics.Ascent + (lineHeight * lineIndex);
                    }

                    context.Canvas.DrawText(line, localX, localY, paint);
                    lineIndex++;
                }

                context.Canvas.Restore();
            }
        }

        /// <summary>
        /// Implementa il word wrapping ZPL per il comando ^FB (Field Block).
        /// Spezza il testo in righe che non superano la larghezza massima del blocco.
        /// Usa '\n' e '\\&' come separatori di linea espliciti (specifica ZPL).
        /// </summary>
        private List<string> WrapText(string text, SKPaint paint, float maxWidth, int maxLines)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
            {
                result.Add(text ?? "");
                return result;
            }

            // ZPL usa \& come separatore di riga esplicito in ^FB
            text = text.Replace("\\&", "\n");

            var paragraphs = text.Split('\n');

            foreach (var paragraph in paragraphs)
            {
                if (result.Count >= maxLines) break;

                if (string.IsNullOrEmpty(paragraph))
                {
                    result.Add("");
                    continue;
                }

                var words = paragraph.Split(' ');
                string currentLine = "";

                foreach (var word in words)
                {
                    if (result.Count >= maxLines) break;

                    string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    float testWidth = paint.MeasureText(testLine);

                    if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                    {
                        // La riga corrente è piena, salvala e inizia una nuova
                        result.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }

                // Aggiungi l'ultima riga del paragrafo
                if (!string.IsNullOrEmpty(currentLine) && result.Count < maxLines)
                {
                    result.Add(currentLine);
                }
            }

            if (result.Count == 0)
            {
                result.Add("");
            }

            return result;
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
                // Outline — inset dello stroke per restare dentro i limiti
                var rect = SKRect.Create(box.X, box.Y, box.Width, box.Height);
                float halfStroke = box.BorderThickness / 2.0f;
                rect.Inflate(-halfStroke, -halfStroke); 

                if (box.CornerRounding > 0)
                {
                    // ZPL ^GB: il 5° parametro (r) va da 0 a 8.
                    // Il raggio di arrotondamento è proporzionale al lato minore del rettangolo.
                    // r=8 significa raggio = metà del lato minore (completamente arrotondato).
                    float minSide = Math.Min(rect.Width, rect.Height);
                    float cornerRadius = (box.CornerRounding / 8.0f) * (minSide / 2.0f);
                    context.Canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, paint);
                }
                else
                {
                    context.Canvas.DrawRect(rect, paint);
                }
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
            
            // Posizione — trasla al punto di ancoraggio
            context.Canvas.Translate(img.X, img.Y);
            
            // Rotazione con pivot corretto secondo specifica ZPL
            switch (img.Orientation)
            {
                case Rendering.FieldOrientation.Rotated90:
                    context.Canvas.RotateDegrees(90);
                    context.Canvas.Translate(0, -img.Bitmap.Height);
                    break;
                case Rendering.FieldOrientation.Inverted:
                    context.Canvas.RotateDegrees(180);
                    context.Canvas.Translate(-img.Bitmap.Width, -img.Bitmap.Height);
                    break;
                case Rendering.FieldOrientation.Rotated270:
                    context.Canvas.RotateDegrees(270);
                    context.Canvas.Translate(-img.Bitmap.Height, 0);
                    break;
                case Rendering.FieldOrientation.Normal:
                default:
                    break;
            }
            
            // Draw
            context.Canvas.DrawBitmap(img.Bitmap, 0, 0);
            
            context.Canvas.Restore();
        }
    }
}
