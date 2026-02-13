using System;

namespace ZplRenderer.Utils
{
    /// <summary>
    /// ZPL font characteristics including system font name and style.
    /// </summary>
    public class ZplFontInfo
    {
        public string FontFamily { get; set; } = "Arial";
        public bool IsBold { get; set; } = false;
        public bool IsMonospace { get; set; } = false;
        /// <summary>
        /// Width to height ratio for this font (used when width is not specified).
        /// ZPL bitmap fonts have specific aspect ratios.
        /// </summary>
        public float AspectRatio { get; set; } = 0.6f;
    }

    /// <summary>
    /// Provides mapping between ZPL font identifiers and system font families.
    /// ZPL fonts are designated by single letters (A-Z) or digits (0-9).
    /// Font 0 is the scalable "Swiss 721" style font.
    /// Fonts A-H are bitmap fonts with specific sizes.
    /// </summary>
    public static class FontMappings
    {
        /// <summary>
        /// Gets the system font name for a ZPL font identifier.
        /// </summary>
        public static string GetSystemFont(string zplFontName)
        {
            return GetFontInfo(zplFontName).FontFamily;
        }

        /// <summary>
        /// Gets detailed font information for a ZPL font identifier.
        /// </summary>
        public static ZplFontInfo GetFontInfo(string zplFontName)
        {
            if (string.IsNullOrEmpty(zplFontName))
                return new ZplFontInfo { FontFamily = "Arial", AspectRatio = 0.6f };

            // If font name ends with .TTF or .FNT, use the name without extension
            if (zplFontName.EndsWith(".TTF", StringComparison.OrdinalIgnoreCase) ||
                zplFontName.EndsWith(".FNT", StringComparison.OrdinalIgnoreCase))
            {
                return new ZplFontInfo 
                { 
                    FontFamily = System.IO.Path.GetFileNameWithoutExtension(zplFontName),
                    AspectRatio = 0.6f 
                };
            }

            // ZPL standard font mappings
            // Reference: Zebra ZPL II Programming Guide
            switch (zplFontName.ToUpperInvariant())
            {
                // Font 0: Scalable Swiss 721 (similar to Helvetica/Arial)
                case "0":
                    return new ZplFontInfo { FontFamily = "Arial", IsBold = true, AspectRatio = 0.6f };

                // Font A: 9x5 matrix, monospaced
                case "A":
                    return new ZplFontInfo { FontFamily = "Consolas", IsMonospace = true, AspectRatio = 0.56f };

                // Font B: 11x7 matrix
                case "B":
                    return new ZplFontInfo { FontFamily = "Arial Narrow", AspectRatio = 0.64f };

                // Font C/D: 18x10 matrix
                case "C":
                case "D":
                    return new ZplFontInfo { FontFamily = "Arial", AspectRatio = 0.56f };

                // Font E: 28x15 OCR-B style
                case "E":
                    return new ZplFontInfo { FontFamily = "Consolas", IsMonospace = true, AspectRatio = 0.54f };

                // Font F: 26x13 matrix
                case "F":
                    return new ZplFontInfo { FontFamily = "Arial", AspectRatio = 0.5f };

                // Font G: 60x40 large bold
                case "G":
                    return new ZplFontInfo { FontFamily = "Arial", IsBold = true, AspectRatio = 0.67f };

                // Font H: 21x13 OCR-A style
                case "H":
                    return new ZplFontInfo { FontFamily = "Consolas", IsMonospace = true, AspectRatio = 0.62f };

                // Font P-V: Additional fonts, map to Arial variants
                case "P":
                case "Q":
                case "R":
                case "S":
                case "T":
                case "U":
                case "V":
                    return new ZplFontInfo { FontFamily = "Arial", AspectRatio = 0.6f };

                // Numeric fonts 1-9: Various sizes, map to appropriate styles
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                    return new ZplFontInfo { FontFamily = "Arial", AspectRatio = 0.6f };

                default:
                    return new ZplFontInfo { FontFamily = "Arial", AspectRatio = 0.6f };
            }
        }
    }
}
