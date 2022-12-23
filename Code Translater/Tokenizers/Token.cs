using System;
using System.Collections.Generic;
using System.Text;
using static Code_Translater.Tokenizers.Tokenizer;

namespace Code_Translater.Tokenizers
{
    public class Token
    {
        public string Value;
        public TokenType Type;

        public Token()
        {

        }

        public Token(string value, TokenType type)
        {
            Value = value;
            Type = type;
        }
    }
}
