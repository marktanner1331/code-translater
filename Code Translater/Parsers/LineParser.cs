using System;
using System.Collections.Generic;
using Code_Translater.AST;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class LineParser
    {
        private readonly Parser _parser;

        public LineParser(Parser parser)
        {
            _parser = parser;
        }
        
        public void ParseLine()
        {
            switch (_parser.TokenEnumerator.Type)
            {
                case TokenType.ALPHA_NUMERIC:
                    _parser.AddAlphaNumeric();
                    return;
                case TokenType.PUNCTUATION:
                    if (_parser.TokenEnumerator.Value == "//")
                    {
                        _parser.Stack.Peek().Children.Add(_parser.ReadComment());
                        return;
                    }
                    break;
                case TokenType.NEW_LINE:
                    throw new Exception();
            }

            throw new NotImplementedException();
        }
        
        public interface Parser
        {
            TokenEnumerator TokenEnumerator { get; }
            Stack<INodeContainer> Stack { get; }
            void AddAlphaNumeric();
            Node ReadComment();
        }
    }
}