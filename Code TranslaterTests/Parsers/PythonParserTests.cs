using Microsoft.VisualStudio.TestTools.UnitTesting;
using Code_Translater.Parsers;
using System;
using System.Collections.Generic;
using System.Text;
using Code_Translater.Tokenizers;
using Code_Translater.AST;
using System.Reflection;
using System.IO;
using Code_Translater.Utilities;
using System.Linq;
using Code_TranslaterTests;

namespace Code_Translater.Parsers.Tests
{
    [TestClass()]
    public class PythonParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            //string code = "import numpy as np\ndef normalize(x, newLowerBound, newUpperBound):\n    min = np.min(x)\n    max = np.max(x)\n    range = max - min\n    newRange = newUpperBound - newLowerBound\n\n    return [((a - min) / range) * newRange + newLowerBound for a in x]";

            //Tokenizer tokenizer = new PythonTokenizer(code);
            //IEnumerable<Token> tokens = tokenizer.ReadAllTokens();

            //PythonParser parser = new PythonParser();
            //Root root = parser.Parse(tokens);
        }

        [TestMethod()]
        public void ParseTest2()
        {
            //string pythonFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Python Scripts/";
            //string code = File.ReadAllText(pythonFolder + "python 2.py");

            //Tokenizer tokenizer = new PythonTokenizer(code);
            //IEnumerable<Token> tokens = tokenizer.ReadAllTokens();

            //PythonParser parser = new PythonParser();
            //Root root = parser.Parse(tokens);
        }

        [TestMethod()]
        public void ParseTest3()
        {
            Root root = Tester.Parse(Tester.Language.PYTHON, 3);

            Assert.AreEqual(1, root.Children.Count);
            if(root.Children.First() is Comment comment)
            {
                Assert.AreEqual(" this is a comment", comment.Value);
            }
            else
            {
                Assert.Fail();
            }
           
        }
    }
}