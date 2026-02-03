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
                zplInput = GetChiolaSample(); // Run the sample relevant to the task
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

        static string GetChiolaSample()
        {
            return @"^XA
^MMT
^PW812
^LL812
^LS0
^CI28
^FT40,90^A0N,70,70^FH^FDChiola^FS
^FT220,90^A0N,20,20^FH^FDUNA STORIA DI SAPORI^FS
^FT40,135^A0N,18,18^FH^FDProduzione:^FS
^FT40,155^A0N,16,16^FH^FDVia Nazionale 14 - 14011 Baldichieri d'Asti (AT)^FS
^FO650,40^GE110,70,2,B^FS
^FT685,60^A0N,14,14^FH^FDITALIA^FS
^FT682,85^A0N,20,20^FH^FD725M^FS
^FT698,105^A0N,14,14^FH^FDCE^FS
^FT420,60^A0N,18,18^FH^FDSoc. Agr. Gruppo Clemme S.S.^FS
^FT50,200^A0N,35,35^FB710,1,0,C^FH^FD 881852 ^FS
^FT50,238^A0N,35,35^FB710,1,0,C^FH^FD 60232054 ^FS
^FO50,245^GB710,3,3^FS
^FT50,285^A0N,20,20^FH^FDData Produzione/Production date^FS
^FT340,285^A0N,30,30^FH^FD 20/10/2026 ^FS
^FO50,480^BY2,2,40^BEB,,Y,N^FD8001234567890^FS
^XZ";
        }
    }
}
