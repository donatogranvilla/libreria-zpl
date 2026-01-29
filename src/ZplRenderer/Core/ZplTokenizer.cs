using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZplRenderer.Core
{
    /// <summary>
    /// Represents a tokenized ZPL command.
    /// </summary>
    public class ZplToken
    {
        /// <summary>
        /// The command prefix (^ or ~).
        /// </summary>
        public char Prefix { get; set; }

        /// <summary>
        /// The command code (e.g., FO, FD, A, GB, BC, BQ).
        /// </summary>
        public string CommandCode { get; set; }

        /// <summary>
        /// The raw parameters string after the command code.
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Original raw content of the token.
        /// </summary>
        public string RawContent { get; set; }
    }

    /// <summary>
    /// Tokenizes ZPL code into command tokens.
    /// Uses a hybrid approach: Regex for standard commands, manual parsing for data-heavy commands.
    /// </summary>
    public class ZplTokenizer
    {
        // Regex to match ZPL commands: ^ or ~ followed by command letters and parameters
        // Note: We use this iteratively to find the start of commands
        private static readonly Regex CommandStartPattern = new Regex(
            @"([\^~])([A-Za-z0-9@]{1,2})",
            RegexOptions.Compiled);

        /// <summary>
        /// Tokenizes ZPL code into individual command tokens.
        /// </summary>
        /// <param name="zplCode">The raw ZPL code string.</param>
        /// <returns>List of ZPL tokens.</returns>
        public IEnumerable<ZplToken> Tokenize(string zplCode)
        {
            if (string.IsNullOrEmpty(zplCode))
                yield break;

            int currentIndex = 0;

            while (currentIndex < zplCode.Length)
            {
                // Find next command start
                var match = CommandStartPattern.Match(zplCode, currentIndex);

                if (!match.Success)
                {
                    // No more commands found
                    break;
                }

                // If there's content before the match, it's ignored (garbage/whitespace)
                // or belongs to previous command (but we handle params eagerly below).

                int cmdStartIndex = match.Index;
                string prefix = match.Groups[1].Value;
                string commandCode = match.Groups[2].Value.ToUpperInvariant();
                int paramStartIndex = cmdStartIndex + match.Length;

                string parameters = "";
                int nextCmdIndex = -1;

                // Check for complex commands that require special parsing
                if (IsComplexCommand(commandCode))
                {
                    // Manual parsing for complex commands
                    parameters = ParseComplexParameters(zplCode, commandCode, paramStartIndex, out nextCmdIndex);
                }
                else
                {
                    // Standard parsing: read until next ^ or ~
                    // But be careful not to catch the current command's prefix if we are just starting
                    nextCmdIndex = FindNextCommand(zplCode, paramStartIndex);

                    if (nextCmdIndex == -1)
                        parameters = zplCode.Substring(paramStartIndex);
                    else
                        parameters = zplCode.Substring(paramStartIndex, nextCmdIndex - paramStartIndex);
                }

                yield return new ZplToken
                {
                    Prefix = prefix[0],
                    CommandCode = commandCode,
                    Parameters = parameters,
                    RawContent = zplCode.Substring(cmdStartIndex, (nextCmdIndex == -1 ? zplCode.Length : nextCmdIndex) - cmdStartIndex)
                };

                currentIndex = nextCmdIndex == -1 ? zplCode.Length : nextCmdIndex;
            }
        }

        private bool IsComplexCommand(string commandCode)
        {
            // GF: Graphic Field (Binary data)
            // DG: Download Graphics (Binary data)
            // FD: Field Data (Can contain special chars if escaped, though regex handles most cases)
            // SN: Serialization Field (Can contain complex patterns)
            return commandCode == "GF" || commandCode == "DG" || commandCode == "ID" || commandCode == "IL";
        }

        private int FindNextCommand(string zpl, int startIndex)
        {
            // Look for next ^ or ~ followed by alphanumeric
            var nextMatch = CommandStartPattern.Match(zpl, startIndex);
            return nextMatch.Success ? nextMatch.Index : -1;
        }

        private string ParseComplexParameters(string zpl, string commandCode, int startIndex, out int nextCmdIndex)
        {
            // Fallback for end of string
            if (startIndex >= zpl.Length)
            {
                nextCmdIndex = -1;
                return "";
            }

            // For GF (Graphic Field)
            // Format: ^GFa,b,c,d,data
            // We try to parse the header to know how much data to read
            if (commandCode == "GF" || commandCode == "DG")
            {
                // Scan for commas to find the data length parameter
                // ^GF a, b, c, d, data
                // c is total bytes

                int scanIdx = startIndex;
                int commaCount = 0;
                int dataStartIdx = -1;
                int totalBytes = 0;

                // Parse header approximately
                // This is a simplified parser - it tries to find the 'data' part start
                // ZPL is rarely strict, but GF usually has 4 commas before data

                while (scanIdx < zpl.Length && commaCount < 4)
                {
                    char c = zpl[scanIdx];
                    if (c == ',')
                    {
                        commaCount++;
                        if (commaCount == 3) // Found 'c' parameter (total bytes)
                        {
                            // Try parsing the number between 2nd and 3rd comma? 
                            // Actually pars are: a,b,c,d
                            // 1st comma after a
                            // 2nd comma after b
                            // The value BEFORE this 3rd comma is 'c' (total bytes)? 
                            // No, c is the 3rd parameter.
                            // ^GFa,b,c,d,data
                            // params split by commas: [0]=a, [1]=b, [2]=c, [3]=d

                            // Let's rely on finding 4th comma to start data
                        }
                    }
                    else if (c == '^' || c == '~')
                    {
                        // Abort if we hit a new command early (malformed GF?)
                        // But wait, what if params are missing?
                        break;
                    }
                    scanIdx++;
                }

                if (commaCount >= 4)
                {
                    // We found the data start.
                    // The data is hex.
                    // We read until next command char that is NOT part of hex (which is technically A-F, 0-9).
                    // But ZPL hex is continuous.
                    // So effectively we treat everything after 4th comma as data until next ^ or ~
                    // UNLESS the data itself contains ^ or ~ (which is valid for binary download if not hex encoded??)
                    // ZPL manual says GF data is hexadecimal. Hex doesn't contain ^ or ~.
                    // So actually, standard parsing might work for GF if it is strictly hex!
                    // BUT, if it is "Binary" (B compression) it implies raw bytes?
                    // Usually GF is ASCII Hex.

                    // So for now, we treat it same as standard: read until next ^ or ~
                    // But giving it a dedicated block allows us to handle nuances later if needed.
                }
            }

            // Default fallback: same as standard
            nextCmdIndex = FindNextCommand(zpl, startIndex);

            if (nextCmdIndex == -1)
                return zpl.Substring(startIndex);
            else
                return zpl.Substring(startIndex, nextCmdIndex - startIndex);
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace ZplRenderer.Core
//{
//    public class ZplToken
//    {
//        public char Prefix { get; set; }
//        public string CommandCode { get; set; }
//        public string Parameters { get; set; }
//        public string RawContent { get; set; }
//    }

//    public class ZplTokenizer
//    {
//        // Regex per identificare l'inizio di un comando ZPL
//        private static readonly Regex CommandStartPattern = new Regex(
//            @"([\^~])([A-Za-z0-9@]{1,2})",
//            RegexOptions.Compiled | RegexOptions.IgnoreCase);

//        public IEnumerable<ZplToken> Tokenize(string zplCode)
//        {
//            if (string.IsNullOrEmpty(zplCode))
//                yield break;

//            int currentIndex = 0;
//            while (currentIndex < zplCode.Length)
//            {
//                var match = CommandStartPattern.Match(zplCode, currentIndex);
//                if (!match.Success) break;

//                int cmdStartIndex = match.Index;
//                char prefix = match.Groups[1].Value[0];
//                string commandCode = match.Groups[2].Value.ToUpperInvariant();
//                int paramStartIndex = cmdStartIndex + match.Length;

//                int nextCmdIndex;
//                string parameters;

//                // LOGICA DI PARSING DIFFERENZIATA
//                if (IsBinaryGraphicCommand(commandCode))
//                {
//                    // Gestione speciale per GF, DG, IL: leggiamo i byte dichiarati
//                    parameters = ParseGraphicParameters(zplCode, paramStartIndex, out nextCmdIndex);
//                }
//                else if (commandCode == "FD")
//                {
//                    // Gestione speciale per Field Data: termina con ^FS
//                    parameters = ParseFieldData(zplCode, paramStartIndex, out nextCmdIndex);
//                }
//                else
//                {
//                    // Parsing standard: fino al prossimo ^ o ~
//                    nextCmdIndex = FindNextCommand(zplCode, paramStartIndex);
//                    parameters = ExtractSubstring(zplCode, paramStartIndex, nextCmdIndex);
//                }

//                yield return new ZplToken
//                {
//                    Prefix = prefix,
//                    CommandCode = commandCode,
//                    Parameters = parameters,
//                    RawContent = zplCode.Substring(cmdStartIndex, (nextCmdIndex == -1 ? zplCode.Length : nextCmdIndex) - cmdStartIndex)
//                };

//                currentIndex = nextCmdIndex == -1 ? zplCode.Length : nextCmdIndex;
//            }
//        }

//        private bool IsBinaryGraphicCommand(string code)
//            => code == "GF" || code == "DG" || code == "IL";

//        private int FindNextCommand(string zpl, int startIndex)
//        {
//            if (startIndex >= zpl.Length) return -1;
//            var nextMatch = CommandStartPattern.Match(zpl, startIndex);
//            return nextMatch.Success ? nextMatch.Index : -1;
//        }

//        private string ExtractSubstring(string text, int start, int end)
//        {
//            if (end == -1 || end > text.Length) return text.Substring(start);
//            return text.Substring(start, end - start);
//        }

//        /// <summary>
//        /// Gestisce i dati grafici (es. ^GF). Cerca di capire quanti byte leggere 
//        /// per evitare che i dati binari contengano caratteri che sembrano comandi.
//        /// </summary>
//        private string ParseGraphicParameters(string zpl, int startIndex, out int nextCmdIndex)
//        {
//            // Esempio ^GFa,b,c,d,data...
//            // c = numero totale di byte. Se i dati sono ASCII HEX, 1 byte = 2 caratteri.
//            string header = "";
//            int dataStartIdx = -1;
//            int commaCount = 0;
//            int totalBytes = 0;

//            for (int i = startIndex; i < zpl.Length; i++)
//            {
//                if (zpl[i] == ',')
//                {
//                    commaCount++;
//                    if (commaCount == 4) // Dopo la 4a virgola iniziano i dati
//                    {
//                        dataStartIdx = i + 1;
//                        // Estraiamo il valore 'c' (total bytes)
//                        var parts = header.Split(',');
//                        if (parts.Length >= 3)
//                            int.TryParse(parts[2], out totalBytes);
//                        break;
//                    }
//                }
//                if (commaCount < 4) header += zpl[i];
//            }

//            if (dataStartIdx != -1 && totalBytes > 0)
//            {
//                // Ipotizziamo ASCII Hex (molto comune): 1 byte = 2 caratteri
//                // Nota: se fosse binario puro, sarebbe totalBytes.
//                int expectedDataLength = totalBytes * 2;
//                nextCmdIndex = FindNextCommand(zpl, dataStartIdx + expectedDataLength);
//            }
//            else
//            {
//                nextCmdIndex = FindNextCommand(zpl, startIndex);
//            }

//            return ExtractSubstring(zpl, startIndex, nextCmdIndex);
//        }

//        /// <summary>
//        /// Gestisce il testo dei campi (^FD). Deve ignorare ^ o ~ finché non trova ^FS.
//        /// </summary>
//        private string ParseFieldData(string zpl, int startIndex, out int nextCmdIndex)
//        {
//            // Cerca il Field Separator ^FS
//            int fsIndex = zpl.IndexOf("^FS", startIndex, StringComparison.OrdinalIgnoreCase);

//            if (fsIndex != -1)
//            {
//                nextCmdIndex = fsIndex; // Si ferma prima di ^FS, che sarà il prossimo token
//                return zpl.Substring(startIndex, fsIndex - startIndex);
//            }

//            // Fallback se manca ^FS: cerchiamo il prossimo comando
//            nextCmdIndex = FindNextCommand(zpl, startIndex);
//            return ExtractSubstring(zpl, startIndex, nextCmdIndex);
//        }
//    }
//}