using ZplRenderer.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// ^BY - Barcode Field Default command. Sets default barcode module width and ratio.
    /// Format: ^BYw,r,h
    /// w = module width (1-10 dots)
    /// r = wide bar to narrow bar ratio (2.0-3.0)
    /// h = height of bars (1-32000 dots)
    /// </summary>
    public class BarcodeDefaultsCommand : ZplCommand
    {
        public override string CommandCode => "BY";

        /// <summary>Module (narrow bar) width in dots.</summary>
        public int ModuleWidth { get; private set; } = 2;

        /// <summary>Wide bar to narrow bar ratio.</summary>
        public float Ratio { get; private set; } = 3.0f;

        /// <summary>Default bar height in dots.</summary>
        public int Height { get; private set; } = 100;

        public override void Execute(RenderContext context)
        {
            context.ModuleWidth = ModuleWidth;
            context.ModuleRatio = Ratio;
            context.BarcodeHeight = Height;
        }

        public override void Parse(string parameters)
        {
            var parts = SplitParameters(parameters);
            
            ModuleWidth = parts.Length > 0 ? ParseInt(parts[0], 2) : 2;
            
            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                if (float.TryParse(parts[1].Trim().Replace('.', ','), out float ratio) ||
                    float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out ratio))
                {
                    Ratio = ratio;
                }
            }
            
            Height = parts.Length > 2 ? ParseInt(parts[2], 100) : 100;

            // Clamp values
            if (ModuleWidth < 1) ModuleWidth = 1;
            if (ModuleWidth > 10) ModuleWidth = 10;
            if (Ratio < 2.0f) Ratio = 2.0f;
            if (Ratio > 3.0f) Ratio = 3.0f;
        }
    }
}
