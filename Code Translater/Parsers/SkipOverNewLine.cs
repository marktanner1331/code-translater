using System;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class NewLineSkipper
    {
        private readonly Parser _parser;

        public NewLineSkipper(Parser parser)
        {
            _parser = parser;
        }

        public void Skip()
        {
            while (_parser.TokenEnumerator.Type == TokenType.NEW_LINE)
            {
                _parser.LineNumber++;
                _parser.TokenEnumerator.MoveNext();

                if (_parser.TokenEnumerator.Type == TokenType.INDENT)
                {
                    _parser.TokenEnumerator.MoveNext();
                }
            }
        }

        public interface Parser
        {
            TokenEnumerator TokenEnumerator { get; }
            int LineNumber { get; set; }
        }
    }
}