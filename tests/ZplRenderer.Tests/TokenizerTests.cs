using System.Linq;
using Xunit;
using ZplRenderer.Core;

namespace ZplRenderer.Tests
{
    public class TokenizerTests
    {
        [Fact]
        public void Tokenize_BasicCommands_SplitsCorrectly()
        {
            string zpl = "^XA^FO50,50^FDHello^FS^XZ";
            var tokenizer = new ZplTokenizer();
            var tokens = tokenizer.Tokenize(zpl).ToList();
            
            Assert.Equal(5, tokens.Count);
            Assert.Equal("XA", tokens[0].CommandCode);
            Assert.Equal("FO", tokens[1].CommandCode);
            Assert.Equal("FD", tokens[2].CommandCode);
            Assert.Equal("Hello", tokens[2].Parameters); // Tokenizer extracts generic param
            Assert.Equal("FS", tokens[3].CommandCode);
            Assert.Equal("XZ", tokens[4].CommandCode);
        }

        [Fact]
        public void Tokenize_ComplexGraphicData_HandlesByteData()
        {
            // ~DG with some data
            string zpl = "~DGR:TEST.GRF,1,1,FF^XA^XZ";
            var tokenizer = new ZplTokenizer();
            var tokens = tokenizer.Tokenize(zpl).ToList();
            
            Assert.Contains(tokens, t => t.CommandCode == "DG");
            var dgToken = tokens.First(t => t.CommandCode == "DG");
            Assert.Contains("FF", dgToken.Parameters);
        }

        [Fact]
        public void Tokenize_TildeCommands_Recognized()
        {
            string zpl = "~DG^XA~SD15^XZ";
            var tokenizer = new ZplTokenizer();
            var tokens = tokenizer.Tokenize(zpl).ToList();
            
            Assert.Contains(tokens, t => t.CommandCode == "SD");
        }
    }
}
