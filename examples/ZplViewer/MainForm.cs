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
^MMT
^PW812
^LL812
^LS0
^CI28
^FT40,90^A0N,70,70^FH^FDChiola^FS
^FT220,90^A0N,20,20^FH^FDUNA STORIA DI SAPORI^FS
^FT40,135^A0N,18,18^FH^FDProduzione:^FS
^FT40,155^A0N,16,16^FH^FDVia Nazionale 14 - 14011 Baldichieri d'Asti (AT)^FS
^FO650,40^GE110,70,2,B^FS
^FT685,60^A0N,14,14^FH^FDITALIA^FS
^FT682,85^A0N,20,20^FH^FD725M^FS
^FT698,105^A0N,14,14^FH^FDCE^FS
^FT420,60^A0N,18,18^FH^FDSoc. Agr. Gruppo Clemme S.S.^FS
^FT50,200^A0N,35,35^FB710,1,0,C^FH^FD 881852 ^FS
^FT50,238^A0N,35,35^FB710,1,0,C^FH^FD 60232054 ^FS
^FO50,245^GB710,3,3^FS
^FT50,285^A0N,20,20^FH^FDData Produzione/Production date^FS
^FT340,285^A0N,30,30^FH^FD 20/10/2026 ^FS
^FO50,480^BY2,2,40^BEB,,Y,N^FD8001234567890^FS
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
