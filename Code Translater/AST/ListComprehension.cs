using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    internal class ListComprehension : Node
    {
        public Node Collection;
        public string VariableName;
        public Node Expression;
    }
}
