using System;
using System.Collections.Generic;
using ZplRenderer.Commands;

namespace ZplRenderer.Core
{
    /// <summary>
    /// Parses ZPL code and creates a ZplLabel with all commands.
    /// </summary>
    public class ZplParser
    {
        private readonly ZplTokenizer _tokenizer;
        private readonly CommandFactory _commandFactory;

        /// <summary>
        /// Creates a new ZPL parser.
        /// </summary>
        public ZplParser()
        {
            _tokenizer = new ZplTokenizer();
            _commandFactory = new CommandFactory();
        }

        /// <summary>
        /// Parses ZPL code and returns a label with parsed commands.
        /// </summary>
        /// <param name="zplCode">The raw ZPL code string.</param>
        /// <returns>A ZplLabel containing all parsed commands.</returns>
        public ZplLabel Parse(string zplCode)
        {
            var label = new ZplLabel();

            if (string.IsNullOrWhiteSpace(zplCode))
                return label;

            var tokens = _tokenizer.Tokenize(zplCode);
            ZplCommand pendingFontCommand = null;
            ZplCommand pendingBarcodeCommand = null;

            foreach (var token in tokens)
            {
                var command = _commandFactory.CreateCommand(token.CommandCode);
                
                if (command == null)
                {
                    // Skip unsupported commands
                    continue;
                }

                // Parse command parameters
                string parameters = token.Parameters;

                // For font commands, we need to handle the special format ^A0N,30,30
                if (command is FontCommand fontCmd && token.CommandCode.Length > 1)
                {
                    // Prepend the font identifier to parameters
                    parameters = token.CommandCode.Substring(1) + 
                        (string.IsNullOrEmpty(parameters) ? "" : "," + parameters);
                    // Reset command code for parsing
                }

                command.Parse(parameters);
                label.Commands.Add(command);

                // Track font and barcode commands for text rendering
                if (command is FontCommand)
                {
                    pendingFontCommand = command;
                    pendingBarcodeCommand = null;
                }
                else if (command is BarcodeCode128Command || command is BarcodeQRCommand)
                {
                    pendingBarcodeCommand = command;
                    pendingFontCommand = null;
                }
                else if (command is FieldDataCommand)
                {
                    // After field data, if there's a pending font command (not barcode),
                    // we need to render text
                    if (pendingFontCommand != null && pendingBarcodeCommand == null)
                    {
                        var textRender = new TextRenderCommand();
                        textRender.Parse("");
                        label.Commands.Add(textRender);
                    }
                }
                else if (command is FieldSeparatorCommand)
                {
                    // Reset pending commands after field separator
                    pendingFontCommand = null;
                    pendingBarcodeCommand = null;
                }
            }

            return label;
        }

        /// <summary>
        /// Validates ZPL code and returns any warnings or errors.
        /// </summary>
        /// <param name="zplCode">The raw ZPL code string.</param>
        /// <returns>List of validation messages.</returns>
        public IEnumerable<string> Validate(string zplCode)
        {
            var messages = new List<string>();

            if (string.IsNullOrWhiteSpace(zplCode))
            {
                messages.Add("ZPL code is empty");
                return messages;
            }

            bool hasStartFormat = false;
            bool hasEndFormat = false;

            foreach (var token in _tokenizer.Tokenize(zplCode))
            {
                if (token.CommandCode == "XA")
                    hasStartFormat = true;
                else if (token.CommandCode == "XZ")
                    hasEndFormat = true;
                else if (!_commandFactory.IsSupported(token.CommandCode))
                {
                    messages.Add($"Unsupported command: ^{token.CommandCode}");
                }
            }

            if (!hasStartFormat)
                messages.Add("Missing ^XA (Start Format) command");
            if (!hasEndFormat)
                messages.Add("Missing ^XZ (End Format) command");

            return messages;
        }
    }
}
