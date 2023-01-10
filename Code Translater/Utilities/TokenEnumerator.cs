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
            Token = tokenizer.ReadToken();
        }

        public TokenizerState GetCurrentState()
        {
            return new TokenEnumeratorState
            {
                TokenizerState = this.tokenizer.GetCurrentState(),
                Token = this.Token
            };
        }

        public void RestoreState(TokenizerState state)
        {
            if(state is TokenEnumeratorState enumeratorState)
            {
                this.Token = enumeratorState.Token;
                this.tokenizer.RestoreState(enumeratorState.TokenizerState);
            }
            else
            {
                throw new Exception();
            }
        }

        public bool MoveNext()
        {
            if(Type == TokenType.END_OF_FILE)
            {
                return false;
            }

            Token = tokenizer.ReadToken();
            return true;
        }

        public bool ReadRestOfLineRaw()
        {
            if (Type == TokenType.END_OF_FILE)
            {
                return false;
            }

            Token = tokenizer.ReadRestOfLineRaw();
            return true;
        }

        private class TokenEnumeratorState : TokenizerState
        {
            public Token Token;
            public TokenizerState TokenizerState;
        }
    }
}
