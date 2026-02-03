using System;
using System.Collections.Generic;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// Factory for creating ZPL command instances from tokens.
    /// </summary>
    public class CommandFactory
    {
        private static readonly Dictionary<string, Func<ZplCommand>> CommandCreators = 
            new Dictionary<string, Func<ZplCommand>>(StringComparer.OrdinalIgnoreCase)
        {
            // Format commands
            { "XA", () => new StartFormatCommand() },
            { "XZ", () => new EndFormatCommand() },
            { "FS", () => new FieldSeparatorCommand() },
            
            // Basic format commands
            { "PW", () => new PrintWidthCommand() },
            { "LL", () => new LabelLengthCommand() },
            { "PQ", () => new PrintQuantityCommand() },
            { "MD", () => new MediaDarknessCommand() },
            { "PR", () => new PrintRateCommand() },
            
            // Field commands
            { "FO", () => new FieldOriginCommand() },
            { "FT", () => new FieldTypesetCommand() },
            { "FD", () => new FieldDataCommand() },
            { "FB", () => new FieldBlockCommand() },
            { "FR", () => new FieldReverseCommand() },
            { "FN", () => new FieldNumberCommand() },
            { "FH", () => new FieldHexCommand() },
            { "SN", () => new SerializationCommand() },
            
            { "FW", () => new FieldOrientationCommand() },
            { "LH", () => new LabelHomeCommand() },
            
            // Font/Text commands
            { "A", () => new FontCommand() },
            { "CF", () => new ChangeDefaultFontCommand() },
            { "A@", () => new ScalableFontCommand() },
            { "CI", () => new ChangeInternationalFontCommand() },

            
            // Graphic commands
            { "GB", () => new GraphicBoxCommand() },
            { "GD", () => new GraphicDiagonalCommand() },
            { "GC", () => new GraphicCircleCommand() },
            { "GE", () => new GraphicEllipseCommand() },
            { "GF", () => new GraphicFieldCommand() },
            { "DG", () => new DownloadGraphicsCommand() },
            { "XG", () => new RecallGraphicCommand() },
            { "IM", () => new ImageMoveCommand() },
            
            // Barcode commands
            { "BY", () => new BarcodeDefaultsCommand() },
            { "BC", () => new BarcodeCode128Command() },
            { "B3", () => new Code39Command() },
            { "BE", () => new EAN13Command() },
            { "BA", () => new Code93Command() },
            { "BU", () => new UPCACommand() },
            { "BQ", () => new BarcodeQRCommand() },
            { "BX", () => new DataMatrixCommand() },
            { "B7", () => new PDF417Command() },
            { "B0", () => new AztecCommand() },
            { "BD", () => new MaxiCodeCommand() },
        };

        /// <summary>
        /// Creates a ZPL command from a command code.
        /// </summary>
        /// <param name="commandCode">The command code (e.g., "FO", "FD").</param>
        /// <returns>A new command instance, or null if command is not supported.</returns>
        public ZplCommand CreateCommand(string commandCode)
        {
            // Check registered commands first (handles A, A@, etc.)
            if (CommandCreators.TryGetValue(commandCode, out var creator))
            {
                return creator();
            }

            // Handle font commands which start with 'A' followed by font identifier (like A0, AE)
            if (commandCode.Length >= 1 && commandCode[0] == 'A')
            {
                return new FontCommand();
            }

            return null; // Unknown command
        }

        /// <summary>
        /// Checks if a command code is supported.
        /// </summary>
        public bool IsSupported(string commandCode)
        {
            if (string.IsNullOrEmpty(commandCode))
                return false;

            // Font commands
            if (commandCode.Length >= 1 && commandCode[0] == 'A')
                return true;

            return CommandCreators.ContainsKey(commandCode);
        }

        /// <summary>
        /// Gets a list of all supported command codes.
        /// </summary>
        public IEnumerable<string> GetSupportedCommands()
        {
            return CommandCreators.Keys;
        }
    }
}
