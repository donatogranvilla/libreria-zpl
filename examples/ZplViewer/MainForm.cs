using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SkiaSharp;
using ZplRenderer;

namespace ZplViewer
{
    public class MainForm : Form
    {
        private SplitContainer splitContainer;
        private TextBox txtZplInput;
        private PictureBox pbPreview;
        private Button btnRender;
        private Panel leftPanel;

        public MainForm()
        {
            this.Text = "ZPL Viewer Example";
            this.Size = new Size(1200, 800);

            InitializeComponent();
            
            // Set default ZPL
            txtZplInput.Text = @"^XA
^PW812^LL1200
^CI28
^FO50,50^A0N,50,50^FDZPL ENHANCED DEMO^FS
^FO50,120^A0N,30,30^FD1. Codabar Barcode (^BK):^FS
^FO50,160^BKN,N,50^FDA12345B^FS
^FO50,250^A0N,30,30^FD2. Interleaved 2of5 (^B2):^FS
^FO50,290^B2N,50,Y,N,N^FD1234567890^FS
^FO50,380^A0N,30,30^FD3. International Chars (^CI28):^FS
^FO50,420^A0N,30,30^FDUTF8: È à ñ ö €^FS
^FO50,500^A0N,30,30^FDHex: _C3_89_C3_A0_C3_B1_C3_B6_E2_82_AC^FS
^FO50,550^GB700,0,2^FS
^FO50,580^A0N,30,30^FD4. Global Orientation (^PO):^FS
^FO50,620^A0N,25,25^FD(This label is Normal, ^POI would invert it)^FS
^FO50,660^A0N,30,30^FD5. Label Shift (^LS):^FS
^FO50,700^A0N,25,25^FD(Currently 0. try changing code to ^LS50)^FS
^XZ";
        }

        private void InitializeComponent()
        {
            splitContainer = new SplitContainer();
            txtZplInput = new TextBox();
            pbPreview = new PictureBox();
            btnRender = new Button();
            leftPanel = new Panel();

            // Split Container
            splitContainer.Dock = DockStyle.Fill;
            this.Controls.Add(splitContainer);

            // Left Panel (Input + Button)
            leftPanel.Dock = DockStyle.Fill;
            splitContainer.Panel1.Controls.Add(leftPanel);

            // Button
            btnRender.Text = "Render ZPL";
            btnRender.Dock = DockStyle.Bottom;
            btnRender.Height = 50;
            btnRender.Click += BtnRender_Click;
            leftPanel.Controls.Add(btnRender);

            // TextBox
            txtZplInput.Multiline = true;
            txtZplInput.ScrollBars = ScrollBars.Vertical;
            txtZplInput.Dock = DockStyle.Fill;
            txtZplInput.Font = new Font("Consolas", 10);
            leftPanel.Controls.Add(txtZplInput);

            // Preview
            pbPreview.Dock = DockStyle.Fill;
            pbPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pbPreview.BackColor = Color.LightGray;
            splitContainer.Panel2.Controls.Add(pbPreview);
        }

        private void BtnRender_Click(object? sender, EventArgs e)
        {
            try
            {
                var engine = new ZplEngine();
                using (var skBitmap = engine.Render(txtZplInput.Text))
                {
                    // Convert SkiaSharp Bitmap to Windows Forms Bitmap (via Stream)
                    using (var image = SKImage.FromBitmap(skBitmap))
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (var stream = new MemoryStream())
                    {
                        data.SaveTo(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        
                        // Dispose old image if any
                        if (pbPreview.Image != null) 
                            pbPreview.Image.Dispose();
                        
                        pbPreview.Image = new Bitmap(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rendering Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
