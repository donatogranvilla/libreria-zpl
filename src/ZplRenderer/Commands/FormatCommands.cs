using ZplRenderer.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// ^XA - Start Format command. Marks the beginning of a label format.
    /// </summary>
    public class StartFormatCommand : ZplCommand
    {
        public override string CommandCode => "XA";

        public override void Execute(RenderContext context)
        {
            // Reset context to default state for new label
            context.CurrentX = 0;
            context.CurrentY = 0;
            context.FieldData = null;
            context.FieldOrientation = FieldOrientation.Normal;
        }

        public override void Parse(string parameters)
        {
            // ^XA has no parameters
        }
    }

    /// <summary>
    /// ^XZ - End Format command. Marks the end of a label format.
    /// </summary>
    public class EndFormatCommand : ZplCommand
    {
        public override string CommandCode => "XZ";

        public override void Execute(RenderContext context)
        {
            // End of label - nothing specific to do
        }

        public override void Parse(string parameters)
        {
            // ^XZ has no parameters
        }
    }

    /// <summary>
    /// ^FS - Field Separator command. Ends a field definition.
    /// </summary>
    public class FieldSeparatorCommand : ZplCommand
    {
        public override string CommandCode => "FS";

        public override void Execute(RenderContext context)
        {
            // PendingBarcode should have been consumed by ^FD.
            // If it wasn't, it implies a malformed ZPL sequence (e.g. ^BC without ^FD).
            // In that case, we should probably discard it or render a placeholder?
            // ZPL spec says if no ^FD, command is ignored.
            context.PendingBarcode = null;

            // Clear pending field data after field is complete
            // (Wait, ZplCommand logic usually clears it or the Parser handles it? 
            // In the previous logic, FieldData was assigned in ^FD.
            // Here we just ensure we reset for the next field.
            context.FieldData = null;
        }

        public override void Parse(string parameters)
        {
            // ^FS has no parameters
        }
    }

    /// <summary>
    /// ^PW - Print Width. Sets the print width.
    /// </summary>
    public class PrintWidthCommand : ZplCommand
    {
        public override string CommandCode => "PW";
        public int Width { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.PrintWidth = Width;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Width = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
        }
    }

    /// <summary>
    /// ^LL - Label Length. Sets the length of the label.
    /// </summary>
    public class LabelLengthCommand : ZplCommand
    {
        public override string CommandCode => "LL";
        public int Length { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.LabelLength = Length;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Length = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
        }
    }

    /// <summary>
    /// ^PQ - Print Quantity. Control the quantity of labels to print.
    /// </summary>
    public class PrintQuantityCommand : ZplCommand
    {
        public override string CommandCode => "PQ";
        public int Quantity { get; private set; } = 1;

        public override void Execute(RenderContext context)
        {
            // Metadata only, no rendering effect
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Quantity = parts.Length > 0 ? ParseInt(parts[0], 1) : 1;
        }
    }

    /// <summary>
    /// ^MD - Media Darkness. Adjusts the darkness of the printed image.
    /// </summary>
    public class MediaDarknessCommand : ZplCommand
    {
        public override string CommandCode => "MD";
        public int DarknessLevel { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.MediaDarkness = DarknessLevel;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            DarknessLevel = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
        }
    }

    /// <summary>
    /// ^PR - Print Rate. Determines the speed of the print.
    /// </summary>
    public class PrintRateCommand : ZplCommand
    {
        public override string CommandCode => "PR";
        public string Speed { get; private set; } = "A";

        public override void Execute(RenderContext context)
        {
            context.PrintSpeed = Speed;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0) Speed = parts[0];
        }
    }
}
