using Microsoft.VisualStudio.TestTools.UnitTesting;
using Code_Translater;
using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.Tests
{
    [TestClass()]
    public class TokenizerTests
    {
        [TestMethod()]
        public void ReadAlphaNumericTest()
        {
            string code = "def test():";
            Tokenizer tokenizer = new Tokenizer(code);
            string token = tokenizer.ReadAlphaNumeric();
            Assert.AreEqual("def", tokenizer);
        }
    }
}