using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class ListComprehension : Node
    {
        public Node Collection;
        public string VariableName;
        public Node Expression;
    }
}
