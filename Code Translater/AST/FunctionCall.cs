using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class FunctionCall : Node
    {
        public string FunctionName;
        public List<FunctionParameter> Parameters;
    }
}
