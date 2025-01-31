using System;
using Code_Translater.AST;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class ValueReader
    {
         private readonly Parser _parser;

        public ValueReader(Parser parser)
        {
            _parser = parser;
        }
        
        public Node ReadValue()
        {
            Expression expression = new Expression();
            expression.Coefficients.Add(_parser.ReadProperty());
            while (true)
            {
                object tokenEnumerator;
                if (_parser.TokenEnumerator.Type == TokenType.PUNCTUATION)
                {
                    if ("-+*/%".Contains(_parser.TokenEnumerator.Value))
                    {
                        expression.Operators.Add(_parser.TokenEnumerator.Value);
                        _parser.TokenEnumerator.MoveNext();
                        expression.Coefficients.Add(_parser.ReadProperty());
                    }
                    else if (_parser.TokenEnumerator.Value.Length == 2)
                    {
                        if(_parser.TokenEnumerator.Value == "==")
                        {
                            expression.Operators.Add(_parser.TokenEnumerator.Value);
                            _parser.TokenEnumerator.MoveNext();
                            expression.Coefficients.Add(_parser.ReadProperty());
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else if (_parser.TokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    break;
                }
                else
                {
                    break;
                }
            }

            if(expression.Coefficients.Count == 1)
            {
                return expression.Coefficients[0];
            }
            else
            {
                return expression;
            }
        }
        
        public interface Parser
        {
            TokenEnumerator TokenEnumerator { get; }
            Node ReadProperty();
        }
    }
}