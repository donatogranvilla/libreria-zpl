using System;
using System.Collections.Generic;
using System.Linq;
using ZplRenderer.Commands;

namespace ZplRenderer.Core
{
    /// <summary>
    /// Eccezione lanciata durante il parsing di codice ZPL.
    /// </summary>
    public class ZplParseException : Exception
    {
        public int Position { get; }
        public int Length { get; }
        public string CommandCode { get; }

        public ZplParseException(string message, int position = -1, int length = 0, string commandCode = null)
            : base(message)
        {
            Position = position;
            Length = length;
            CommandCode = commandCode;
        }

        public ZplParseException(string message, Exception innerException)
            : base(message, innerException)
        {
            Position = -1;
            Length = 0;
            CommandCode = null;
        }
    }

    /// <summary>
    /// Contesto di parsing che mantiene lo stato durante l'interpretazione del codice ZPL.
    /// I comandi modal (^FH, ^CI, ^FO, ^A, ecc.) influenzano i comandi successivi.
    /// </summary>
    public class ZplParserContext
    {
        // Stato per ^FH (Field Hexadecimal Indicator)
        public char HexIndicator { get; set; } = '_';
        public bool HexModeActive { get; set; } = false;

        // Stato per ^CI (Change International Font/Encoding)
        public int CurrentEncoding { get; set; } = 0;

        // Stato per ^FO (Field Origin) - posizione corrente
        public int CurrentX { get; set; } = 0;
        public int CurrentY { get; set; } = 0;

        // Stato per ^A (Font) - font corrente
        public string CurrentFont { get; set; } = "0";
        public int CurrentFontHeight { get; set; } = 30;
        public int CurrentFontWidth { get; set; } = 30;

        // Stato per ^LH (Label Home) - offset dell'etichetta
        public int LabelHomeX { get; set; } = 0;
        public int LabelHomeY { get; set; } = 0;

        // Tracciamento formato
        public bool InsideFormat { get; set; } = false;
        public bool InsideDownloadFormat { get; set; } = false;
        public string CurrentDownloadFormatName { get; set; } = null;

        // Formati memorizzati (^DF/^XF)
        public Dictionary<string, List<ZplToken>> StoredFormats { get; } = new Dictionary<string, List<ZplToken>>();

        /// <summary>
        /// Resetto lo stato per una nuova etichetta (dopo ^XA o ^XZ).
        /// </summary>
        public void ResetLabelState()
        {
            HexModeActive = false;
            HexIndicator = '_';
            CurrentEncoding = 0;
            CurrentX = 0;
            CurrentY = 0;
            CurrentFont = "0";
            CurrentFontHeight = 30;
            CurrentFontWidth = 30;
            InsideFormat = false;
        }
    }

    /// <summary>
    /// Parsa il codice ZPL e crea una ZplLabel con tutti i comandi.
    /// Gestisce lo stato dei comandi modal e la validazione avanzata.
    /// </summary>
    public class ZplParser
    {
        private readonly ZplTokenizer _tokenizer;
        private readonly CommandFactory _commandFactory;

        /// <summary>
        /// Creo un nuovo parser ZPL.
        /// </summary>
        public ZplParser()
        {
            _tokenizer = new ZplTokenizer();
            _commandFactory = new CommandFactory();
        }

        /// <summary>
        /// Parso il codice ZPL e restituisco una label con i comandi parsati.
        /// </summary>
        /// <param name="zplCode">La stringa di codice ZPL grezzo.</param>
        /// <param name="throwOnError">Se true, lancia eccezioni per errori di parsing. Altrimenti li ignora.</param>
        /// <returns>Una ZplLabel contenente tutti i comandi parsati.</returns>
        public ZplLabel Parse(string zplCode, bool throwOnError = false)
        {
            var label = new ZplLabel();

            if (string.IsNullOrWhiteSpace(zplCode))
                return label;

            var context = new ZplParserContext();
            var tokens = _tokenizer.Tokenize(zplCode).ToList();

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                try
                {
                    // Aggiorno il contesto prima di parsare il comando
                    UpdateContextBeforeParsing(token, context);

                    // Creo il comando
                    var command = _commandFactory.CreateCommand(token.CommandCode);

                    if (command == null)
                    {
                        // Comando non supportato, lo salto
                        if (throwOnError)
                        {
                            throw new ZplParseException(
                                $"Comando non supportato: {token.Prefix}{token.CommandCode}",
                                token.Position, token.Length, token.CommandCode);
                        }
                        continue;
                    }

                    // Preparo i parametri in base al contesto
                    string parameters = PrepareParameters(token, context, command);

                    // Parso il comando
                    command.Parse(parameters);

                    // Applico il contesto al comando se necessario
                    ApplyContextToCommand(command, context);

                    label.Commands.Add(command);

                    UpdateLabelProperties(token, label, context);

                    // Aggiorno il contesto dopo il parsing
                    UpdateContextAfterParsing(token, command, context);
                }
                catch (Exception ex)
                {
                    if (throwOnError)
                    {
                        if (ex is ZplParseException)
                            throw;

                        throw new ZplParseException(
                            $"Errore nel parsing del comando {token.Prefix}{token.CommandCode}: {ex.Message}",
                            ex);
                    }
                    // Aggiungo il messaggio di errore alla label
                    label.ValidationMessages.Add($"Errore nel comando {token.Prefix}{token.CommandCode} (pos: {token.Position}): {ex.Message}");
                }
            }

            // Applico il contesto finale alla label (solo se hai aggiunto il metodo alla ZplLabel)
            if (label.GetType().GetMethod("ApplyContext") != null)
            {
                label.ApplyContext(context);
            }

            return label;
        }

        /// <summary>
        /// Aggiorno il contesto prima di parsare un comando.
        /// Gestisco comandi speciali come ^XA, ^DF, ecc.
        /// </summary>
        private void UpdateContextBeforeParsing(ZplToken token, ZplParserContext context)
        {
            switch (token.CommandCode)
            {
                case "XA":
                    // Inizio formato
                    context.InsideFormat = true;
                    context.ResetLabelState();
                    break;

                case "DF":
                    // Inizio download formato
                    context.InsideDownloadFormat = true;
                    // Il nome del formato � nei parametri (prima parte prima di ^XZ)
                    ExtractFormatName(token.Parameters, out string formatName);
                    context.CurrentDownloadFormatName = formatName;
                    break;
            }
        }

        /// <summary>
        /// Preparo i parametri per il comando in base al contesto.
        /// Gestisco casi speciali come ^FD con ^FH attivo.
        /// </summary>
        private string PrepareParameters(ZplToken token, ZplParserContext context, ZplCommand command)
        {
            string parameters = token.Parameters;

            // Gestione speciale per comandi di campo con ^FH attivo
            if ((token.CommandCode == "FD" || token.CommandCode == "SN" || token.CommandCode == "FV")
                && context.HexModeActive)
            {
                // Decodifico i valori hex nel formato _XX dove _ � l'hex indicator
                parameters = DecodeHexIndicators(parameters, context.HexIndicator);
            }

            // Gestione speciale per font commands con formato ^A0N,30,30
            if (command is FontCommand && token.CommandCode.Length > 1)
            {
                // Prependo l'identificatore font ai parametri
                parameters = token.CommandCode.Substring(1) + parameters;
            }

            return parameters;
        }

        /// <summary>
        /// Applico informazioni dal contesto al comando appena parsato.
        /// Posso estendere questo per applicare font corrente, posizione, ecc.
        /// </summary>
        private void ApplyContextToCommand(ZplCommand command, ZplParserContext context)
        {
            // Qui posso applicare valori di default dal contesto ai comandi
            // Esempio: se un comando non ha posizione esplicita, uso quella del contesto

            // Questo � un hook per future estensioni
        }

        /// <summary>
        /// Aggiorno il contesto dopo aver parsato un comando.
        /// I comandi modal influenzano i comandi successivi.
        /// </summary>
        private void UpdateContextAfterParsing(ZplToken token, ZplCommand command, ZplParserContext context)
        {
            switch (token.CommandCode)
            {
                case "FH":
                    // Field Hexadecimal Indicator attivo
                    context.HexModeActive = true;
                    if (!string.IsNullOrEmpty(token.Parameters))
                        context.HexIndicator = token.Parameters[0];
                    else
                        context.HexIndicator = '_';
                    break;

                case "CI":
                    // Change International Font/Encoding
                    if (int.TryParse(token.Parameters, out int encoding))
                        context.CurrentEncoding = encoding;
                    break;

                case "FO":
                    // Field Origin - aggiorno posizione corrente
                    var parts = token.Parameters.Split(',');
                    if (parts.Length >= 1 && int.TryParse(parts[0], out int x))
                        context.CurrentX = x;
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int y))
                        context.CurrentY = y;
                    break;

                case "A":
                    // Font command - aggiorno font corrente
                    var fontParts = token.Parameters.Split(',');
                    if (fontParts.Length >= 1)
                        context.CurrentFont = fontParts[0];
                    if (fontParts.Length >= 2 && int.TryParse(fontParts[1], out int height))
                        context.CurrentFontHeight = height;
                    if (fontParts.Length >= 3 && int.TryParse(fontParts[2], out int width))
                        context.CurrentFontWidth = width;
                    break;

                case "LH":
                    // Label Home - offset dell'etichetta
                    var lhParts = token.Parameters.Split(',');
                    if (lhParts.Length >= 1 && int.TryParse(lhParts[0], out int lhX))
                        context.LabelHomeX = lhX;
                    if (lhParts.Length >= 2 && int.TryParse(lhParts[1], out int lhY))
                        context.LabelHomeY = lhY;
                    break;

                case "XZ":
                    // Fine formato
                    if (context.InsideDownloadFormat)
                    {
                        // Salvo il formato scaricato
                        context.InsideDownloadFormat = false;
                        context.CurrentDownloadFormatName = null;
                    }
                    else
                    {
                        context.InsideFormat = false;
                    }
                    break;

                case "FS":
                    // Field Separator - resetto alcuni stati se necessario
                    // Per ora non faccio nulla
                    break;
            }
        }

        /// <summary>
        /// Decodifico gli indicatori hex nel formato _XX dove _ � l'hex indicator.
        /// Esempio: "Test_0A_0DHello" con indicator '_' diventa "Test\n\rHello"
        /// </summary>
        private string DecodeHexIndicators(string text, char hexIndicator)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf(hexIndicator) == -1)
                return text;

            var result = new System.Text.StringBuilder(text.Length);
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == hexIndicator && i + 2 < text.Length)
                {
                    // Tento di parsare i prossimi 2 caratteri come hex
                    string hexValue = text.Substring(i + 1, 2);
                    if (IsHexString(hexValue))
                    {
                        // Converto in carattere
                        int charCode = Convert.ToInt32(hexValue, 16);
                        result.Append((char)charCode);
                        i += 3; // Salto indicator + 2 cifre hex
                        continue;
                    }
                }

                result.Append(text[i]);
                i++;
            }

            return result.ToString();
        }

        /// <summary>
        /// Verifico se una stringa � valida hex (0-9, A-F).
        /// </summary>
        private bool IsHexString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            foreach (char c in str)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Estraggo il nome del formato da ^DF.
        /// Formato: d:o.x (device:objectname.extension)
        /// </summary>
        private void ExtractFormatName(string parameters, out string formatName)
        {
            formatName = parameters;

            // Rimuovo eventuale device prefix (R:, E:, etc.)
            if (parameters.Contains(":"))
            {
                formatName = parameters.Substring(parameters.IndexOf(':') + 1);
            }

            // Prendo solo il nome prima di virgole o altri comandi
            int commaIndex = formatName.IndexOf(',');
            if (commaIndex > 0)
            {
                formatName = formatName.Substring(0, commaIndex);
            }

            formatName = formatName.Trim();
        }

        /// <summary>
        /// Aggiorno le propriet� della label in base ai comandi parsati.
        /// </summary>
        private void UpdateLabelProperties(ZplToken token, ZplLabel label, ZplParserContext context)
        {
            switch (token.CommandCode)
            {
                case "PW":
                    // Print Width
                    if (int.TryParse(token.Parameters, out int width))
                        label.WidthDots = width;
                    break;

                case "LL":
                    // Label Length
                    if (int.TryParse(token.Parameters, out int height))
                        label.HeightDots = height;
                    break;

                case "LH":
                    // Label Home (gi� gestito nel context, ma lo setto anche qui)
                    var parts = token.Parameters.Split(',');
                    if (parts.Length >= 1 && int.TryParse(parts[0], out int lhX))
                        label.LabelHomeX = lhX;
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int lhY))
                        label.LabelHomeY = lhY;
                    break;

                case "PO":
                    // Print Orientation (solo se hai aggiunto la propriet� alla ZplLabel)
                    if (!string.IsNullOrEmpty(token.Parameters))
                        label.PrintOrientation = char.ToUpperInvariant(token.Parameters[0]);
                    break;

                case "MM":
                    // Media Type (solo se hai aggiunto la propriet� alla ZplLabel)
                    if (!string.IsNullOrEmpty(token.Parameters))
                        label.MediaType = char.ToUpperInvariant(token.Parameters[0]);
                    break;
            }
        }

        /// <summary>
        /// Valido il codice ZPL e restituisco eventuali warning o errori.
        /// </summary>
        /// <param name="zplCode">La stringa di codice ZPL grezzo.</param>
        /// <returns>Lista di messaggi di validazione.</returns>
        public IEnumerable<string> Validate(string zplCode)
        {
            var messages = new List<string>();

            if (string.IsNullOrWhiteSpace(zplCode))
            {
                messages.Add("Il codice ZPL � vuoto");
                return messages;
            }

            bool hasStartFormat = false;
            bool hasEndFormat = false;
            int startFormatCount = 0;
            int endFormatCount = 0;
            var tokens = _tokenizer.Tokenize(zplCode).ToList();

            foreach (var token in tokens)
            {
                if (token.CommandCode == "XA")
                {
                    hasStartFormat = true;
                    startFormatCount++;
                }
                else if (token.CommandCode == "XZ")
                {
                    hasEndFormat = true;
                    endFormatCount++;
                }
                else if (!_commandFactory.IsSupported(token.CommandCode))
                {
                    messages.Add($"Comando non supportato: {token.Prefix}{token.CommandCode} (posizione: {token.Position})");
                }
            }

            if (!hasStartFormat)
                messages.Add("Manca il comando ^XA (Start Format)");

            if (!hasEndFormat)
                messages.Add("Manca il comando ^XZ (End Format)");

            if (startFormatCount != endFormatCount)
                messages.Add($"Numero disallineato di ^XA ({startFormatCount}) e ^XZ ({endFormatCount})");

            return messages;
        }
    }
}
