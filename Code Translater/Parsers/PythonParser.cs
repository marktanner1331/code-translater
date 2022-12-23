using Code_Translater.AST;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Code_Translater.Parsers
{
    public class PythonParser
    {
        private Root root;
        private TokenEnumerator tokenEnumerator;

        private Stack<INodeContainer> stack;
        private int currentIndent;

        //maps aliases to their package names
        //i.e. np -> numpy
        private Dictionary<string, string> packageAliases;

        public Root Parse(IEnumerable<Token> tokens)
        {
            root = new Root();

            stack = new Stack<INodeContainer>();
            stack.Push(root);
            currentIndent = 0;

            tokenEnumerator = new TokenEnumerator(tokens);

            packageAliases = new Dictionary<string, string>();

            while (tokenEnumerator.Type != TokenType.END_OF_FILE)
            {
                if (tokenEnumerator.Type == TokenType.INDENT)
                {
                    ProcessIndent();
                }

                ParseLine();

                if (tokenEnumerator.Type == TokenType.NEW_LINE)
                {
                    tokenEnumerator.MoveNext();
                }
            }

            return root;
        }

        private void ParseLine()
        {
            switch (tokenEnumerator.Type)
            {
                case TokenType.ALPHA_NUMERIC:
                    ParseAlpaNumeric();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// the current token has a type of INDENT
        /// </summary>
        private void ProcessIndent()
        {
            int newIndent = int.Parse(tokenEnumerator.Value);
            tokenEnumerator.MoveNext();

            int difference = newIndent - currentIndent;
            currentIndent = newIndent;

            if (difference == 0)
            {
                return;
            }

            if (difference == 1)
            {
                Node mostRecent = stack.Peek().Children.Last();
                if (mostRecent is INodeContainer == false)
                {
                    throw new Exception();
                }

                stack.Push((INodeContainer)mostRecent);
                return;
            }

            if (difference > 1)
            {
                throw new Exception();
            }

            while (difference < 0)
            {
                stack.Pop();
                difference++;
            }
        }

        /// <summary>
        /// the current token has a type of ALPHA_NUMERIC
        /// </summary>
        private void ParseAlpaNumeric()
        {
            if (TryParseKeyword())
            {
                return;
            }

            if (TryParseAssignment())
            {
                return;
            }

            throw new NotImplementedException();
        }

        private bool TryParseKeyword()
        {
            switch (tokenEnumerator.Value)
            {
                case "def":
                    ParseFunction();
                    return true;
                case "import":
                    ParseImport();
                    return true;
                case "return":
                    ParseReturn();
                    return true;
                default:
                    return false;
            }
        }

        private void ParseReturn()
        {
            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type == TokenType.NEW_LINE)
            {
                throw new NotImplementedException();
            }

            Node rValue = ParseRValue();

            if(stack.Peek() is Function == false)
            {
                throw new Exception();
            }

            Function function = (Function)stack.Peek();
            function.ReturnType = GetTypeFromNode(rValue);

            stack.Peek().Children.Add(new Return
            {
                Value = rValue
            });
        }

        private void ParseImport()
        {
            if (currentIndent != 0)
            {
                //cant handle function level imports yet
                throw new NotImplementedException();
            }

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string packageName = tokenEnumerator.Value;
            string alias;

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type == TokenType.NEW_LINE)
            {
                alias = "";
            }
            else if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC && tokenEnumerator.Value == "as")
            {
                tokenEnumerator.MoveNext();

                if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
                {
                    throw new Exception();
                }

                alias = tokenEnumerator.Value;
                tokenEnumerator.MoveNext();
            }
            else
            {
                throw new Exception();
            }

            root.Children.Add(new Import
            {
                Name = packageName
            });

            if (alias != "")
            {
                packageAliases[alias] = packageName;
            }
        }

        private bool TryParseAssignment()
        {
            string variableName = tokenEnumerator.Value;
            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.PUNCTUATION || tokenEnumerator.Value != "=")
            {
                tokenEnumerator.MovePrevious();
                return false;
            }

            tokenEnumerator.MoveNext();

            Node rValue = ParseRValue();

            stack.Peek().Children.Add(new Assignment
            {
                Name = variableName,
                RValue = rValue,
                Type = GetTypeFromNode(rValue)
            });

            return true;
        }

        private string GetTypeFromNode(Node node)
        {
            if(node is FunctionCall functionCall)
            {
                if(functionCall.PackageName == "Math")
                {
                    return "float";
                }
            }
            else if(node is BinaryExpression expression)
            {
                if(expression.Left is Variable variableL)
                {
                    string type = GetTypeFromVariable(variableL.Name);
                    if(type != "")
                    {
                        return type;
                    }
                }

                if (expression.Right is Variable variableR)
                {
                    string type = GetTypeFromVariable(variableR.Name);
                    if (type != "")
                    {
                        return type;
                    }
                }

                if("-+*/".Contains(expression.Operator))
                {
                    return "float";
                }
            }
            else if(node is Expression)
            {
                return "float";
            }
            else if(node is ListComprehension listComprehension)
            {
                return "IEnumerable<" + GetTypeFromNode(listComprehension.Expression) + ">";
            }

            return "Object";
        }

        private string GetTypeFromVariable(string name)
        {
            Assignment assigment = (Assignment)stack
                .SelectMany(x => x.Children)
                .Where(x => x is Assignment assignment && assignment.Name == name)
                .FirstOrDefault();

            if(assigment != null)
            {
                return assigment.Type;
            }

            return "";
        }

        private Node ParseRValue()
        {
            if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
            {
                if (TryParseFunctionCall(out FunctionCall functionCall))
                {
                    return functionCall;
                }

                if (TryParseBinaryExpression(out BinaryExpression binaryExpression))
                {
                    return binaryExpression;
                }
            }
            else if (tokenEnumerator.Type == TokenType.OPEN_BRACKET && tokenEnumerator.Value == "[")
            {
                if (TryParseListComprehension(out ListComprehension listComprehension))
                {
                    return listComprehension;
                }
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// tokenEnumerator.Value should be "["
        /// </summary>
        private bool TryParseListComprehension(out ListComprehension listComprehension)
        {
            tokenEnumerator.MoveNext();

            bool success = TryParseExpression(out Node expression);
            if (tokenEnumerator.Value != "for")
            {
                //need to reset the enumerator
                throw new Exception();
                listComprehension = null;
                return false;
            }

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string variable = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Value != "in")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string collection = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.CLOSE_BRACKET || tokenEnumerator.Value != "]")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            listComprehension = new ListComprehension
            {
                Collection = new Variable
                {
                    Name = collection
                },
                VariableName = variable,
                Expression = expression
            };
            return true;

            //List<Token> tokens = new List<Token>();
            //while (true)
            //{
            //    tokens.Add(tokenEnumerator.Token);

            //    if (tokenEnumerator.Type == TokenType.CLOSE_BRACKET && tokenEnumerator.Value == "]")
            //    {
            //        break;
            //    }

            //    tokenEnumerator.MoveNext();
            //}



            //for (int i = 0; i < tokens.Count; i++)
            //{
            //    tokenEnumerator.MovePrevious();
            //}

            //listComprehension = null;
            //return false;
        }

        private bool TryParseExpression(out Node node)
        {
            List<Node> coefficients = new List<Node>();
            List<string> operators = new List<string>();

            while (true)
            {
                if (tokenEnumerator.Type == TokenType.OPEN_BRACKET && tokenEnumerator.Value == "(")
                {
                    tokenEnumerator.MoveNext();

                    bool success = TryParseExpression(out Node inner);

                    if (!success)
                    {
                        throw new Exception();
                    }

                    if (tokenEnumerator.Type != TokenType.CLOSE_BRACKET || tokenEnumerator.Value != ")")
                    {
                        throw new Exception();
                    }

                    tokenEnumerator.MoveNext();
                    coefficients.Add(inner);
                }
                else if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    coefficients.Add(new Variable
                    {
                        Name = tokenEnumerator.Value
                    });

                    tokenEnumerator.MoveNext();
                }
                else
                {
                    throw new Exception();
                }

                if (tokenEnumerator.Type != TokenType.PUNCTUATION)
                {
                    node = new Expression
                    {
                        Coefficients = coefficients,
                        Operators = operators
                    };
                    return true;
                }

                if (!"-+*/".Contains(tokenEnumerator.Value))
                {
                    throw new NotImplementedException();
                    node = null;
                    return false;
                }

                string _operator = tokenEnumerator.Value;
                operators.Add(_operator);
                tokenEnumerator.MoveNext();
            }
        }

        /// <summary>
        /// tokenEnumerator.Type should be ALPHA_NUMERIC
        /// </summary>
        private bool TryParseBinaryExpression(out BinaryExpression binaryExpression)
        {
            string value = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.PUNCTUATION)
            {
                binaryExpression = null;
                tokenEnumerator.MovePrevious();
                return false;
            }

            if (!"-+*/".Contains(tokenEnumerator.Value))
            {
                binaryExpression = null;
                tokenEnumerator.MovePrevious();
                return false;
            }

            string _operator = tokenEnumerator.Value;
            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                binaryExpression = null;
                tokenEnumerator.MovePrevious();
                tokenEnumerator.MovePrevious();
                return false;
            }

            binaryExpression = new BinaryExpression
            {
                Left = new Variable
                {
                    Name = value
                },
                Operator = _operator,
                Right = new Variable
                {
                    Name = tokenEnumerator.Value
                }
            };

            tokenEnumerator.MoveNext();

            return true;
        }

        private void MakeFunctionCallGeneric(FunctionCall functionCall)
        {
            switch(functionCall.PackageName)
            {
                case "numpy":
                    switch(functionCall.FunctionName)
                    {
                        case "min":
                        case "max":
                            functionCall.PackageName = "Enumerable";
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// tokenEnumerator.Type should be ALPHA_NUMERIC
        /// </summary>
        private bool TryParseFunctionCall(out FunctionCall functionCall)
        {
            string value = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type == TokenType.PUNCTUATION && tokenEnumerator.Value == ".")
            {
                tokenEnumerator.MoveNext();

                if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
                {
                    throw new Exception();
                }

                if (packageAliases.ContainsKey(value))
                {
                    functionCall = new FunctionCall()
                    {
                        PackageName = packageAliases[value],
                        FunctionName = tokenEnumerator.Value,
                        Parameters = ParseFunctionCallParameters()
                    };

                    MakeFunctionCallGeneric(functionCall);

                    return true;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                functionCall = null;
                tokenEnumerator.MovePrevious();
                return false;
            }
        }

        private List<FunctionCallParameter> ParseFunctionCallParameters()
        {
            tokenEnumerator.MoveNext();
            if (tokenEnumerator.Value != "(")
            {
                throw new Exception();
            }

            List<FunctionCallParameter> parameters = new List<FunctionCallParameter>();

            while (true)
            {
                tokenEnumerator.MoveNext();
                if (tokenEnumerator.Value == ")")
                {
                    tokenEnumerator.MoveNext();

                    return parameters;
                }

                if (tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();
                }

                if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    parameters.Add(new FunctionCallParameter
                    {
                        Value = tokenEnumerator.Value
                    });
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// the current token has a value of "def"
        /// </summary>
        private void ParseFunction()
        {
            Function function = new Function();
            root.Children.Add(function);

            tokenEnumerator.MoveNext();
            function.Name = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();
            if (tokenEnumerator.Value != "(")
            {
                throw new Exception();
            }

            while (true)
            {
                tokenEnumerator.MoveNext();
                if (tokenEnumerator.Value == ")")
                {
                    tokenEnumerator.MoveNext();
                    if (tokenEnumerator.Value != ":")
                    {
                        throw new Exception();
                    }

                    tokenEnumerator.MoveNext();

                    return;
                }

                if (tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();
                }

                if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    function.Parameters.Add(new FunctionParameter
                    {
                        Name = tokenEnumerator.Value
                    });
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
