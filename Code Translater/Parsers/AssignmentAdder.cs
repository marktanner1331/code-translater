using System.Collections.Generic;
using Code_Translater.AST;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class AssignmentAdder
    {
        private readonly Parser _parser;

        public AssignmentAdder(Parser parser)
        {
            _parser = parser;
        }
        
        public bool TryAddAssignment()
        {
            TokenizerState currentStete = _parser.TokenEnumerator.GetCurrentState();

            Node lValue = _parser.ReadValue();
            if (_parser.IsLValue(lValue) == false)
            {
                _parser.TokenEnumerator.RestoreState(currentStete);
                return false;
            }

            if (_parser.TokenEnumerator.Type != TokenType.PUNCTUATION || _parser.TokenEnumerator.Value != "=")
            {
                _parser.TokenEnumerator.RestoreState(currentStete);
                return false;
            }

            _parser.TokenEnumerator.MoveNext();

            Node rValue = _parser.ReadValue();

            _parser.Stack.Peek().Children.Add(new Assignment
            {
                LValue = lValue,
                RValue = rValue
            });

            return true;
        }
        
        public interface Parser
        {
            TokenEnumerator TokenEnumerator { get; }
            Node ReadValue();
            bool IsLValue(Node node);
            Stack<INodeContainer> Stack { get; }
        }
    }
}