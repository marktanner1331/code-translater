using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class If : Node, INodeContainer
    {
        public Node Expression;

        public List<Node> Children { get; set; }

        public If()
        {
            Children = new List<Node>();
        }
    }
}
