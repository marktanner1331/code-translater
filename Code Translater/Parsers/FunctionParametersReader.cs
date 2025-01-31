using System;
using System.Collections.Generic;
using Code_Translater.AST;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class FunctionParametersReader
    {
         private readonly Parser _parser;

        public FunctionParametersReader(Parser parser)
        {
            _parser = parser;
        }
        
        public List<FunctionParameter> ReadFunctionParameters()
        {
            if (_parser.TokenEnumerator.Value != "(")
            {
                throw new Exception();
            }

            List<FunctionParameter> parameters = new List<FunctionParameter>();

            _parser.TokenEnumerator.MoveNext();

            if (_parser.TokenEnumerator.Value == ")")
            {
                _parser.TokenEnumerator.MoveNext();
                return parameters;
            }

            while (true)
            {
                Node value = _parser.ReadValue();

                if (_parser.TokenEnumerator.Value == "=" && value is Variable variable)
                {
                    _parser.TokenEnumerator.MoveNext();

                    parameters.Add(new FunctionParameter
                    {
                        Name = variable.Name,
                        Value = _parser.ReadValue()
                    });
                }
                else
                {
                    parameters.Add(new FunctionParameter
                    {
                        Value = value
                    });
                }

                if (_parser.TokenEnumerator.Value == ",")
                {
                    _parser.TokenEnumerator.MoveNext();
                    continue;
                }
                else if (_parser.TokenEnumerator.Value == ")")
                {
                    _parser.TokenEnumerator.MoveNext();
                    return parameters;
                }

                throw new Exception();
            }
        }
        public interface Parser
        {
            TokenEnumerator TokenEnumerator { get; }
            Node ReadValue();
        }
    }
}