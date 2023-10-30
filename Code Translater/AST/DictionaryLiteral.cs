using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class DictionaryLiteral : Node
    {
        public Dictionary<Node, Node> Values = new Dictionary<Node, Node>();
    }
}
