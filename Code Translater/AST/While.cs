using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class While : Node, INodeContainer
    {
        public Node Expression;

        public List<Node> Children { get; set; }

        public While()
        {
            Children = new List<Node>();
        }
    }
}
