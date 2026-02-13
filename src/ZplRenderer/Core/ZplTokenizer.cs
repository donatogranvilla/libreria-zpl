using System;
using System.Collections.Generic;
using System.Text;

namespace ZplRenderer.Core
{
    /// <summary>
    /// Rappresenta un comando ZPL tokenizzato.
    /// </summary>
    public class ZplToken
    {
        /// <summary>
        /// Il prefisso del comando (^ o ~).
        /// </summary>
        public char Prefix { get; set; }

        /// <summary>
        /// Il codice del comando (es. FO, FD, A0, GB, BC, BQ).
        /// Per i comandi font ^A, include il font identifier (es. "A0", "AE").
        /// </summary>
        public string CommandCode { get; set; }

        /// <summary>
        /// La stringa dei parametri grezzi dopo il codice comando.
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Contenuto grezzo originale del token.
        /// </summary>
        public string RawContent { get; set; }

        /// <summary>
        /// Posizione del token nel codice sorgente (utile per debug).
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Lunghezza del token nel codice sorgente.
        /// </summary>
        public int Length { get; set; }
    }

    /// <summary>
    /// Tokenizza il codice ZPL in token di comando.
    /// 
    /// Implementazione basata su scanner carattere per carattere (NO regex).
    /// I comandi ZPL iniziano SEMPRE con ^ o ~ seguiti da 1-2 lettere/cifre.
    /// I parametri sono tutto il testo tra il codice comando e il prossimo ^ o ~.
    /// 
    /// Casi speciali gestiti:
    /// - ^FD, ^SN, ^FV: terminano con ^FS (il contenuto puo' includere ^ o ~)
    /// - ^GF: dati grafici con byte count
    /// - ~DG, ~DY: download con dati hex/binari
    /// - ^DF: formato memorizzato, termina con ^XZ
    /// - ^FX: commento, tutto fino al prossimo ^ viene ignorato
    /// - ^A: il font identifier (0-9, A-Z) fa parte del codice comando
    /// </summary>
    public class ZplTokenizer
    {
        /// <summary>
        /// Tokenizza il codice ZPL in singoli token di comando.
        /// Scansiona carattere per carattere cercando solo ^ e ~ come delimitatori.
        /// </summary>
        /// <param name="zplCode">La stringa di codice ZPL grezzo.</param>
        /// <returns>Lista di token ZPL.</returns>
        public IEnumerable<ZplToken> Tokenize(string zplCode)
        {
            if (string.IsNullOrEmpty(zplCode))
                yield break;

            int i = 0;

            while (i < zplCode.Length)
            {
                // Cerco il prossimo delimitatore di comando (^ o ~)
                char prefix = zplCode[i];
                if (prefix != '^' && prefix != '~')
                {
                    // Carattere non-comando, lo salto
                    i++;
                    continue;
                }

                int cmdStartIndex = i;
                i++; // Avanzo oltre il prefisso

                // Verifico che ci siano caratteri dopo il prefisso
                if (i >= zplCode.Length)
                    break;

                // Estraggo il codice comando (1-2 caratteri alfanumerici)
                string commandCode = ExtractCommandCode(zplCode, ref i);
                if (string.IsNullOrEmpty(commandCode))
                {
                    // Nessun codice valido dopo il prefisso, salto
                    continue;
                }

                // Estraggo i parametri in base al tipo di comando
                string parameters;
                int tokenEndIndex;

                if (IsFieldDataCommand(commandCode))
                {
                    // ^FD, ^SN, ^FV: tutto fino a ^FS (contenuto puo' avere ^ o ~)
                    parameters = ParseUntilFieldSeparator(zplCode, i, out tokenEndIndex);
                }
                else if (IsDownloadFormatCommand(commandCode))
                {
                    // ^DF: formato memorizzato, termina con ^XZ
                    parameters = ParseUntilEndFormat(zplCode, i, out tokenEndIndex);
                }
                else if (commandCode == "GF")
                {
                    // ^GF: dati grafici inline con byte count
                    parameters = ParseGraphicField(zplCode, i, out tokenEndIndex);
                }
                else if (IsDownloadCommand(prefix, commandCode))
                {
                    // ~DG, ~DY ecc: download con dati binari/hex
                    parameters = ParseDownloadCommand(zplCode, commandCode, i, out tokenEndIndex);
                }
                else if (commandCode == "FX")
                {
                    // ^FX: commento ZPL, ignoro tutto fino al prossimo ^
                    parameters = ParseComment(zplCode, i, out tokenEndIndex);
                }
                else
                {
                    // Parsing standard: parametri fino al prossimo ^ o ~
                    parameters = ParseStandardParameters(zplCode, i, out tokenEndIndex);
                }

                int tokenLength = tokenEndIndex - cmdStartIndex;

                yield return new ZplToken
                {
                    Prefix = prefix,
                    CommandCode = commandCode.ToUpperInvariant(),
                    Parameters = parameters?.Trim() ?? "",
                    RawContent = zplCode.Substring(cmdStartIndex, tokenLength),
                    Position = cmdStartIndex,
                    Length = tokenLength
                };

                i = tokenEndIndex;
            }
        }

        #region Estrazione Codice Comando

        /// <summary>
        /// Estrae il codice comando dalla posizione corrente.
        /// 
        /// Regole ZPL per i codici comando:
        /// - La maggior parte dei comandi ha 2 caratteri (FO, FD, GB, BC, XA, FS, ecc.)
        /// - Il comando ^A (font) e' speciale: ^A seguito da un identificatore font (0-9, A-Z)
        ///   forma un codice di 2 caratteri (es. A0, AE) ma il font identifier e' semanticamente
        ///   il primo parametro. Lo includiamo nel codice per compatibilita' con CommandFactory.
        /// - Comandi monocarattere: ^A da solo (raro), ~J (host commands)
        /// </summary>
        private string ExtractCommandCode(string zpl, ref int index)
        {
            if (index >= zpl.Length)
                return null;

            char first = zpl[index];

            // Il primo carattere deve essere una lettera o cifra
            if (!IsAlphaNumeric(first))
                return null;

            index++;

            // Provo a leggere il secondo carattere del codice
            if (index < zpl.Length)
            {
                char second = zpl[index];

                // Per il comando ^A (font): il secondo carattere e' il font identifier
                // e fa parte del codice (es. A0, AE, A@)
                if (first == 'A' || first == 'a')
                {
                    if (IsAlphaNumeric(second) || second == '@')
                    {
                        index++;
                        return new string(new[] { char.ToUpperInvariant(first), second });
                    }
                    // ^A da solo
                    return char.ToUpperInvariant(first).ToString();
                }

                // Per tutti gli altri comandi: secondo carattere alfanumerico = parte del codice
                if (IsAlphaNumeric(second))
                {
                    index++;
                    return new string(new[] { char.ToUpperInvariant(first), char.ToUpperInvariant(second) });
                }
            }

            // Comando monocarattere
            return char.ToUpperInvariant(first).ToString();
        }

        #endregion

        #region Identificazione Tipi di Comando

        /// <summary>
        /// Comandi che terminano con ^FS: il contenuto puo' contenere ^ e ~ letterali.
        /// ^FD (Field Data), ^SN (Serialization), ^FV (Field Variable).
        /// </summary>
        private bool IsFieldDataCommand(string code)
        {
            return code.Equals("FD", StringComparison.OrdinalIgnoreCase) ||
                   code.Equals("SN", StringComparison.OrdinalIgnoreCase) ||
                   code.Equals("FV", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// ^DF: Download Format, contiene un formato ZPL completo che termina con ^XZ.
        /// </summary>
        private bool IsDownloadFormatCommand(string code)
        {
            return code.Equals("DF", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Comandi di download con dati binari/hex (~DG, ~DB, ~DS, ~DT, ~DU, ~DY).
        /// Usano il prefisso ~ e hanno formati speciali con byte count.
        /// </summary>
        private bool IsDownloadCommand(char prefix, string code)
        {
            if (prefix != '~') return false;
            return code.Equals("DG", StringComparison.OrdinalIgnoreCase) ||
                   code.Equals("DB", StringComparison.OrdinalIgnoreCase) ||
                   code.Equals("DS", StringComparison.OrdinalIgnoreCase) ||
                   code.Equals("DT", StringComparison.OrdinalIgnoreCase) ||
                   code.Equals("DU", StringComparison.OrdinalIgnoreCase) ||
                   code.Equals("DY", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Parser Parametri

        /// <summary>
        /// Parsing standard: legge i parametri fino al prossimo ^ o ~.
        /// Usato per la maggior parte dei comandi ZPL.
        /// </summary>
        private string ParseStandardParameters(string zpl, int startIndex, out int endIndex)
        {
            int nextCmd = FindNextCommandDelimiter(zpl, startIndex);
            if (nextCmd == -1)
            {
                // Fine stringa
                endIndex = zpl.Length;
                return zpl.Substring(startIndex);
            }
            endIndex = nextCmd;
            return zpl.Substring(startIndex, nextCmd - startIndex);
        }

        /// <summary>
        /// Parsing per ^FD, ^SN, ^FV: legge tutto fino a ^FS.
        /// Il contenuto puo' includere ^ e ~ come caratteri letterali.
        /// Se ^FS non viene trovato (ZPL malformato), fallback a parsing standard.
        /// </summary>
        private string ParseUntilFieldSeparator(string zpl, int startIndex, out int endIndex)
        {
            // Cerco ^FS nel testo rimanente
            int fsIndex = zpl.IndexOf("^FS", startIndex, StringComparison.OrdinalIgnoreCase);
            if (fsIndex != -1)
            {
                endIndex = fsIndex; // Mi fermo prima di ^FS (sara' tokenizzato separatamente)
                return zpl.Substring(startIndex, fsIndex - startIndex);
            }

            // Fallback: parsing standard
            return ParseStandardParameters(zpl, startIndex, out endIndex);
        }

        /// <summary>
        /// Parsing per ^DF: legge tutto fino a ^XZ (formato memorizzato completo).
        /// </summary>
        private string ParseUntilEndFormat(string zpl, int startIndex, out int endIndex)
        {
            int xzIndex = zpl.IndexOf("^XZ", startIndex, StringComparison.OrdinalIgnoreCase);
            if (xzIndex != -1)
            {
                endIndex = xzIndex + 3; // Include ^XZ nei parametri
                return zpl.Substring(startIndex, xzIndex + 3 - startIndex);
            }

            // Fallback
            return ParseStandardParameters(zpl, startIndex, out endIndex);
        }

        /// <summary>
        /// Parsing per ^FX (commento ZPL): legge fino al prossimo ^.
        /// Il commento viene ignorato dal rendering.
        /// </summary>
        private string ParseComment(string zpl, int startIndex, out int endIndex)
        {
            // I commenti ^FX terminano con il prossimo ^
            int nextCaret = zpl.IndexOf('^', startIndex);
            if (nextCaret != -1)
            {
                endIndex = nextCaret;
                return zpl.Substring(startIndex, nextCaret - startIndex);
            }
            endIndex = zpl.Length;
            return zpl.Substring(startIndex);
        }

        /// <summary>
        /// Parsing per ^GF (Graphic Field).
        /// Formato: ^GFa,b,c,d,data
        /// a = tipo compressione (A=ASCII, B=Binary, C=Compressed)
        /// b = binary byte count
        /// c = graphic field count (totale byte)
        /// d = bytes per row
        /// data = dati hex (per ASCII: c*2 caratteri)
        /// </summary>
        private string ParseGraphicField(string zpl, int startIndex, out int endIndex)
        {
            if (startIndex >= zpl.Length)
            {
                endIndex = zpl.Length;
                return "";
            }

            // Parso l'header per ottenere il byte count
            int commaCount = 0;
            int totalBytes = 0;
            char compressionType = 'A';
            var headerParts = new List<string>();
            var currentPart = new StringBuilder();

            int headerEnd = startIndex;
            for (int j = startIndex; j < zpl.Length; j++)
            {
                char c = zpl[j];
                if (c == ',')
                {
                    headerParts.Add(currentPart.ToString());
                    currentPart.Clear();
                    commaCount++;

                    if (commaCount == 1 && headerParts.Count > 0)
                    {
                        // Primo parametro: tipo compressione
                        string first = headerParts[0].Trim();
                        if (first.Length > 0)
                            compressionType = char.ToUpperInvariant(first[0]);
                    }

                    if (commaCount == 4)
                    {
                        headerEnd = j + 1;
                        // Terzo parametro (indice 2): total bytes
                        if (headerParts.Count >= 3)
                        {
                            int.TryParse(headerParts[2].Trim(), out totalBytes);
                        }
                        break;
                    }
                }
                else if (c == '^' || c == '~')
                {
                    // Comando interrotto prima del completamento header
                    headerEnd = j;
                    break;
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            // Calcolo la lunghezza prevista dei dati
            if (commaCount >= 4 && totalBytes > 0)
            {
                int expectedDataLength = compressionType == 'B' ? totalBytes : totalBytes * 2;
                int dataEndIdx = Math.Min(headerEnd + expectedDataLength, zpl.Length);

                // Cerco il prossimo comando dopo i dati
                int nextCmd = FindNextCommandDelimiter(zpl, dataEndIdx);
                endIndex = nextCmd == -1 ? zpl.Length : nextCmd;
            }
            else
            {
                // Fallback: fino al prossimo comando
                int nextCmd = FindNextCommandDelimiter(zpl, startIndex);
                endIndex = nextCmd == -1 ? zpl.Length : nextCmd;
            }

            return zpl.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Parsing per comandi di download (~DG, ~DY, ecc).
        /// Formato ~DG: d:o.x,t,w,data (t = total bytes, data = hex)
        /// </summary>
        private string ParseDownloadCommand(string zpl, string commandCode, int startIndex, out int endIndex)
        {
            if (startIndex >= zpl.Length)
            {
                endIndex = zpl.Length;
                return "";
            }

            if (commandCode.Equals("DG", StringComparison.OrdinalIgnoreCase))
            {
                return ParseDGCommand(zpl, startIndex, out endIndex);
            }

            // Per altri comandi download, parsing standard
            return ParseStandardParameters(zpl, startIndex, out endIndex);
        }

        /// <summary>
        /// Parsing specifico per ~DG (Download Graphics).
        /// Formato: ~DGd:o.x,t,w,data
        /// d = dispositivo, o = nome, x = estensione
        /// t = total bytes, w = bytes per riga
        /// data = dati hex ASCII
        /// </summary>
        private string ParseDGCommand(string zpl, int startIndex, out int endIndex)
        {
            int commaCount = 0;
            int totalBytes = 0;
            int dataStartIdx = -1;
            var parts = new List<string>();
            var currentPart = new StringBuilder();

            for (int j = startIndex; j < zpl.Length; j++)
            {
                char c = zpl[j];
                if (c == ',')
                {
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                    commaCount++;

                    if (commaCount == 2)
                    {
                        // Secondo parametro dopo la prima virgola: total bytes
                        if (parts.Count >= 2)
                        {
                            int.TryParse(parts[1].Trim(), out totalBytes);
                        }
                    }
                    else if (commaCount == 3)
                    {
                        dataStartIdx = j + 1;
                        break;
                    }
                }
                else if ((c == '^' || c == '~') && commaCount < 3)
                {
                    break; // Comando interrotto
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            if (dataStartIdx != -1 && totalBytes > 0)
            {
                int expectedDataLength = totalBytes * 2; // ASCII hex: 2 char per byte
                int dataEndIdx = Math.Min(dataStartIdx + expectedDataLength, zpl.Length);
                int nextCmd = FindNextCommandDelimiter(zpl, dataEndIdx);
                endIndex = nextCmd == -1 ? zpl.Length : nextCmd;
            }
            else
            {
                int nextCmd = FindNextCommandDelimiter(zpl, startIndex);
                endIndex = nextCmd == -1 ? zpl.Length : nextCmd;
            }

            return zpl.Substring(startIndex, endIndex - startIndex);
        }

        #endregion

        #region Utilita

        /// <summary>
        /// Trova il prossimo delimitatore di comando (^ o ~) nella stringa ZPL.
        /// Restituisce -1 se non trovato.
        /// </summary>
        private int FindNextCommandDelimiter(string zpl, int startIndex)
        {
            for (int j = startIndex; j < zpl.Length; j++)
            {
                char c = zpl[j];
                if (c == '^' || c == '~')
                    return j;
            }
            return -1;
        }

        /// <summary>
        /// Verifica se un carattere e' alfanumerico (A-Z, a-z, 0-9).
        /// </summary>
        private bool IsAlphaNumeric(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
        }

        #endregion
    }
}
