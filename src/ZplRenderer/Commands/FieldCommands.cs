using System;
using ZplRenderer.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// ^FO - Field Origin command. Sets the X,Y position for the next field.
    /// Format: ^FOx,y,z
    /// </summary>
    public class FieldOriginCommand : ZplCommand
    {
        public override string CommandCode => "FO";

        /// <summary>X position in dots from left edge.</summary>
        public int X { get; private set; }

        /// <summary>Y position in dots from top edge.</summary>
        public int Y { get; private set; }

        /// <summary>Justification (0=left, 1=right, 2=auto).</summary>
        public int Justification { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.CurrentX = X;
            context.CurrentY = Y;
            // Reset field specific settings
            context.IsBaselinePosition = false;
            context.FieldBlockWidth = 0;
            context.FieldBlockMaxLines = 1;
            context.IsReversePrint = false;
            context.HexReferenceIndicator = null;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            X = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
            Y = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            Justification = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
        }
    }

    /// <summary>
    /// ^FT - Field Typeset command. Sets the X,Y position relative to baseline.
    /// Format: ^FTx,y,z
    /// </summary>
    public class FieldTypesetCommand : ZplCommand
    {
        public override string CommandCode => "FT";

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Justification { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.CurrentX = X;
            context.CurrentY = Y;
            context.IsBaselinePosition = true;
            // Reset others
            context.FieldBlockWidth = 0;
            context.FieldBlockMaxLines = 1;
            context.IsReversePrint = false;
            context.HexReferenceIndicator = null;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            X = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
            Y = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
            Justification = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
        }
    }

    /// <summary>
    /// ^FB - Field Block command. Allows printing text into a defined block.
    /// Format: ^FBw,l,s,j,h
    /// w = width of block in dots
    /// l = max number of lines
    /// s = space between lines in dots (added to font height)
    /// j = justification (L, C, R, J)
    /// h = hanging indent (dots)
    /// </summary>
    public class FieldBlockCommand : ZplCommand
    {
        public override string CommandCode => "FB";

        public int Width { get; private set; }
        public int MaxLines { get; private set; } = 1;
        public int LineSpacing { get; private set; } = 0;
        public char Alignment { get; private set; } = 'L';
        public int HangingIndent { get; private set; } = 0;

        public override void Execute(RenderContext context)
        {
            context.FieldBlockWidth = Width;
            context.FieldBlockMaxLines = MaxLines;
            context.FieldBlockAlignment = Alignment;
            // TODO: Store LineSpacing and HangingIndent if RenderContext supports them
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Width = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
            MaxLines = parts.Length > 1 ? ParseInt(parts[1], 1) : 1;
            LineSpacing = parts.Length > 2 ? ParseInt(parts[2], 0) : 0;
            Alignment = parts.Length > 3 ? ParseChar(parts[3], 'L') : 'L';
            HangingIndent = parts.Length > 4 ? ParseInt(parts[4], 0) : 0;
        }
    }

    /// <summary>
    /// ^FR - Field Reverse Print command. Prints white on black background.
    /// Format: ^FR
    /// </summary>
    public class FieldReverseCommand : ZplCommand
    {
        public override string CommandCode => "FR";

        public override void Execute(RenderContext context)
        {
            context.IsReversePrint = true;
        }

        public override void Parse(string parameters)
        {
            // No parameters
        }
    }

    /// <summary>
    /// ^FD - Field Data command. Defines the data/text content of a field.
    /// Format: ^FDdata
    /// </summary>
    public class FieldDataCommand : ZplCommand
    {
        public override string CommandCode => "FD";

        /// <summary>The field data content.</summary>
        public string Data { get; private set; }

        public override void Execute(RenderContext context)
        {
            // Apply Hex Decoding if Indicator is set
            string finalData = Data;
            if (context.HexReferenceIndicator.HasValue)
            {
                finalData = DecodeHex(Data, context.HexReferenceIndicator.Value);
            }
            
            // Check if we have a Pending Barcode element waiting for data
            if (context.PendingBarcode is ZplRenderer.Elements.ZplBarcode barcode)
            {
                barcode.Content = finalData;
                context.Elements.Add(barcode);
                context.PendingBarcode = null;
            }
            else
            {
                // Create Text Element
                var textField = new ZplRenderer.Elements.ZplTextField
                {
                    X = context.AbsoluteX,
                    Y = context.AbsoluteY,
                    Text = finalData,
                    Font = new SkiaSharp.SKFont(context.CurrentFont.Typeface, context.CurrentFont.Size), 
                    Orientation = context.FieldOrientation,
                    IsReversePrint = context.IsReversePrint,
                    OriginType = context.IsBaselinePosition ? ZplRenderer.Elements.ElementOriginType.Baseline : ZplRenderer.Elements.ElementOriginType.TopLeft,
                    ScaleX = (context.FontWidth > 0 && context.FontHeight > 0) ? (float)context.FontWidth / context.FontHeight : 1.0f
                };

                // Apply Field Block if active
                if (context.FieldBlockWidth > 0)
                {
                    textField.FieldBlock = new ZplRenderer.Elements.ZplFieldBlock
                    {
                         Width = context.FieldBlockWidth,
                         MaxLines = context.FieldBlockMaxLines,
                         Alignment = context.FieldBlockAlignment
                    };
                }

                context.Elements.Add(textField);
            }

            // Important: After printing a field, some ZPL state might reset, 
            // but usually ^FS resets it. ^FD itself just adds data.
        }

        private string DecodeHex(string input, char indicator)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.IndexOf(indicator) < 0) return input;

            var resultBytes = new System.Collections.Generic.List<byte>();
            
            // Assume UTF-8 handling for ^CI28 compliance
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == indicator)
                {
                    if (i + 2 < input.Length && IsHex(input[i+1]) && IsHex(input[i+2]))
                    {
                        string hex = input.Substring(i + 1, 2);
                        resultBytes.Add(Convert.ToByte(hex, 16));
                        i += 2;
                    }
                    else if (i + 1 < input.Length && input[i+1] == indicator)
                    {
                        // Escaped indicator?
                         resultBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(new[] { indicator }));
                         i++;
                    }
                    else
                    {
                        // Just the indicator char (not a valid escape)
                        resultBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(new[] { c }));
                    }
                }
                else
                {
                    resultBytes.AddRange(System.Text.Encoding.UTF8.GetBytes(new[] { c }));
                }
            }
            
            return System.Text.Encoding.UTF8.GetString(resultBytes.ToArray());
        }

        private bool IsHex(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        public override void Parse(string parameters)
        {
            // Field data is everything after ^FD until ^FS or next command
            Data = parameters ?? string.Empty;
        }
    }

    /// <summary>
    /// ^FH - Field Hexadecimal Indicator.
    /// Format: ^FHa
    /// </summary>
    public class FieldHexCommand : ZplCommand
    {
        public override string CommandCode => "FH";
        public char Indicator { get; private set; } = '_';

        public override void Execute(RenderContext context)
        {
            context.HexReferenceIndicator = Indicator;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Indicator = parts.Length > 0 ? ParseChar(parts[0], '_') : '_';
        }
    }

    /// <summary>
    /// ^FN - Field Number command. 
    /// Format: ^FN#
    /// </summary>
    public class FieldNumberCommand : ZplCommand
    {
        public override string CommandCode => "FN";
        public int Number { get; private set; }

        public override void Execute(RenderContext context)
        {
            // Used for templating. In preview mode, we usually just proceed.
            // If we had a variable map, we would look it up here.
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            Number = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
        }
    }
    
    /// <summary>
    /// ^SN - Serialization Data command.
    /// Format: ^SNv,n,z
    /// </summary>
    public class SerializationCommand : ZplCommand
    {
        public override string CommandCode => "SN";
        public string StartValue { get; private set; }
        public int Increment { get; private set; } = 1;
        public bool PadZeros { get; private set; } = false;

        public override void Execute(RenderContext context)
        {
            context.FieldData = StartValue;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            StartValue = parts.Length > 0 ? parts[0] : "";
            Increment = parts.Length > 1 ? ParseInt(parts[1], 1) : 1;
            PadZeros = parts.Length > 2 ? ParseYesNo(parts[2], false) : false;
        }
    }

    /// <summary>
    /// ^FW - Field Orientation (default). Sets default rotation for all fields.
    /// Format: ^FWr,z
    /// </summary>
    public class FieldOrientationCommand : ZplCommand
    {
        public override string CommandCode => "FW";

        /// <summary>Default field orientation.</summary>
        public FieldOrientation Orientation { get; private set; }

        /// <summary>Default justification.</summary>
        public int Justification { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.FieldOrientation = Orientation;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                Orientation = RenderContext.ParseOrientation(parts[0][0]);
            }
            else
            {
                Orientation = FieldOrientation.Normal;
            }
            
            Justification = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
        }
    }

    /// <summary>
    /// ^LH - Label Home command. Sets the label home position.
    /// Format: ^LHx,y
    /// </summary>
    public class LabelHomeCommand : ZplCommand
    {
        public override string CommandCode => "LH";

        public int X { get; private set; }
        public int Y { get; private set; }

        public override void Execute(RenderContext context)
        {
            context.LabelHomeX = X;
            context.LabelHomeY = Y;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            X = parts.Length > 0 ? ParseInt(parts[0], 0) : 0;
            Y = parts.Length > 1 ? ParseInt(parts[1], 0) : 0;
        }
    }
}
