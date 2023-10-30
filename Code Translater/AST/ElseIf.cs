using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class ElseIf : Node, INodeContainer
    {
        public Node Expression;

        public List<Node> Children { get; set; }

        public ElseIf()
        {
            Children = new List<Node>();
        }
    }
}
