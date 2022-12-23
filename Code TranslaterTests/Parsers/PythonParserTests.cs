using Microsoft.VisualStudio.TestTools.UnitTesting;
using Code_Translater.Parsers;
using System;
using System.Collections.Generic;
using System.Text;
using Code_Translater.Tokenizers;
using Code_Translater.AST;

namespace Code_Translater.Parsers.Tests
{
    [TestClass()]
    public class PythonParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            string code = "import numpy as np\ndef normalize(x, newLowerBound, newUpperBound):\n    min = np.min(x)\n    max = np.max(x)\n    range = max - min\n    newRange = newUpperBound - newLowerBound\n\n    return [((a - min) / range) * newRange + newLowerBound for a in x]";

            Tokenizer tokenizer = new PythonTokenizer(code);
            IEnumerable<Token> tokens = tokenizer.ReadAllTokens();

            PythonParser parser = new PythonParser();
            Root root = parser.Parse(tokens);
        }
    }
}