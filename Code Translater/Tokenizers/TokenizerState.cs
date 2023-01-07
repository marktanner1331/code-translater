using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.Tokenizers
{
    public unsafe class TokenizerState
    {
        public long PointerOffset;
        public Token MostRecentToken;
        public int IndentSize;
    }
}
