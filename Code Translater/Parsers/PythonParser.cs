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
        private TokenEnumerator tokenEnumerator;

        private Stack<INodeContainer> stack;
        private int currentIndent;

        private int LineNumber = 0;

        //maps aliases to their package names
        //i.e. np -> numpy
        private Dictionary<string, string> packageAliases;

        public Root Parse(string code)
        {
            Tokenizer tokenizer = new PythonTokenizer(code);
            this.tokenEnumerator = new TokenEnumerator(tokenizer);

            Root root = new Root();

            stack = new Stack<INodeContainer>();
            stack.Push(root);
            currentIndent = 0;

            packageAliases = new Dictionary<string, string>();

            LineNumber = 1;

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
                    if (indent != currentIndent)
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
                    else if(tokenEnumerator.Type == TokenType.END_OF_FILE)
                    {
                        break;
                    }
                    else
                    {
                        throw new Exception();
                    }

                }

                LineNumber++;
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
                int numBlankLines = 0;

                //blank lines at the end of a block should belong on the parent scope
                while (stack.Peek().Children.Last() is BlankLine)
                {
                    stack.Peek().Children.RemoveAt(stack.Peek().Children.Count - 1);
                    numBlankLines++;
                }

                Node mostRecent = stack.Peek().Children.Last();
                if (mostRecent is INodeContainer == false)
                {
                    throw new Exception();
                }

                stack.Push((INodeContainer)mostRecent);

                for (int i = 0; i < numBlankLines; i++)
                {
                    stack.Peek().Children.Add(new BlankLine());
                }

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
                while (stack.Peek().Children.Last() is BlankLine)
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

            if(tokenEnumerator.Value == "msg")
            {

            }

            if (TryAddAssignment())
            {
                return;
            }

            if (TryAddMultipleAssignment())
            {
                return;
            }

            if (TryParseProperty(out Node node) && node is FunctionCall)
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
                case "from":
                    AddImport();
                    return true;
                case "return":
                    AddReturn();
                    return true;
                case "while":
                    ParseWhileLoop();
                    return true;
                case "if":
                    AddIfStatement();
                    return true;
                case "break":
                    AddBreak();
                    return true;
                case "class":
                    AddClass();
                    return true;
                case "for":
                    AddForLoop();
                    return true;
                default:
                    return false;
            }
        }

        private void AddForLoop()
        {
            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string variableName = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();

            if(tokenEnumerator.Value != "in")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            if(!TryParseProperty(out Node collection))
            {
                throw new Exception();
            }

            if(tokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            stack.Peek().Children.Add(new ForEach
            {
                VariableName = variableName,
                Collection = collection
            });
        }

        private void AddClass()
        {
            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string className = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            stack.Peek().Children.Add(new Class
            {
                Name = className
            });
        }

        private void AddIfStatement()
        {
            tokenEnumerator.MoveNext();

            Node rValue;
            if (tokenEnumerator.Value == "not")
            {
                tokenEnumerator.MoveNext();

                rValue = ParseRValue();
                rValue = new Expression
                {
                    Coefficients = new List<Node>
                    {
                        rValue,
                        new BooleanLiteral
                        {
                            Value = false
                        }
                    },
                    Operators = new List<string>
                    {
                        "=="
                    }
                };
            }
            else
            {
                rValue = ParseRValue();
            }

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

            if (tokenEnumerator.Value != ":")
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
            if (stack.Any(x => x is Function) == false)
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type == TokenType.NEW_LINE)
            {
                stack.Peek().Children.Add(new Return());
                return;
            }

            Node rValue = ParseRValue();

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

            //either 'from' or 'import'
            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            if(TryParseProperty(out Node property) == false || property is Variable variable == false)
            {
                throw new Exception();
            }

            string packageName = variable.Name;
            string component;

            if (tokenEnumerator.Value == "import")
            {
                tokenEnumerator.MoveNext();
                if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
                {
                    throw new Exception();
                }

                component = tokenEnumerator.Value;
                tokenEnumerator.MoveNext();
            }
            else
            {
                component = null;
            }

            string alias;

            if (tokenEnumerator.Type == TokenType.NEW_LINE)
            {
                alias = null;
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

            stack.Peek().Children.Add(new Import
            {
                Package = packageName,
                Component = component,
            });

            if (alias != null)
            {
                if(component != null)
                {
                    packageAliases[alias] = component;
                }
                else
                {
                    packageAliases[alias] = packageName;
                }
            }
        }

        private bool TryAddAssignment()
        {
            if(LineNumber ==20)
            {

            }

            TokenizerState currentStete = tokenEnumerator.GetCurrentState();

            if(!TryParseProperty(out Node left) || left is Variable leftVariable == false)
            {
                tokenEnumerator.RestoreState(currentStete);
                return false;
            }
            
            if (tokenEnumerator.Type != TokenType.PUNCTUATION || tokenEnumerator.Value != "=")
            {
                tokenEnumerator.RestoreState(currentStete);
                return false;
            }

            tokenEnumerator.MoveNext();

            Node rValue = ParseRValue();

            stack.Peek().Children.Add(new Assignment
            {
                Name = leftVariable.Name,
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
                else if (tokenEnumerator.Value == ",")
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
            if (tokenEnumerator.Value == "True")
            {
                booleanLiteral = new BooleanLiteral
                {
                    Value = true
                };

                tokenEnumerator.MoveNext();

                return true;
            }
            else if (tokenEnumerator.Value == "False")
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

        private Node ParseUnaryRValue()
        {
            if(LineNumber == 20)
            {

            }
            if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
            {
                if(tokenEnumerator.Value == "None")
                {
                    tokenEnumerator.MoveNext();
                    return new Null();
                }

                if (TryParseBoolean(out BooleanLiteral booleanLiteral))
                {
                    return booleanLiteral;
                }

                if (TryParseProperty(out Node property))
                {
                    return property;
                }
            }
            else if (tokenEnumerator.Type == TokenType.OPEN_BRACKET)
            {
                if (tokenEnumerator.Value == "[")
                {
                    if (TryParseListComprehension(out ListComprehension listComprehension))
                    {
                        return listComprehension;
                    }
                    else if (TryParseListLiteral(out ListLiteral arrayLiteral))
                    {
                        return arrayLiteral;
                    }
                }
                else if (tokenEnumerator.Value == "(")
                {
                    if (TryParseTuple(out TupleNode tuple))
                    {
                        return tuple;
                    }
                }
            }
            else if (tokenEnumerator.Type == TokenType.NUMBER)
            {
                return ParseNumber();
            }
            else if (tokenEnumerator.Type == TokenType.STRING_LITERAL)
            {
                return ParseStringLiteral();
            }

            throw new NotImplementedException();
        }

        private Node ParseRValue()
        {
            Node expression = ParseExpression();
            if(tokenEnumerator.Value == ",")
            {
                TupleNode tupleNode = new TupleNode();
                tupleNode.Values.Add(expression);

                while(tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();

                    expression = ParseExpression();
                    tupleNode.Values.Add(expression);
                }

                return tupleNode;
            }
            else
            {
                return expression;
            }
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


        private bool TryParseListLiteral(out ListLiteral listLiteral)
        {
            TokenizerState currentState = tokenEnumerator.GetCurrentState();

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type == TokenType.CLOSE_BRACKET && tokenEnumerator.Value == "]")
            {
                tokenEnumerator.MoveNext();
                listLiteral = new ListLiteral
                {
                    Values = new List<Node>()
                };

                return true;
            }

            List<Node> values = new List<Node>();

            while (true)
            {
                values.Add(ParseExpression());

                if (tokenEnumerator.Type == TokenType.CLOSE_BRACKET && tokenEnumerator.Value == "]")
                {
                    tokenEnumerator.MoveNext();
                    listLiteral = new ListLiteral
                    {
                        Values = values
                    };

                    return true;
                }
                else if (tokenEnumerator.Type == TokenType.PUNCTUATION && tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();
                    continue;
                }
                else
                {
                    listLiteral = null;
                    tokenEnumerator.RestoreState(currentState);
                    return false;
                }
            }
        }

        private bool TryParseTuple(out TupleNode tuple)
        {
            TokenizerState currentState = tokenEnumerator.GetCurrentState();

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Type == TokenType.CLOSE_BRACKET && tokenEnumerator.Value == ")")
            {
                tokenEnumerator.MoveNext();
                tuple = new TupleNode
                {
                    Values = new List<Node>()
                };

                return true;
            }

            List<Node> values = new List<Node>();

            while (true)
            {
                values.Add(ParseExpression());

                if (tokenEnumerator.Type == TokenType.CLOSE_BRACKET && tokenEnumerator.Value == ")")
                {
                    if (values.Count > 1)
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
                else if (tokenEnumerator.Type == TokenType.PUNCTUATION && tokenEnumerator.Value == ",")
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
            TokenizerState currentState = tokenEnumerator.GetCurrentState();

            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Value == "]")
            {
                listComprehension = null;
                tokenEnumerator.RestoreState(currentState);
                return false;
            }


            Node expression = ParseExpression();
            if (tokenEnumerator.Value != "for")
            {
                listComprehension = null;
                tokenEnumerator.RestoreState(currentState);
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
        }

        private Node ParseExpression()
        {
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

                        Node inner = ParseExpression();

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
                    coefficients.Add(ParseUnaryRValue());
                }

                bool reachedEnd;
                string _operator = tokenEnumerator.Value;

                if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    switch (tokenEnumerator.Value)
                    {
                        case "is":
                            _operator = "==";
                            reachedEnd = false;
                            break;
                        case "and":
                            _operator = "&&";
                            reachedEnd = false;
                            break;
                        default:
                            reachedEnd = true;
                            break;
                    }
                }
                else if (tokenEnumerator.Type == TokenType.PUNCTUATION)
                {
                    if (tokenEnumerator.Value.Length == 1)
                    {
                        reachedEnd = !"-+*/%".Contains(tokenEnumerator.Value);
                    }
                    else if (tokenEnumerator.Value.Length == 2)
                    {
                        reachedEnd = tokenEnumerator.Value != "==";
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    reachedEnd = true;
                }

                if (reachedEnd)
                {
                    if (coefficients.Count == 1)
                    {
                        return coefficients.First();
                    }
                    else
                    {
                        return new Expression
                        {
                            Coefficients = coefficients,
                            Operators = operators
                        };
                    }
                }

                operators.Add(_operator);
                tokenEnumerator.MoveNext();
            }
        }

        private void MakeFunctionCallGeneric(FunctionCall functionCall)
        {
            if (functionCall.PackageName != null && packageAliases.ContainsKey(functionCall.PackageName))
            {
                functionCall.PackageName = packageAliases[functionCall.PackageName];
            }

            switch (functionCall.PackageName)
            {
                case "numpy":
                    switch (functionCall.FunctionName)
                    {
                        case "min":
                        case "max":
                            functionCall.PackageName = "Enumerable";
                            break;
                    }
                    break;
                case null:
                    switch (functionCall.FunctionName)
                    {
                        case "int":
                            functionCall.PackageName = "ParseOrCast";
                            break;
                        case "print":
                            functionCall.PackageName = "Console";
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
                if(value == "self")
                {
                    value = "this";
                }

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
            if (inner(out node))
            {
                if (node is FunctionCall functionCall)
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
                Node value = ParseExpression();

                if (tokenEnumerator.Value == "=" && value is Variable variable)
                {
                    tokenEnumerator.MoveNext();

                    parameters.Add(new FunctionParameter
                    {
                        Name = variable.Name,
                        Value = ParseExpression()
                    });
                }
                else
                {
                    parameters.Add(new FunctionParameter
                    {
                        Value = value
                    });
                }

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

        private string ParseTypeHint()
        {
            if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            if (tokenEnumerator.Value == "Optional")
            {
                tokenEnumerator.MoveNext();
                if (tokenEnumerator.Value != "[")
                {
                    throw new Exception();
                }

                if (!TryParseListLiteral(out ListLiteral listLiteral))
                {
                    throw new Exception();
                }

                if (listLiteral.Values.Count != 1 || listLiteral.Values[0] is Variable == false)
                {
                    throw new Exception();
                }

                return (listLiteral.Values[0] as Variable).Name + "?";
            }
            else if (tokenEnumerator.Value == "List")
            {
                tokenEnumerator.MoveNext();
                if (tokenEnumerator.Value != "[")
                {
                    throw new Exception();
                }

                if (!TryParseListLiteral(out ListLiteral listLiteral))
                {
                    throw new Exception();
                }

                if (listLiteral.Values.Count != 1 || listLiteral.Values[0] is Variable == false)
                {
                    throw new Exception();
                }

                return $"List<{(listLiteral.Values[0] as Variable).Name}>";
            }
            else
            {
                string type = tokenEnumerator.Value;
                tokenEnumerator.MoveNext();
                return type;
            }
        }

        /// <summary>
        /// the current token has a value of "def"
        /// </summary>
        private void AddFunction()
        {
            Function function = new Function();
            stack.Peek().Children.Add(function);

            tokenEnumerator.MoveNext();
            function.Name = tokenEnumerator.Value;

            tokenEnumerator.MoveNext();
            if (tokenEnumerator.Value != "(")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            bool foundEnd = false;

            if (tokenEnumerator.Value == ")")
            {
                tokenEnumerator.MoveNext();
                foundEnd = true;
            }

            while (foundEnd == false)
            {
                if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    FunctionParameter functionParameter = new FunctionParameter
                    {
                        Name = tokenEnumerator.Value
                    };

                    tokenEnumerator.MoveNext();

                    if (tokenEnumerator.Value == ":")
                    {
                        tokenEnumerator.MoveNext();
                        functionParameter.Type = ParseTypeHint();
                    }

                    if (functionParameter.Name != "self")
                    {
                        function.Parameters.Add(functionParameter);
                    }
                }
                else
                {
                    throw new Exception();
                }

                if (tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();
                }
                else if (tokenEnumerator.Value == ")")
                {
                    tokenEnumerator.MoveNext();
                    foundEnd = true;
                }
                else
                {
                    throw new Exception();
                }
            }

            if (tokenEnumerator.Value == "->")
            {
                tokenEnumerator.MoveNext();
                function.ReturnType = ParseTypeHint();
            }

            if (tokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();
        }
    }
}
