using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Assignment : Node, IHasType
    {
        public string Type { get; set; }
        public Node LValue;
        public Node RValue;
    }
}
