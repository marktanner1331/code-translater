using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.Tokenizers
{
    public unsafe class PythonTokenizer : Tokenizer
    {
        public PythonTokenizer(string code) : base(code)
        {
        }

        protected override Token ReadTokenInner()
        {
            if(End - Pointer > 2)
            {
                if((*Pointer == 'f' || *Pointer == 'F') && Pointer[1] == '"')
                {
                    char c = Pointer[1];
                    return ReadInterpolatedString();
                }
            }

            return base.ReadTokenInner();
        }

        private Token ReadInterpolatedString()
        {
            char c = *Pointer;

            Pointer++;
            Token token = base.ReadStringLiteral();

            token.Value = c + token.Value;
            return token;
        }

        protected override Token ReadStringLiteral()
        {
            char* start = Pointer;
            char quoteLetter = *Pointer;

            if(Pointer[1] == quoteLetter && Pointer[2] == quoteLetter)
            {
                //triple quoted multiline string literals
                Pointer += 3;

                while (Pointer != End)
                {
                    if (Pointer[0] == quoteLetter && Pointer[1] == quoteLetter && Pointer[2] == quoteLetter)
                    {
                        Pointer += 3;
                        break;
                    }
                    else
                    {
                        Pointer++;
                    }
                }
            }
            else
            {
                return base.ReadStringLiteral();
            }
            
            return new Token
            {
                Value = new string(start, 0, (int)(Pointer - start)),
                Type = TokenType.STRING_LITERAL
            };
        }
    }
}
