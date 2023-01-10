using Microsoft.VisualStudio.TestTools.UnitTesting;
using Code_Translater.Serializers;
using System;
using System.Collections.Generic;
using System.Text;
using Code_Translater.AST;
using Code_Translater.Parsers;
using Code_Translater.Tokenizers;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using Code_Translater.Utilities;

namespace Code_TranslaterTests.Serializers
{
    [TestClass()]
    public class CSharpSerializerTests
    {
        [TestMethod()]
        public void SerializeTest()
        {
            Tester.SerializeTest(Tester.Language.PYTHON, Tester.Language.CSHARP, 1);
        }

        [TestMethod()]
        public void SerializeTest2()
        {
            Tester.SerializeTest(Tester.Language.PYTHON, Tester.Language.CSHARP, 2);
        }

        [TestMethod()]
        public void SerializeTest3()
        {
            Tester.SerializeTest(Tester.Language.PYTHON, Tester.Language.CSHARP, 3);
        }

        [TestMethod()]
        public void SerializeTest4()
        {
            Tester.SerializeTest(Tester.Language.PYTHON, Tester.Language.CSHARP, 4);
        }

        [TestMethod()]
        public void SerializeTest5()
        {
            Tester.SerializeTest(Tester.Language.PYTHON, Tester.Language.CSHARP, 5);
        }
    }
}