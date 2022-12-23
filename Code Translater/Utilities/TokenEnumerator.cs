using Code_Translater.Tokenizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code_Translater.Utilities
{
    public class TokenEnumerator
    {
        private Token[] Tokens;
        private int Index = 0;

        public string Value
        {
            get => Tokens[Index].Value;
        }
        public TokenType Type
        {
            get => Tokens[Index].Type;
        }

        public Token Token
        {
            get => Tokens[Index];
        }

        public TokenEnumerator(IEnumerable<Token> tokens)
        {
            this.Tokens = tokens.ToArray();
        }

        public bool MoveNext()
        {
            Index++;
            return Index < Tokens.Length;
        }

        public void MovePrevious()
        {
            Index--;
        }
    }
}
