using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code_Translater
{
    public unsafe class Tokenizer
    {
        public readonly string Code;
        private char[] Buffer;
        private int Pointer;
        private int End;

        public Tokenizer(string code)
        {
            Code = code;
            Buffer = code.ToCharArray();
            Pointer = 0;
            End = Buffer.Length;
        }

        /// <summary>
        /// reads a token matching [0-9a-zA-Z_]+
        /// </summary>
        /// <returns></returns>
        public string ReadAlphaNumeric()
        {
            StringBuilder sb = new StringBuilder();
            for(;Pointer != End;Pointer++)
            {
                char c = Buffer[Pointer];
                if(char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    break;
                }
            }

            return sb.ToString();
        }
    }
}
