using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Else : Node, INodeContainer
    {
        public List<Node> Children { get; set; }

        public Else()
        {
            Children = new List<Node>();
        }
    }
}
