using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class FunctionParameter : IHasType
    {
        public string Name;
        public string Type { get; set; }

        public Node Value;
    }
}
