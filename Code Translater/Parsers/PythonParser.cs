using Code_Translater.AST;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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
                List<Node> extraNodes = new List<Node>();
                //blank lines and comments at the start of a block should belong on the child block
                while (stack.Peek().Children.Last() is BlankLine || stack.Peek().Children.Last() is Comment)
                {
                    Node extra = stack.Peek().Children.Last();
                    stack.Peek().Children.RemoveAt(stack.Peek().Children.Count - 1);
                }

                Node mostRecent = stack.Peek().Children.Last();
                if (mostRecent is INodeContainer == false)
                {
                    throw new Exception();
                }

                stack.Push((INodeContainer)mostRecent);

                extraNodes.Reverse();
                stack.Peek().Children.AddRange(extraNodes);

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

            Node node = ReadValue();

            if (node is FunctionCall || (node is Property property && property.Values.Last() is FunctionCall))
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
                case "elif":
                    AddElseIfStatement();
                    return true;
                case "else":
                    AddElseStatement();
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

            Node collection = ReadValue();

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

        private void AddElseIfStatement()
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

            stack.Peek().Children.Add(new ElseIf
            {
                Expression = rValue
            });
        }

        private void AddElseStatement()
        {
            tokenEnumerator.MoveNext();

            if (tokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            tokenEnumerator.MoveNext();

            stack.Peek().Children.Add(new Else());
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

            Node property = ReadValue();

            string packageName;
            if (property is Variable variable)
            {
                packageName = variable.Name;
            }
            else if(property is Property property1 && property1.Values.All(x => x is Variable))
            {
                packageName = String.Join(".", property1.Values.Cast<Variable>().Select(x => x.Name));
            }
            else
            {
                throw new Exception();
            }

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

        private bool IsLValue(Node node)
        {
            if(node is Variable)
            {
                return true;
            }
            else if(node is Property property)
            {
                if(property.Values.Last() is Variable || property.Values.Last() is ArrayAccessor)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryAddAssignment()
        {
            TokenizerState currentStete = tokenEnumerator.GetCurrentState();

            Node lValue = ReadValue();
            if(IsLValue(lValue) == false)
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

            Node rValue = ReadValue();

            stack.Peek().Children.Add(new Assignment
            {
                LValue = lValue,
                RValue = rValue
            });

            return true;
        }

        private bool TryAddMultipleAssignment()
        {
            TokenizerState currentStete = tokenEnumerator.GetCurrentState();

            List<Node> lValues = new List<Node>();

            while (true)
            {
                if (tokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
                {
                    throw new Exception();
                }


                lValues.Add(ReadValue());

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
                        LValues = lValues.Select(x => new Assignment() { LValue = x }).ToList(),
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

        private Node ReadUnary()
        {
            if (tokenEnumerator.Value == "(")
            {
                tokenEnumerator.MoveNext();

                if (tokenEnumerator.Value == ")")
                {
                    tokenEnumerator.MoveNext();
                    return new TupleNode();
                }

                Node value = ReadValue();

                if (tokenEnumerator.Value == ")")
                {
                    if (value is Expression == false)
                    {
                        Expression expression = new Expression();
                        expression.Coefficients.Add(value);
                        value = expression;
                    }

                    tokenEnumerator.MoveNext();

                    return value;
                }
                else if (tokenEnumerator.Value == ",")
                {
                    TupleNode tuple = new TupleNode();
                    tuple.Values.Add(value);

                    tokenEnumerator.MoveNext();

                    while (true)
                    {
                        tuple.Values.Add(ReadProperty());
                        if (tokenEnumerator.Value == ")")
                        {
                            break;
                        }
                        else if (tokenEnumerator.Value == ",")
                        {
                            tokenEnumerator.MoveNext();
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }

                    tokenEnumerator.MoveNext();

                    return tuple;
                }
                else
                {
                    throw new Exception();
                }
            }
            else if(tokenEnumerator.Value == "{")
            {
                return ParseDictionary();
            }
            else if (tokenEnumerator.Value == "[")
            {
                if (TryParseListComprehension(out ListComprehension listComprehension))
                {
                    return listComprehension;
                }

                tokenEnumerator.MoveNext();
                ListLiteral list = new ListLiteral();

                if (tokenEnumerator.Value == "]")
                {
                    tokenEnumerator.MoveNext();
                    return list;
                }

                while (true)
                {
                    list.Values.Add(ReadValue());

                    if (tokenEnumerator.Value == ",")
                    {
                        tokenEnumerator.MoveNext();
                    }
                    else if (tokenEnumerator.Value == "]")
                    {
                        tokenEnumerator.MoveNext();
                        return list;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
            else if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
            {
                if (tokenEnumerator.Value == "None")
                {
                    tokenEnumerator.MoveNext();
                    return new Null();
                }

                if (TryParseBoolean(out BooleanLiteral booleanLiteral))
                {
                    return booleanLiteral;
                }

                string name = tokenEnumerator.Value;
                tokenEnumerator.MoveNext();

                if (tokenEnumerator.Value == "(")
                {
                    return new FunctionCall()
                    {
                        FunctionName = name,
                        Parameters = ParseFunctionParameters()
                    };
                }
                else
                {
                    return new Variable
                    {
                        Name = name
                    };
                }
            }
            else if (tokenEnumerator.Type == TokenType.STRING_LITERAL)
            {
                return ParseStringLiteral();
            }
            else if (tokenEnumerator.Type == TokenType.NUMBER)
            {
                Number number = new Number
                {
                    Value = tokenEnumerator.Value
                };

                tokenEnumerator.MoveNext();
                return number;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private Node ReadProperty()
        {
            SkipOverNewLine();

            Node node = ReadUnary();
            if (node is Variable variable && variable.Name == "self")
            {
                variable.Name = "this";
            }

            if (node is FunctionCall functionCall)
            {
                node = MakeFunctionCallGeneric(functionCall);
            }

            while (true)
            {
                if (tokenEnumerator.Type == TokenType.PUNCTUATION)
                {
                    if (tokenEnumerator.Value == ".")
                    {
                        if (node is Property property == false)
                        {
                            property = new Property();
                            property.Values.Add(node);
                            node = property;
                        }

                        tokenEnumerator.MoveNext();

                        Node node2 = ReadUnary();
                        property.Values.Add(node2);

                        if (node2 is FunctionCall)
                        {
                            MakeFunctionCallGeneric(property);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else if (tokenEnumerator.Value == "[")
                {
                    if (node is Property property == false)
                    {
                        property = new Property();
                        property.Values.Add(node);
                        node = property;
                    }

                    Node node1 = ReadUnary();
                    if (node1 is ListLiteral list == false || list.Values.Count != 1)
                    {
                        throw new Exception();
                    }

                    property.Values.Add(new ArrayAccessor
                    {
                        Indexer = list.Values[0]
                    });
                }
                else
                {
                    break;
                }
            }

            return node;
        }

        private void SkipOverNewLine()
        {
            //sometimes dictionaries or tuples are over multiple lines
            //but this doesn't mean they have their own scope
            //so we just skip over it
            while (tokenEnumerator.Type == TokenType.NEW_LINE)
            {
                LineNumber++;
                tokenEnumerator.MoveNext();

                if (tokenEnumerator.Type == TokenType.INDENT)
                {
                    tokenEnumerator.MoveNext();
                }
            }
        }

        private Node ReadValue()
        {
            Expression expression = new Expression();
            expression.Coefficients.Add(ReadProperty());
            while (true)
            {
                if (tokenEnumerator.Type == TokenType.PUNCTUATION)
                {
                    if ("-+*/%".Contains(tokenEnumerator.Value))
                    {
                        expression.Operators.Add(tokenEnumerator.Value);
                        tokenEnumerator.MoveNext();
                        expression.Coefficients.Add(ReadProperty());
                    }
                    else if (tokenEnumerator.Value.Length == 2)
                    {
                        if(tokenEnumerator.Value == "==")
                        {
                            expression.Operators.Add(tokenEnumerator.Value);
                            tokenEnumerator.MoveNext();
                            expression.Coefficients.Add(ReadProperty());
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
                else if (tokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    string _operator = null;

                    switch (tokenEnumerator.Value)
                    {
                        case "is":
                            _operator = "is";
                            break;
                        case "and":
                            _operator = "&&";
                            break;
                        default:
                            break;
                    }

                    if(_operator != null)
                    {
                        expression.Operators.Add(_operator);
                        tokenEnumerator.MoveNext();
                        expression.Coefficients.Add(ReadProperty());
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

            if(expression.Coefficients.Count == 1)
            {
                return expression.Coefficients[0];
            }
            else
            {
                return expression;
            }
        }

        private Node ParseRValue()
        {
            Node expression = ReadValue();
            if(tokenEnumerator.Value == ",")
            {
                TupleNode tupleNode = new TupleNode();
                tupleNode.Values.Add(expression);

                while(tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();

                    expression = ReadValue();
                    tupleNode.Values.Add(expression);
                }

                return tupleNode;
            }
            else
            {
                return expression;
            }
        }

        public DictionaryLiteral ParseDictionary()
        {
            tokenEnumerator.MoveNext();
            
            DictionaryLiteral dictionary = new DictionaryLiteral();

            while (true)
            {
                if (tokenEnumerator.Value == "}")
                {
                    tokenEnumerator.MoveNext();
                    return dictionary;
                }

                Node key = ReadValue();
                if(tokenEnumerator.Value != ":")
                {
                    throw new Exception();
                }

                tokenEnumerator.MoveNext();

                Node value = ReadValue();

                dictionary.Values.Add(key, value);

                SkipOverNewLine();

                if (tokenEnumerator.Value == "}")
                {
                    tokenEnumerator.MoveNext();
                    return dictionary;
                }
                else if (tokenEnumerator.Value == ",")
                {
                    tokenEnumerator.MoveNext();
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public Node ParseStringLiteral()
        {
            string s = tokenEnumerator.Value;
            tokenEnumerator.MoveNext();

            if(s.StartsWith('f') || s.StartsWith('F'))
            {
                return new InterpolatedStringLiteral
                {
                    Value = s.Substring(1)
                };
            }
            else if(s.StartsWith("\"\"\""))
            {
                return new StringLiteral
                {
                    Value = s
                };
            }
            else
            {
                return new StringLiteral
                {
                    Value = s
                };
            }
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
                values.Add(ReadValue());

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
                values.Add(ReadValue());

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


            Node expression = ReadValue();
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

        private Node MakeFunctionCallGeneric(FunctionCall functionCall)
        {
            switch (functionCall.FunctionName)
            {
                case "int":
                    return new Property
                    {
                        Values = new List<Node>
                        {
                            new Variable
                            {
                                Name = "ParseOrCast"
                            },
                            functionCall
                        }
                    };
                case "print":
                    return new Property
                    {
                        Values = new List<Node>
                        {
                            new Variable
                            {
                                Name = "Console"
                            },
                            functionCall
                        }
                    };
                default:
                    return functionCall;
            }
        }

        private void MakeFunctionCallGeneric(Property property)
        {
            if(property.Values.Count < 2)
            {
                return;
            }

            var packageVariableNames = property.Values.Take(property.Values.Count - 1).ToList();
            if(packageVariableNames.All(x => x is Variable) == false)
            {
                return;
            }

            string packageName = String.Join(".", packageVariableNames.Select(x => ((Variable)x).Name));

            if (packageAliases.ContainsKey(packageName))
            {
                packageName = packageAliases[packageName];
            }

            FunctionCall functionCall = property.Values.Last() as FunctionCall;

            switch (packageName)
            {
                case "numpy":
                    switch (functionCall.FunctionName)
                    {
                        case "min":
                        case "max":
                            packageName = "Enumerable";
                            break;
                        default:
                            return;
                    }
                    break;
                default:
                    return;
            }

            property.Values = packageName.Split('.').Select(x => (Node)new Variable { Name = x }).ToList();
            property.Values.Add(functionCall);
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
                Node value = ReadValue();

                if (tokenEnumerator.Value == "=" && value is Variable variable)
                {
                    tokenEnumerator.MoveNext();

                    parameters.Add(new FunctionParameter
                    {
                        Name = variable.Name,
                        Value = ReadValue()
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
