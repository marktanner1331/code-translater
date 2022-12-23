using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Assignment : Node
    {
        public string Type;
        public string Name;
        public Node RValue;
    }
}
