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
            // ^FS segna la fine di un campo ZPL. Secondo la specifica ZPL II,
            // TUTTE le impostazioni per-campo devono essere resettate qui,
            // inclusi Field Block (^FB), Reverse Print (^FR), e Hex Indicator (^FH).
            
            // Reset barcode pendente (se ^FD non lo ha consumato, il comando è ignorato)
            context.PendingBarcode = null;

            // Reset dati campo
            context.FieldData = null;

            // Reset stato Field Block (^FB) — il blocco è per-campo, non persistente
            context.FieldBlockWidth = 0;
            context.FieldBlockMaxLines = 1;
            context.FieldBlockAlignment = 'L';

            // Reset Reverse Print (^FR) — si applica solo al campo corrente
            context.IsReversePrint = false;

            // Reset Hex Indicator (^FH) — si applica solo al campo corrente
            context.HexReferenceIndicator = null;
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
    /// <summary>
    /// ^PO - Print Orientation. Inverts the label output 180 degrees.
    /// Format: ^POa (N = Normal, I = Inverted)
    /// </summary>
    public class PrintOrientationCommand : ZplCommand
    {
        public override string CommandCode => "PO";
        public PrintOrientation Orientation { get; private set; } = PrintOrientation.Normal;

        public override void Execute(RenderContext context)
        {
            context.PrintOrientation = Orientation;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            if (parts.Length > 0 && parts[0].Length > 0)
            {
                 char orient = char.ToUpperInvariant(parts[0][0]);
                 Orientation = (orient == 'I') ? PrintOrientation.Inverted : PrintOrientation.Normal;
            }
        }
    }

    /// <summary>
    /// ^LT - Label Top. Moves the entire label vertically.
    /// Format: ^LTz
    /// </summary>
    public class LabelTopCommand : ZplCommand
    {
        public override string CommandCode => "LT";
        public int Offset { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.LabelTop = Offset;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Offset = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
        }
    }

    /// <summary>
    /// ^LS - Label Shift. Shifts all fields horizontally.
    /// Format: ^LSz
    /// </summary>
    public class LabelShiftCommand : ZplCommand
    {
        public override string CommandCode => "LS";
        public int Shift { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.LabelShiftX = Shift;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Shift = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
        }
    }
}
