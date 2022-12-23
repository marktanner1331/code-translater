using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Root : Node, INodeContainer
    {
        public List<Node> Children { get; }
        public Root()
        {
            Children = new List<Node>();
        }
    }
}
