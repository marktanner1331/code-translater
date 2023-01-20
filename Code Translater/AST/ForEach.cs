using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class ForEach : Node, INodeContainer
    {
        public string VariableName;
        public Node Collection;

        public List<Node> Children { get; set; }

        public ForEach()
        {
            Children = new List<Node>();
        }
    }
}
