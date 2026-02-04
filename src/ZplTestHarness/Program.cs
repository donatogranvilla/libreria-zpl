using System;
using System.IO;
using System.Linq;
using SkiaSharp;
using ZplRenderer;

namespace ZplTestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            string zplInput = "";
            string outputPath = "output.png";
            int width = 812;
            int height = 812;
            int dpi = 203;

            // Simple command line parsing
            if (args.Length > 0 && File.Exists(args[0]))
            {
                zplInput = File.ReadAllText(args[0]);
                Console.WriteLine($"Loaded ZPL from {args[0]}");
                outputPath = Path.ChangeExtension(args[0], ".png");
            }
            else
            {
                // Fallback to sample
                Console.WriteLine("Usage: ZplTestHarness <file.zpl> [width] [height] [dpi]");
                Console.WriteLine("Running default sample...");
                zplInput = GetEnhancedSample(); 
            }

            if (args.Length > 1 && int.TryParse(args[1], out int w)) width = w;
            if (args.Length > 2 && int.TryParse(args[2], out int h)) height = h;
            if (args.Length > 3 && int.TryParse(args[3], out int d)) dpi = d;


            try 
            {
                var engine = new ZplEngine();
                
                // Validate first
                Console.WriteLine("Validating ZPL...");
                var validationErrors = engine.Validate(zplInput);
                if (validationErrors.Any())
                {
                    Console.WriteLine("Validation Errors/Warnings:");
                    foreach (var err in validationErrors)
                    {
                        Console.WriteLine($"- {err}");
                    }
                }
                else
                {
                    Console.WriteLine("Validation passed.");
                }

                // Render
                Console.WriteLine($"Rendering at {width}x{height} ({dpi} DPI)...");
                using (var bitmap = engine.Render(zplInput, width, height, dpi))
                {
                    using (var image = SKImage.FromBitmap(bitmap))
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        using (var stream = File.OpenWrite(outputPath))
                        {
                            data.SaveTo(stream);
                        }
                    }
                    Console.WriteLine($"Label rendered successfully to {Path.GetFullPath(outputPath)}");
                    Console.WriteLine($"Dimensions: {bitmap.Width}x{bitmap.Height}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering label: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static string GetEnhancedSample()
        {
            return @"^XA
^PW812^LL1200
^CI28
^FO50,50^A0N,50,50^FDZPL ENHANCED DEMO^FS
^FO50,120^A0N,30,30^FD1. Codabar Barcode (^BK):^FS
^FO50,160^BKN,N,50^FDA12345B^FS
^FO50,250^A0N,30,30^FD2. Interleaved 2of5 (^B2):^FS
^FO50,290^B2N,50,Y,N,N^FD1234567890^FS
^FO50,380^A0N,30,30^FD3. International Chars (^CI28):^FS
^FO50,420^A0N,30,30^FDUTF8: È à ñ ö €^FS
^FO50,500^A0N,30,30^FDHex: _C3_89_C3_A0_C3_B1_C3_B6_E2_82_AC^FS
^FO50,550^GB700,0,2^FS
^FO50,580^A0N,30,30^FD4. Global Orientation (^PO):^FS
^FO50,620^A0N,25,25^FD(This label is Normal, ^POI would invert it)^FS
^FO50,660^A0N,30,30^FD5. Label Shift (^LS):^FS
^FO50,700^A0N,25,25^FD(Currently 0. try changing code to ^LS50)^FS
^XZ";
        }
    }
}
