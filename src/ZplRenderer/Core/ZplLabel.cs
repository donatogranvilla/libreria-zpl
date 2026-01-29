using System;
using System.Collections.Generic;

namespace ZplRenderer.Core
{
    /// <summary>
    /// Represents a parsed ZPL label containing all commands to render.
    /// </summary>
    public class ZplLabel
    {
        /// <summary>
        /// List of parsed ZPL commands in execution order.
        /// </summary>
        public List<Commands.ZplCommand> Commands { get; } = new List<Commands.ZplCommand>();

        /// <summary>
        /// Label width in dots (set by ^PW command or default).
        /// </summary>
        public int WidthDots { get; set; } = 812; // ~4 inches at 203 DPI

        /// <summary>
        /// Label height in dots (set by ^LL command or default).
        /// </summary>
        public int HeightDots { get; set; } = 1218; // ~6 inches at 203 DPI

        /// <summary>
        /// Label home X offset (set by ^LH command).
        /// </summary>
        public int LabelHomeX { get; set; } = 0;

        /// <summary>
        /// Label home Y offset (set by ^LH command).
        /// </summary>
        public int LabelHomeY { get; set; } = 0;
    }
}
