using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class ListLiteral : Node
    {
        public List<Node> Values = new List<Node>();
    }
}
