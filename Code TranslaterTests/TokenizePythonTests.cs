using Code_Translater.Tokenizers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Code_TranslaterTests
{
    [TestClass()]
    public class TokenizePythonTests
    {
        [TestMethod()]
        public void ReadTokensTest1()
        {
            string code = "def normalize(x, newLowerBound, newUpperBound):\n    min = np.min(x)\n    max = np.max(x)\n    range = max - min\n    newRange = newUpperBound - newLowerBound\n\n    return [((a - min) / range) * newRange + newLowerBound for a in x]";
            List<Token> expectedTokens = new List<Token>
            {
                new Token("def", TokenType.ALPHA_NUMERIC),
                new Token("normalize", TokenType.ALPHA_NUMERIC),
                new Token("(", TokenType.OPEN_BRACKET),
                new Token("x", TokenType.ALPHA_NUMERIC),
                new Token(",", TokenType.PUNCTUATION),
                new Token("newLowerBound", TokenType.ALPHA_NUMERIC),
                new Token(",", TokenType.PUNCTUATION),
                new Token("newUpperBound", TokenType.ALPHA_NUMERIC),
                new Token(")", TokenType.CLOSE_BRACKET),
                new Token(":", TokenType.PUNCTUATION),
                new Token("", TokenType.NEW_LINE),
                new Token("1", TokenType.INDENT),
                new Token("min", TokenType.ALPHA_NUMERIC),
                new Token("=", TokenType.PUNCTUATION),
                new Token("np", TokenType.ALPHA_NUMERIC),
                new Token(".", TokenType.PUNCTUATION),
                new Token("min", TokenType.ALPHA_NUMERIC),
                new Token("(", TokenType.OPEN_BRACKET),
                new Token("x", TokenType.ALPHA_NUMERIC),
                new Token(")", TokenType.CLOSE_BRACKET),
                new Token("", TokenType.NEW_LINE),
                new Token("1", TokenType.INDENT),
                new Token("max", TokenType.ALPHA_NUMERIC),
                new Token("=", TokenType.PUNCTUATION),
                new Token("np", TokenType.ALPHA_NUMERIC),
                new Token(".", TokenType.PUNCTUATION),
                new Token("max", TokenType.ALPHA_NUMERIC),
                new Token("(", TokenType.OPEN_BRACKET),
                new Token("x", TokenType.ALPHA_NUMERIC),
                new Token(")", TokenType.CLOSE_BRACKET),
                new Token("", TokenType.NEW_LINE),
                new Token("1", TokenType.INDENT),
                new Token("range", TokenType.ALPHA_NUMERIC),
                new Token("=", TokenType.PUNCTUATION),
                new Token("max", TokenType.ALPHA_NUMERIC),
                new Token("-", TokenType.PUNCTUATION),
                new Token("min", TokenType.ALPHA_NUMERIC),
                new Token("", TokenType.NEW_LINE),
                new Token("1", TokenType.INDENT),
                new Token("newRange", TokenType.ALPHA_NUMERIC),
                new Token("=", TokenType.PUNCTUATION),
                new Token("newUpperBound", TokenType.ALPHA_NUMERIC),
                new Token("-", TokenType.PUNCTUATION),
                new Token("newLowerBound", TokenType.ALPHA_NUMERIC),
                new Token("", TokenType.NEW_LINE),
                new Token("1", TokenType.INDENT),
                new Token("return", TokenType.ALPHA_NUMERIC),
                new Token("[", TokenType.OPEN_BRACKET),
                new Token("(", TokenType.OPEN_BRACKET),
                new Token("(", TokenType.OPEN_BRACKET),
                new Token("a", TokenType.ALPHA_NUMERIC),
                new Token("-", TokenType.PUNCTUATION),
                new Token("min", TokenType.ALPHA_NUMERIC),
                new Token(")", TokenType.CLOSE_BRACKET),
                new Token("/", TokenType.PUNCTUATION),
                new Token("range", TokenType.ALPHA_NUMERIC),
                new Token(")", TokenType.CLOSE_BRACKET),
                new Token("*", TokenType.PUNCTUATION),
                new Token("newRange", TokenType.ALPHA_NUMERIC),
                new Token("+", TokenType.PUNCTUATION),
                new Token("newLowerBound", TokenType.ALPHA_NUMERIC),
                new Token("for", TokenType.ALPHA_NUMERIC),
                new Token("a", TokenType.ALPHA_NUMERIC),
                new Token("in", TokenType.ALPHA_NUMERIC),
                new Token("x", TokenType.ALPHA_NUMERIC),
                new Token("]", TokenType.CLOSE_BRACKET)
            };

            Tokenizer tokenizer = new PythonTokenizer(code);

            //while (true)
            //{
            //    Token token = tokenizer.ReadToken();
            //    if (token.Type == TokenType.END_OF_FILE)
            //    {
            //        break;
            //    }

            //    Debug.WriteLine($"new Token(\"{token.Value}\", TokenType.{token.Type}),");
            //}

            foreach (var expectedToken in expectedTokens)
            {
                Token token = tokenizer.ReadToken();
                Assert.AreEqual(expectedToken.Type, token.Type);
                Assert.AreEqual(expectedToken.Value, token.Value);
            }
        }
    }
}
