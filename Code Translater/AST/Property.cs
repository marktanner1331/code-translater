using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class Property : Node
    {
        public List<Node> Values = new List<Node>();

        /// <summary>
        /// returns true if the property is of the form variable.function()
        /// </summary>
        public bool IsVariablePlusFunctionCall(out string variableName, out FunctionCall functionCall)
        {
            if(Values.Count != 2)
            {
                variableName = null;
                functionCall = null;
                return false;
            }

            if(Values[0] is Variable variable == false || Values[1] is FunctionCall functionCall1 == false)
            {
                variableName = null;
                functionCall = null;
                return false;
            }

            variableName = variable.Name;
            functionCall = functionCall1;
            return true;
        }
    }
}
