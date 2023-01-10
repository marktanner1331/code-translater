using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Function : Node, INodeContainer
    {
        public string Name;
        public List<FunctionParameter> Parameters = new List<FunctionParameter>();
        public List<Node> Children { get; set; }
        public string ReturnType;

        public Function()
        {
            Children = new List<Node>();
        }
    }
}
