using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Class : Node, INodeContainer
    {
        public string Name;

        public List<Node> Children { get; set; }

        public Class()
        {
            Children = new List<Node>();
        }
    }
}
