using System;
using ZplRenderer.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// Abstract base class for all ZPL commands.
    /// </summary>
    public abstract class ZplCommand
    {
        /// <summary>
        /// The ZPL command code (e.g., "FO", "FD", "A").
        /// </summary>
        public abstract string CommandCode { get; }

        /// <summary>
        /// Executes the command using the provided render context.
        /// </summary>
        /// <param name="context">The rendering context containing state and graphics.</param>
        public abstract void Execute(RenderContext context);

        /// <summary>
        /// Parses the command parameters from the raw parameter string.
        /// </summary>
        /// <param name="parameters">The raw parameter string from the ZPL token.</param>
        public abstract void Parse(string parameters);

        /// <summary>
        /// Helper method to split comma-separated parameters.
        /// </summary>
        protected string[] SplitParameters(string parameters)
        {
            if (string.IsNullOrEmpty(parameters))
                return new string[0];
            
            return parameters.Split(new[] { ',' }, StringSplitOptions.None);
        }

        /// <summary>
        /// Helper method to parse an integer parameter with a default value.
        /// </summary>
        protected int ParseInt(string value, int defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;
            
            return int.TryParse(value.Trim(), out int result) ? result : defaultValue;
        }

        /// <summary>
        /// Helper method to parse a character parameter with a default value.
        /// </summary>
        protected char ParseChar(string value, char defaultValue = ' ')
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;
            
            return value.Trim()[0];
        }

        /// <summary>
        /// Helper method to parse a boolean (Y/N) parameter.
        /// </summary>
        protected bool ParseYesNo(string value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;
            
            char c = char.ToUpperInvariant(value.Trim()[0]);
            return c == 'Y';
        }
    }
}
