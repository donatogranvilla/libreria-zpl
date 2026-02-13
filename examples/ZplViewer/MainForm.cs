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
^PW600^LL800
^CI28

^FO20,20^GB560,760,3^FS

^FB560,1,0,C,0
^FO20,40^A0N,45,45^FDPasta Fresca Bianchi^FS
^FB560,1,0,C,0
^FO20,95^A0N,28,28^FDVia Milano 22 - Bologna^FS

^FO30,135^GB540,2,2^FS

^FO40,160
^BQN,2,5
^FDQA,https://example.com/product^FS

^FO340,160
^B0N,5
^FDLAS99831^FS

^FO30,330^GB540,2,2^FS

^FO40,350^A0N,30,30^FDProduct:^FS
^FO300,350^A0N,30,30^FDLasagne^FS
^FO40,390^A0N,30,30^FDArticle:^FS
^FO300,390^A0N,30,30^FDLAS99831^FS
^FO40,430^A0N,30,30^FDLot:^FS
^FO300,430^A0N,30,30^FDL77821^FS
^FO40,470^A0N,30,30^FDWeight:^FS
^FO300,470^A0N,30,30^FD0.85 kg^FS

^FO30,515^GB540,2,2^FS

^FO350,650^GE200,100,2^FS
^FB200,1,0,C,0
^FO350,685^A0N,26,26^FDITALIA^FS
^FB200,1,0,C,0
^FO350,715^A0N,20,20^FDCE^FS

^FO40,660^BY2,3,60
^BCN,,Y,N
^FDLAS99831-L77821^FS

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
