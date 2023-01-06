using Code_Translater.Tokenizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code_Translater.Utilities
{
    public unsafe class TokenEnumerator
    {
        private Tokenizer tokenizer;

        private char* PreviousPointer;
        private Token PreviousToken;

        public string Value
        {
            get => Token.Value;
        }
        public TokenType Type
        {
            get => Token.Type;
        }

        public Token Token { get; private set; }

        public TokenEnumerator(Tokenizer tokenizer)
        {
            this.tokenizer = tokenizer;

            this.PreviousPointer = tokenizer.Pointer;
            Token = tokenizer.ReadToken();
        }

        public bool MoveNext()
        {
            if(Type == TokenType.END_OF_FILE)
            {
                return false;
            }

            this.PreviousPointer = tokenizer.Pointer;
            this.PreviousToken = Token;

            Token = tokenizer.ReadToken();
            return true;
        }

        public void MovePrevious()
        {
            if(this.tokenizer.Pointer == this.PreviousPointer)
            {
                throw new Exception("Cannot MovePrevious() multiple times");
            }

            this.Token = PreviousToken;
            this.tokenizer.Pointer = PreviousPointer;
        }

        public bool ReadRestOfLineRaw()
        {
            if (Type == TokenType.END_OF_FILE)
            {
                return false;
            }

            this.PreviousPointer = tokenizer.Pointer;
            this.PreviousToken = Token;

            Token = tokenizer.ReadRestOfLineRaw();
            return true;
        }
    }
}
