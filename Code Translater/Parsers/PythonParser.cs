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
    public class PythonParser : IParser
    {
        private Root root;
        private TokenEnumerator tokenEnumerator;

        private Stack<INodeContainer> stack;
        private int currentIndent;

        //maps aliases to their package names
        //i.e. np -> numpy
        private Dictionary<string, string> packageAliases;

        public Root Parse(string code)
        {
            Tokenizer tokenizer = new PythonTokenizer(code);
            this.tokenEnumerator = new TokenEnumerator(tokenizer);

            root = new Root();

            stack = new Stack<INodeContainer>();
            stack.Push(root);
            currentIndent = 0;

            packageAliases = new Dictionary<string, string>();

            int lineNumber = 1;

            while (tokenEnumerator.Type != TokenType.END_OF_FILE)
            {
                int indent = 0;
                if (tokenEnumerator.Type == TokenType.INDENT)
                {
                    indent = int.Parse(tokenEnumerator.Value);
                    tokenEnumerator.MoveNext();
                }

                if (tokenEnumerator.Type == TokenType.NEW_LINE)
                {
                    //no need to use the indent to change the stack here
                    //as new lines can contain 0 indent
                    stack.Peek().Children.Add(new BlankLine());
                    tokenEnumerator.MoveNext();
                }
                else
                {
                    if(indent != currentIndent)
                    {
                        ResetIndent(indent);
                    }

                    ParseLine();

                    if (tokenEnumerator.Value == "#")
                    {
                        stack.Peek().Children.Last().InlineComment = ParseComment();
                    }

                    if (tokenEnumerator.Type == TokenType.NEW_LINE)
                    {
                        tokenEnumerator.MoveNext();
                    }
                }

                lineNumber++;
            }

            return root;
        }

        private void ParseLine()
        {
            switch (tokenEnumerator.Type)
            {
                case TokenType.ALPHA_NUMERIC:
                    ParseAlpaNumeric();
                    return;
                case TokenType.PUNCTUATION:
                    if (tokenEnumerator.Value == "#")
                    {
                        stack.Peek().Children.Add(ParseComment());
                        return;
                    }
                    break;
                case TokenType.NEW_LINE:
                    throw new Exception();   
            }

            throw new NotImplementedException();
        }

        private Comment ParseComment()
        {
            tokenEnumerator.ReadRestOfLineRaw();

            Comment comment = new Comment
            {
                Value = tokenEnumerator.Value
            };
            
            tokenEnumerator.MoveNext();
            return comment;
        }

        /// <summary>
        /// the current token has a type of INDENT
        /// </summary>
        private void ResetIndent(int newIndent)
        {
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
                int numBlankLines = 0;

                //blank lines at the end of a block should belong on the parent scope
                while(stack.Peek().Children.Last() is BlankLine)
                {
                    stack.Peek().Children.RemoveAt(stack.Peek().Children.Count - 1);
                    numBlankLines++;
                }

                stack.Pop();

                for (int i = 0; i < numBlankLines; i++)
                {
                    stack.Peek().Children.Add(new BlankLine());
                }

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

            if (TryAddAssignment())
            {
                return;
            }

            if(TryAddMultipleAssignment())
            {
                return;
            }

            if(TryParseProperty(out Node node) && node is FunctionCall)
            {
                stack.Peek().Children.Add(node);
                return;
            }
            else
            {
                throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        private bool TryParseKeyword()
        {
            switch (tokenEnumerator.Value)
            {
                case "def":
                    AddFunction();
                    return true;
                case "import":
                    AddImport();
                    return true;
                case "return":
                    AddReturn();
                    return true;
                case "while":
                    ParseWhileLoop();
                    return true;
                case "if":
                    ParseIfStatement();
                    return true;
                case "break":
                    AddBreak();
                    return true;
                default:
                    return false;
            }
        }

        private void ParseIfStatement()
        {
            tokenEnumerator.MoveNext();
            Node rValue = ParseRValue();

            if (tokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            stack.Peek().Children.Add(new If
            {
                Expression = rValue
            });
        }

        private void ParseWhileLoop()
        {
            tokenEnumerator.MoveNext();
            Node rValue = ParseRValue();

            if(tokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            stack.Peek().Children.Add(new While
            {
                Expression = rValue
            });
        }

        private void AddBreak()
        {
            stack.Peek().Children.Add(new Break());
            tokenEnumerator.MoveNext();
        }

        private void AddReturn()
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

            stack.Peek().Children.Add(new Return
            {
                Value = rValue
            });
        }

        private void AddImport()
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

        private bool TryAddAssignment()
        {
            TokenizerState currentStete = tokenEnumerator.GetCurrentState();

            string variableName = tokenEnumerator.Value;
            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.PUNCTUATION || tokenEnumerator.Value != "=")
            {
                tokenEnumerator.RestoreState(currentStete);
                return false;
            }

            tokenEnumerator.MoveNext();

            Node rValue = ParseRValue();

            stack.Peek().Children.Add(new Assignment
            {
                Name = variableName,
                RValue = rValue
            });

            return true;
        }

        private bool TryAddMultipleAssignment()
        {
            TokenizerState currentStete = tokenEnumerator.GetCurrentState();

            List<string> variableNames = new List<string>();

            while (true)
            {
                if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
                {
                    throw new Exception();
                }

                variableNames.Add(tokenEnumerator.Value);
                tokenEnumerator.MoveNext();

                if (tokenEnumerator.Type != TokenType.PUNCTUATION)
                {
                    tokenEnumerator.RestoreState(currentStete);
                    return false;
                }

                if (tokenEnumerator.Value == "=")
                {
                    tokenEnumerator.MoveNext();

                    Node rValue = ParseRValue();

                    stack.Peek().Children.Add(new MultipleAssignment
                    {
                        VariableNames = variableNames,
                        RValue = rValue
                    });

                    return true;
                }
                else if(tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();
                }
                else
                {
                    tokenEnumerator.RestoreState(currentStete);
                    return false;
                }
            }
        }

        private bool TryParseBoolean(out BooleanLiteral booleanLiteral)
        {
            if(tokenEnumerator.Value == "True")
            {
                booleanLiteral = new BooleanLiteral
                {
                    Value = true
                };

                tokenEnumerator.MoveNext();

                return true;
            }
            else if(tokenEnumerator.Value == "False")
            {
                booleanLiteral = new BooleanLiteral
                {
                    Value = false
                };

                tokenEnumerator.MoveNext();

                return true;
            }
            else
            {
                booleanLiteral = null;
                return false;
            }
        }

        private bool TryParseUnaryRValue(out Node node)
        {
            if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
            {
                if (TryParseBoolean(out BooleanLiteral booleanLiteral))
                {
                    node = booleanLiteral;
                    return true;
                }

                if (TryParseProperty(out Node property))
                {
                    node = property;
                    return true;
                }
            }
            else if (tokenEnumerator.Type == TokenType.OPEN_BRACKET)
            {
                if (tokenEnumerator.Value == "[")
                {
                    if (TryParseListComprehension(out ListComprehension listComprehension))
                    {
                        node = listComprehension;
                        return true;
                    }
                }
                else if (tokenEnumerator.Value == "(")
                {
                    if (TryParseTuple(out TupleNode tuple))
                    {
                        node = tuple;
                        return true;
                    }
                }
            }
            else if (tokenEnumerator.Type == TokenType.NUMBER)
            {
                node = ParseNumber();
                return true;
            }
            else if (tokenEnumerator.Type == TokenType.STRING_LITERAL)
            {
                node = ParseStringLiteral();
                return true;
            }

            node = null;
            return false;
        }

        private Node ParseRValue()
        {
            if (TryParseExpression(out Node expression))
            {
                return expression;
            }

            throw new NotImplementedException();
        }

        public StringLiteral ParseStringLiteral()
        {
            string s = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();

            return new StringLiteral
            {
                Value = s
            };
        }

        private bool TryParseTuple(out TupleNode tuple)
        {
            TokenizerState currentState = tokenEnumerator.GetCurrentState();

            List<Node> values = new List<Node>();

            tokenEnumerator.MoveNext();

            while (true)
            {
                values.Add(ParseRValue());

                if(tokenEnumerator.Type == TokenType.CLOSE_BRACKET && tokenEnumerator.Value == ")")
                {
                    if(values.Count > 1)
                    {
                        tokenEnumerator.MoveNext();

                        tuple = new TupleNode
                        {
                            Values = values
                        };

                        return true;
                    }
                    else
                    {
                        tuple = null;
                        tokenEnumerator.RestoreState(currentState);
                        return false;
                    }
                }
                else if(tokenEnumerator.Type == TokenType.PUNCTUATION && tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();
                    continue;
                }
                else
                {
                    tuple = null;
                    tokenEnumerator.RestoreState(currentState);
                    return false;
                }
            }
        }

        private Number ParseNumber()
        {
            Number number = new Number
            {
                Value = tokenEnumerator.Value
            };

            tokenEnumerator.MoveNext();
            return number;
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
            TokenizerState currentState = tokenEnumerator.GetCurrentState();

            List<Node> coefficients = new List<Node>();
            List<string> operators = new List<string>();

            while (true)
            {
                if (tokenEnumerator.Type == TokenType.OPEN_BRACKET && tokenEnumerator.Value == "(")
                {
                    if (TryParseTuple(out TupleNode tuple))
                    {
                        coefficients.Add(tuple);
                    }
                    else
                    {
                        tokenEnumerator.MoveNext();

                        bool success = TryParseExpression(out Node inner);

                        if (!success)
                        {
                            tokenEnumerator.RestoreState(currentState);
                            node = null;
                            return false;
                        }

                        if (tokenEnumerator.Type != TokenType.CLOSE_BRACKET || tokenEnumerator.Value != ")")
                        {
                            throw new Exception();
                        }

                        tokenEnumerator.MoveNext();
                        coefficients.Add(inner);
                    }
                }
                else
                {
                    if (TryParseUnaryRValue(out Node inner))
                    {
                        coefficients.Add(inner);
                    }
                    else
                    {
                        tokenEnumerator.RestoreState(currentState);
                        node = null;
                        return false;
                    }
                }

                bool reachedEnd = false;
                if (tokenEnumerator.Type != TokenType.PUNCTUATION)
                {
                    reachedEnd = true;
                }
                else
                {
                    if(tokenEnumerator.Value.Length == 1)
                    {
                        reachedEnd = !"-+*/%".Contains(tokenEnumerator.Value);
                    }
                    else if(tokenEnumerator.Value.Length == 2)
                    {
                        reachedEnd = tokenEnumerator.Value != "==";
                    }
                    else
                    {
                        throw new Exception();
                    }
                }

                if (reachedEnd)
                {
                    if(coefficients.Count == 1)
                    {
                        node = coefficients.First();
                    }
                    else
                    {
                        node = new Expression
                        {
                            Coefficients = coefficients,
                            Operators = operators
                        };
                    }
                    
                    return true;
                }

                string _operator = tokenEnumerator.Value;
                operators.Add(_operator);
                tokenEnumerator.MoveNext();
            }
        }

        private void MakeFunctionCallGeneric(FunctionCall functionCall)
        {
            if(functionCall.PackageName != null && packageAliases.ContainsKey(functionCall.PackageName))
            {
                functionCall.PackageName = packageAliases[functionCall.PackageName];
            }

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
                case null:
                    switch(functionCall.FunctionName)
                    {
                        case "int":
                            functionCall.PackageName = "ParseOrCast";
                            break;
                    }
                    break;
            }
        }

        private bool TryParseProperty(out Node node)
        {
            bool inner(out Node node2)
            {
                string value = tokenEnumerator.Value;

                tokenEnumerator.MoveNext();

                if (tokenEnumerator.Type == TokenType.OPEN_BRACKET && tokenEnumerator.Value == "(")
                {
                    node2 = new FunctionCall()
                    {
                        PackageName = null,
                        FunctionName = value,
                        Parameters = ParseFunctionParameters()
                    };

                    return true;
                }
                else if (tokenEnumerator.Type == TokenType.PUNCTUATION && tokenEnumerator.Value == ".")
                {
                    tokenEnumerator.MoveNext();

                    if (inner(out Node subNode))
                    {
                        if (subNode is FunctionCall functionCall)
                        {
                            if (functionCall.PackageName != null)
                            {
                                functionCall.PackageName = value + "." + functionCall.PackageName;
                            }
                            else
                            {
                                functionCall.PackageName = value;
                            }
                        }
                        else if (subNode is Variable variable)
                        {
                            variable.Name = value + "." + variable.Name;
                        }

                        node2 = subNode;
                        return true;
                    }
                }
                else
                {
                    node2 = new Variable
                    {
                        Name = value
                    };

                    return true;
                }

                node2 = null;
                
                return false;
            }

            TokenizerState currentStete = tokenEnumerator.GetCurrentState();
            if(inner(out node))
            {
                if(node is FunctionCall functionCall)
                {
                    MakeFunctionCallGeneric(functionCall);
                }

                return true;
            }
            else
            {
                tokenEnumerator.RestoreState(currentStete);
                return false;
            }
        }

        private List<FunctionParameter> ParseFunctionParameters()
        {
            if (tokenEnumerator.Value != "(")
            {
                throw new Exception();
            }

            List<FunctionParameter> parameters = new List<FunctionParameter>();

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Value == ")")
            {
                tokenEnumerator.MoveNext();
                return parameters;
            }

            while (true)
            {
                parameters.Add(new FunctionParameter
                {
                    Value = ParseRValue()
                });

                if (tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();
                    continue;
                }
                else if (tokenEnumerator.Value == ")")
                {
                    tokenEnumerator.MoveNext();
                    return parameters;
                }
                
                throw new Exception();
            }
        }

        /// <summary>
        /// the current token has a value of "def"
        /// </summary>
        private void AddFunction()
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
