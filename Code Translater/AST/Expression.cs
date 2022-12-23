using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Expression : Node
    {
        public List<Node> Coefficients = new List<Node>();
        public List<string> Operators = new List<string>();
    }
}
