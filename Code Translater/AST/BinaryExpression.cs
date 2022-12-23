using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class BinaryExpression : Node
    {
        public Node Left;
        public Node Right;
        public string Operator;
    }
}
