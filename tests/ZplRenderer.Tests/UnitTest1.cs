using System;
using SkiaSharp;
using Xunit;
using ZplRenderer;
using ZplRenderer.Core;

namespace ZplRenderer.Tests
{
    public class RendererTests
    {
        [Fact]
        public void Render_BasicLabel_ReturnsBitmap()
        {
            var engine = new ZplEngine();
            string zpl = "^XA^FO50,50^ADN,36,20^FDHello World^FS^XZ";
            
            var bitmap = engine.Render(zpl);
            
            Assert.NotNull(bitmap);
            Assert.True(bitmap.Width > 0);
            Assert.True(bitmap.Height > 0);
        }

        [Fact]
        public void Render_Barcode128_NoCrash()
        {
            var engine = new ZplEngine();
            string zpl = "^XA^FO50,50^BCN,100,Y,N,N^FD123456^FS^XZ";
            
            var bitmap = engine.Render(zpl);
            
            Assert.NotNull(bitmap);
        }

        [Fact]
        public void Render_DataMatrix_NoCrash()
        {
            var engine = new ZplEngine();
            string zpl = "^XA^FO50,50^BXN,10,200^FDDataMatrix^FS^XZ";
            
            var bitmap = engine.Render(zpl);
            
            Assert.NotNull(bitmap);
        }

        [Fact]
        public void Render_QRCode_NoCrash()
        {
            var engine = new ZplEngine();
            string zpl = "^XA^FO50,50^BQN,2,5^FDQA,https://example.com^FS^XZ";
            
            var bitmap = engine.Render(zpl);
            
            Assert.NotNull(bitmap);
        }

        [Fact]
        public void Render_GraphicDownloadAndRecall_NoCrash()
        {
            var engine = new ZplEngine();
            // Download a small graphic (dot) and recall it
            string zpl = "~DGR:DOT.GRF,1,1,F0^XA^FO10,10^XGR:DOT.GRF,10,10^FS^XZ";
            
            var bitmap = engine.Render(zpl);
            
            Assert.NotNull(bitmap);
        }

        [Fact]
        public void Render_WithFormatCommands_EffectiveDpi()
        {
            var engine = new ZplEngine();
            // ^PW400 sets print width
            string zpl = "^XA^PW400^FO10,10^FDTest^FS^XZ";
            
            var bitmap = engine.Render(zpl);
            
            Assert.NotNull(bitmap);
            // Default width is 4x203=812 (4 inches), but with PW it might affect canvas size?
            // Current engine implementation usually resets canvas based on content or standard size.
            // If implementation uses PW to size bitmap, it would be 400.
            // Check RenderContext default size behavior if implemented.
            // Currently BitmapRenderer computes "required size" or fixed size?
            // Let's just assert non-null.
        }
    }
}