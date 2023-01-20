using Code_Translater.AST;
using Code_Translater.Parsers;
using Code_Translater.Serializers;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Code_TranslaterTests
{
    public class Tester
    {
        private static string ScriptsFolder = @"C:\Users\Home\Documents\code-translater\Code TranslaterTests\Scripts\";

        public static Root Parse(Language input, int scriptName)
        {
            string sourceCodePath = ScriptsFolder + GetFolderName(input) + "/" + scriptName + GetExtension(input);
            string sourceCode = File.ReadAllText(sourceCodePath);

            IParser parser = GetParser(input);
            return parser.Parse(sourceCode);
        }

        public static void SerializeTest(Language input, Language output, int scriptName)
        {
            string codePath = ScriptsFolder + GetFolderName(input) + "/";

            string sourceCodePath = codePath + scriptName + "/" + scriptName + "." + GetExtension(input);
            string sourceCode = File.ReadAllText(sourceCodePath);

            string destCodePath = codePath + scriptName + "/" + scriptName + "." + GetExtension(output);
            string destCode = File.ReadAllText(destCodePath);

            IParser parser = GetParser(input);

            Root root = parser.Parse(sourceCode);

            ISerializer serializer = GetSerializer(output);
            string actualDestCode = serializer.Serialize(root);

            if (Debugger.IsAttached)
            {
                Debug.WriteLine(actualDestCode);
            }

            Assert.AreEqual(destCode, actualDestCode);
        }

        private static ISerializer GetSerializer(Language language)
        {
            switch (language)
            {
                case Language.CSHARP:
                    return new CSharpSerializer();
                default:
                    throw new Exception();
            }
        }

        private static IParser GetParser(Language language)
        {
            switch (language)
            {
                case Language.PYTHON:
                    return new PythonParser();
                default:
                    throw new Exception();
            }
        }

        private static string GetFolderName(Language language)
        {
            switch (language)
            {
                case Language.CSHARP:
                    return "CSharp";
                case Language.PYTHON:
                    return "Python";
                default:
                    throw new Exception();
            }
        }

        private static string GetExtension(Language language)
        {
            switch (language)
            {
                case Language.CSHARP:
                    return "cs";
                case Language.PYTHON:
                    return "py";
                default:
                    throw new Exception();
            }
        }

        public enum Language
        {
            CSHARP,
            PYTHON
        }
    }
}
