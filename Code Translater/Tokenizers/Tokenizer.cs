using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Code_Translater.Tokenizers
{
    public unsafe class Tokenizer
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, long count);

        public readonly string Code;
        private char* Buffer;
        protected char* Pointer;
        protected readonly char* End;

        private Token MostRecentToken = null;
        private int IndentSize = -1;

        public Tokenizer(string code)
        {
            Code = code;
            fixed (char* temp = code)
            {
                Buffer = (char*)Marshal.AllocHGlobal(code.Length * 2);

                //assuming chars are utf16
                //and assuming all code is ASCII
                memcpy((IntPtr)Buffer, (IntPtr)temp, code.Length * 2);

                Pointer = Buffer;
                End = Buffer + code.Length;
            }
        }

        public TokenizerState GetCurrentState()
        {
            return new TokenizerState
            {
                PointerOffset = Pointer - Buffer,
                IndentSize = IndentSize,
                MostRecentToken = MostRecentToken
            };
        }

        public void RestoreState(TokenizerState state)
        {
            this.Pointer = this.Buffer + state.PointerOffset;
            this.IndentSize = state.IndentSize;
            this.MostRecentToken = state.MostRecentToken;
        }

        public IEnumerable<Token> ReadAllTokens()
        {
            while(true)
            {
                Token token = ReadToken();
                yield return token;

                if(token.Type == TokenType.END_OF_FILE)
                {
                    break;
                }
            }
        }

        public Token ReadRestOfLineRaw()
        {
            char* start = Pointer;
            Pointer++;

            while (Pointer != End && *Pointer != '\r' && *Pointer != '\n')
            {
                Pointer++;
            }

            return new Token
            {
                Value = new string(start, 0, (int)(Pointer - start)),
                Type = TokenType.ALPHA_NUMERIC
            };
        }

        /// <summary>
        /// reads a token matching [0-9a-zA-Z_]+
        /// </summary>
        public Token ReadToken()
        {
            long temp = Pointer - Buffer;

            if(Pointer == End)
            {
                return new Token("", TokenType.END_OF_FILE);
            }

            if (MostRecentToken != null && MostRecentToken.Type == TokenType.NEW_LINE)
            {
                MostRecentToken = ReadIndent();
                if (MostRecentToken.Value != "0")
                {
                    return MostRecentToken;
                }
            }
            else
            {
                while (Pointer != End)
                {
                    if (*Pointer == ' ')
                    {
                        Pointer++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            
            MostRecentToken = ReadTokenInner();
            return MostRecentToken;
        }

        protected virtual Token ReadTokenInner()
        {
            char c = *Pointer;

            if (char.IsDigit(c))
            {
                return ReadNumber();
            }

            if (char.IsLetter(c) || c == '_')
            {
                return ReadAlphaNumeric();
            }

            if ("{[(".Contains(c))
            {
                Pointer++;
                return new Token
                {
                    Value = c.ToString(),
                    Type = TokenType.OPEN_BRACKET
                };
            }

            if ("}])".Contains(c))
            {
                Pointer++;
                return new Token
                {
                    Value = c.ToString(),
                    Type = TokenType.CLOSE_BRACKET
                };
            }

            if (":,=.-+/*#%><".Contains(c))
            {
                return ReadPunctuation();
            }

            if (c == '\r' || c == '\n')
            {
                return ReadNewLine();
            }

            if (c == '\0')
            {
                Pointer++;
                return new Token("", TokenType.END_OF_FILE);
            }

            if (c == '"' || c == '\'')
            {
                return ReadStringLiteral();
            }

            throw new NotImplementedException();
        }

        protected virtual Token ReadPunctuation()
        {
            char* start = Pointer;
            Pointer++;

            while (Pointer != End)
            {
                char c = *Pointer;
                if (!":,=.-+/*#%><".Contains(c))
                {
                    break;
                }

                Pointer++;
            }

            return new Token
            {
                Value = new string(start, 0, (int)(Pointer - start)),
                Type = TokenType.PUNCTUATION
            };
        }

        protected virtual Token ReadIndent()
        {
            char* start = Pointer;
            if(*Pointer == '\t')
            {
                while(*Pointer == '\t')
                {
                    Pointer++;
                }

                return new Token((Pointer - start).ToString(), TokenType.INDENT);
            }

            if(*Pointer == ' ')
            {
                while (*Pointer == ' ')
                {
                    Pointer++;
                }

                if(IndentSize == -1)
                {
                    if(Pointer - start > 0)
                    {
                        IndentSize = (int)(Pointer - start);
                        return new Token("1", TokenType.INDENT);
                    }
                    else
                    {
                        return new Token("0", TokenType.INDENT);
                    }
                }

                return new Token(((Pointer - start) / IndentSize).ToString(), TokenType.INDENT);
            }

            return new Token("0", TokenType.INDENT);
        }

        protected virtual Token ReadNewLine()
        {
            Pointer++;
            if (*Pointer == '\r' || *Pointer == '\n')
            {
                Pointer++;
            }

            return new Token("", TokenType.NEW_LINE);
        }

        protected virtual Token ReadAlphaNumeric()
        {
            char* start = Pointer;
            Pointer++;
            
            while (Pointer != End)
            {
                char c = *Pointer;
                if(c > 128)
                {

                }    

                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    Pointer++;
                }
                else
                {
                    break;
                }
            }

            return new Token
            {
                Value = new string(start, 0, (int)(Pointer - start)),
                Type = TokenType.ALPHA_NUMERIC
            };
        }

        protected virtual Token ReadStringLiteral()
        {
            char* start = Pointer;
            char quoteLetter = *Pointer;

            Pointer++;

            while (Pointer != End)
            {
                char c = *Pointer;
                Pointer++;

                if (c == quoteLetter)
                {
                    break;
                }
            }

            return new Token
            {
                Value = new string(start, 0, (int)(Pointer - start)),
                Type = TokenType.STRING_LITERAL
            };
        }

        protected virtual Token ReadNumber()
        {
            char* start = Pointer;
            Pointer++;

            while (Pointer != End)
            {
                char c = *Pointer;
                if (!char.IsDigit(c))
                {
                    break;
                }

                Pointer++;
            }

            return new Token
            {
                Value = new string(start, 0, (int)(Pointer - start)),
                Type = TokenType.NUMBER
            };
        }
    }
}
