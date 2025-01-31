using System;
using System.Collections.Generic;
using System.Linq;
using Code_Translater.AST;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class AlphaNumericAdder
    {
        private readonly Parser _parser;

        public AlphaNumericAdder(Parser parser)
        {
            _parser = parser;
        }
        
        /// <summary>
        /// the current token has a type of ALPHA_NUMERIC
        /// </summary>
        public void AddAlpaNumeric()
        {
            if (_parser.TryAddKeyword())
            {
                return;
            }

            if (_parser.TryAddAssignment())
            {
                return;
            }

            Node node = _parser.ReadValue();

            if (node is FunctionCall || (node is Property property && property.Values.Last() is FunctionCall))
            {
                _parser.Stack.Peek().Children.Add(node);
                return;
            }
            else
            {
                throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }
        
        public interface Parser
        {
            TokenEnumerator TokenEnumerator { get; }
            Stack<INodeContainer> Stack { get; }
            bool TryAddKeyword();
            bool TryAddAssignment();
            Node ReadValue();
        }
    }
}