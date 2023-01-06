using Code_Translater.AST;
using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.Utilities
{
    public class PackageMapper
    {
        private Dictionary<string, FunctionSignature> ParamMap = new Dictionary<string, FunctionSignature>();

        public PackageMapper()
        {
            AddFunction("Enumerable.min", "float", "IEnumerable<float>");
            AddFunction("Enumerable.max", "float", "IEnumerable<float>");
        }

        public bool TryGetReturnType(string package, string function, out string type)
        {
            string name = package + "." + function;

            if (ParamMap.ContainsKey(name) == false)
            {
                type = null;
                return false;
            }

            type = ParamMap[name].ReturnType;
            return true;
        }

        public bool TryGetTypeForParameter(string package, string function, int paramIndex, out string type)
        {
            string name = package + "." + function;

            if(ParamMap.ContainsKey(name) == false)
            {
                type = null;
                return false;
            }

            FunctionSignature signature = ParamMap[name];
            if(signature.ParameterTypes.Length <= paramIndex)
            {
                type = null;
                return false;
            }

            type = signature.ParameterTypes[paramIndex];
            return true;
        }

        private void AddFunction(string name, string returnType, params string[] parameterTypes)
        {
            ParamMap[name] = new FunctionSignature
            {
                ReturnType = returnType,
                ParameterTypes = parameterTypes
            };
        }

        private class FunctionSignature
        {
            public string[] ParameterTypes;
            public string ReturnType;
        }
    }
}
