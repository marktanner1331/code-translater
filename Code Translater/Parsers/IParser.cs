using Code_Translater.AST;
using Code_Translater.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.Parsers
{
    public interface IParser
    {
        public Root Parse();
    }
}
