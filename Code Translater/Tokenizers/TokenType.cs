using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.Tokenizers
{
    public enum TokenType
    {
        NUMBER,
        ALPHA_NUMERIC,
        OPEN_BRACKET,
        CLOSE_BRACKET,
        PUNCTUATION,
        NEW_LINE,
        INDENT,
        END_OF_FILE,
        STRING_LITERAL
    }
}
