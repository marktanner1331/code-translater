using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Code_Translater.Tokenizers;

namespace Code_Translater.Tests
{
    [TestClass()]
    public class TokenizerTests
    {
        [TestMethod()]
        public void ReadNumberTest()
        {
            string code = "23";
            Tokenizer tokenizer = new Tokenizer(code);
            Token token = tokenizer.ReadToken();

            Assert.AreEqual(TokenType.NUMBER, token.Type);
            Assert.AreEqual("23", token.Value);
        }

        [TestMethod()]
        public void ReadAlphaNumericTest()
        {
            string code = "def test():";
            Tokenizer tokenizer = new Tokenizer(code);
            Token token = tokenizer.ReadToken();

            Assert.AreEqual(TokenType.ALPHA_NUMERIC, token.Type);
            Assert.AreEqual("def", token.Value);
        }

        [TestMethod()]
        public void ReadTokensTest1()
        {
            string code = "def test():";
            Tokenizer tokenizer = new Tokenizer(code);
            Token token = tokenizer.ReadToken();

            Assert.AreEqual(TokenType.ALPHA_NUMERIC, token.Type);
            Assert.AreEqual("def", token.Value);

            token = tokenizer.ReadToken();

            Assert.AreEqual(TokenType.ALPHA_NUMERIC, token.Type);
            Assert.AreEqual("test", token.Value);

            token = tokenizer.ReadToken();

            Assert.AreEqual(TokenType.OPEN_BRACKET, token.Type);
            Assert.AreEqual("(", token.Value);

            token = tokenizer.ReadToken();

            Assert.AreEqual(TokenType.CLOSE_BRACKET, token.Type);
            Assert.AreEqual(")", token.Value);

            token = tokenizer.ReadToken();

            Assert.AreEqual(TokenType.PUNCTUATION, token.Type);
            Assert.AreEqual(":", token.Value);
        }
    }
}