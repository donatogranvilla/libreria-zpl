using System;
using System.IO;
using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

namespace ZplTestHarness
{
    public class DecodeTest
    {
        public static void Run(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"File not found: {imagePath}");
                return;
            }

            Console.WriteLine($"--- Decoding barcodes from: {Path.GetFileName(imagePath)} ---");
            
            using (var stream = File.OpenRead(imagePath))
            using (var bitmap = SKBitmap.Decode(stream))
            {
                if (bitmap == null)
                {
                    Console.WriteLine("Failed to load image.");
                    return;
                }

                var reader = new BarcodeReader();
                reader.AutoRotate = true;
                reader.Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = new[]
                    {
                        BarcodeFormat.QR_CODE,
                        BarcodeFormat.AZTEC,
                        BarcodeFormat.CODE_128,
                        BarcodeFormat.CODE_39,
                        BarcodeFormat.EAN_13,
                        BarcodeFormat.DATA_MATRIX,
                        BarcodeFormat.PDF_417
                    }
                };

                var luminanceSource = new SKBitmapLuminanceSource(bitmap);
                var results = reader.DecodeMultiple(luminanceSource);
                if (results == null || results.Length == 0)
                {
                    Console.WriteLine("  No barcodes found.");
                    return;
                }

                foreach (var result in results)
                {
                    Console.WriteLine($"  [{result.BarcodeFormat}] \"{result.Text}\"");
                }
            }
            Console.WriteLine();
        }
    }
}
