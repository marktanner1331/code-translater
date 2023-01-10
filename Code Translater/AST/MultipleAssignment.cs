using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class MultipleAssignment : Node, IHasType
    {
        public string Type { get; set; }
        public List<string> VariableNames;
        public Node RValue;
    }
}
