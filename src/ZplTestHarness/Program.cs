using System;
using System.IO;
using SkiaSharp;
using ZplRenderer;

namespace ZplTestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            string zpl = @"^XA
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

            try 
            {
                var engine = new ZplEngine();
                // Render at 203 DPI (label is 812x812 dots)
                using (var bitmap = engine.Render(zpl, 812, 812, 203))
                {
                    string outputPath = "complex_label.png";
                    engine.RenderToFile(zpl, outputPath, 812, 812, 203);
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
    }
}
