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
        private TokenEnumerator TokenEnumerator;

        private Stack<INodeContainer> Stack;
        private int currentIndent;

        private int LineNumber = 0;

        //maps aliases to their package names
        //i.e. np -> numpy
        private Dictionary<string, string> packageAliases;

        public PythonParser(string code)
        {
            Tokenizer tokenizer = new PythonTokenizer(code);
            this.TokenEnumerator = new TokenEnumerator(tokenizer);
        }

        public Root Parse()
        {
            Root root = new Root();

            Stack = new Stack<INodeContainer>();
            Stack.Push(root);
            currentIndent = 0;

            packageAliases = new Dictionary<string, string>();

            LineNumber = 1;

            while (TokenEnumerator.Type != TokenType.END_OF_FILE)
            {
                int indent = 0;
                if (TokenEnumerator.Type == TokenType.INDENT)
                {
                    indent = int.Parse(TokenEnumerator.Value);
                    TokenEnumerator.MoveNext();
                }

                if (TokenEnumerator.Type == TokenType.NEW_LINE)
                {
                    //no need to use the indent to change the stack here
                    //as new lines can contain 0 indent
                    Stack.Peek().Children.Add(new BlankLine());
                    TokenEnumerator.MoveNext();
                }
                else
                {
                    if (indent != currentIndent)
                    {
                        ResetIndent(indent);
                    }

                    ParseLine();

                    if (TokenEnumerator.Value == "#")
                    {
                        Stack.Peek().Children.Last().InlineComment = ParseComment();
                    }

                    if (TokenEnumerator.Type == TokenType.NEW_LINE)
                    {
                        TokenEnumerator.MoveNext();
                    }
                    else if(TokenEnumerator.Type == TokenType.END_OF_FILE)
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
            switch (TokenEnumerator.Type)
            {
                case TokenType.ALPHA_NUMERIC:
                    ParseAlpaNumeric();
                    return;
                case TokenType.PUNCTUATION:
                    if (TokenEnumerator.Value == "#")
                    {
                        Stack.Peek().Children.Add(ParseComment());
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
            TokenEnumerator.ReadRestOfLineRaw();

            Comment comment = new Comment
            {
                Value = TokenEnumerator.Value
            };

            TokenEnumerator.MoveNext();
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
                while (Stack.Peek().Children.Last() is BlankLine || Stack.Peek().Children.Last() is Comment)
                {
                    Node extra = Stack.Peek().Children.Last();
                    Stack.Peek().Children.RemoveAt(Stack.Peek().Children.Count - 1);
                }

                Node mostRecent = Stack.Peek().Children.Last();
                if (mostRecent is INodeContainer == false)
                {
                    throw new Exception();
                }

                Stack.Push((INodeContainer)mostRecent);

                extraNodes.Reverse();
                Stack.Peek().Children.AddRange(extraNodes);

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
                while (Stack.Peek().Children.Last() is BlankLine)
                {
                    Stack.Peek().Children.RemoveAt(Stack.Peek().Children.Count - 1);
                    numBlankLines++;
                }

                Stack.Pop();

                for (int i = 0; i < numBlankLines; i++)
                {
                    Stack.Peek().Children.Add(new BlankLine());
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

            if(TokenEnumerator.Value == "msg")
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
                Stack.Peek().Children.Add(node);
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
            switch (TokenEnumerator.Value)
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
            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string variableName = TokenEnumerator.Value;

            TokenEnumerator.MoveNext();

            if(TokenEnumerator.Value != "in")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            Node collection = ReadValue();

            if(TokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            Stack.Peek().Children.Add(new ForEach
            {
                VariableName = variableName,
                Collection = collection
            });
        }

        private void AddClass()
        {
            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string className = TokenEnumerator.Value;

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            Stack.Peek().Children.Add(new Class
            {
                Name = className
            });
        }

        private void AddIfStatement()
        {
            TokenEnumerator.MoveNext();

            Node rValue;
            if (TokenEnumerator.Value == "not")
            {
                TokenEnumerator.MoveNext();

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

            if (TokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            Stack.Peek().Children.Add(new If
            {
                Expression = rValue
            });
        }

        private void AddElseIfStatement()
        {
            TokenEnumerator.MoveNext();

            Node rValue;
            if (TokenEnumerator.Value == "not")
            {
                TokenEnumerator.MoveNext();

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

            if (TokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            Stack.Peek().Children.Add(new ElseIf
            {
                Expression = rValue
            });
        }

        private void AddElseStatement()
        {
            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            Stack.Peek().Children.Add(new Else());
        }

        private void ParseWhileLoop()
        {
            TokenEnumerator.MoveNext();
            Node rValue = ParseRValue();

            if (TokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            Stack.Peek().Children.Add(new While
            {
                Expression = rValue
            });
        }

        private void AddBreak()
        {
            Stack.Peek().Children.Add(new Break());
            TokenEnumerator.MoveNext();
        }

        private void AddReturn()
        {
            if (Stack.Any(x => x is Function) == false)
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type == TokenType.NEW_LINE)
            {
                Stack.Peek().Children.Add(new Return());
                return;
            }

            Node rValue = ParseRValue();

            Stack.Peek().Children.Add(new Return
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
            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
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

            if (TokenEnumerator.Value == "import")
            {
                TokenEnumerator.MoveNext();
                if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
                {
                    throw new Exception();
                }

                component = TokenEnumerator.Value;
                TokenEnumerator.MoveNext();
            }
            else
            {
                component = null;
            }

            string alias;

            if (TokenEnumerator.Type == TokenType.NEW_LINE)
            {
                alias = null;
            }
            else if (TokenEnumerator.Type == TokenType.ALPHA_NUMERIC && TokenEnumerator.Value == "as")
            {
                TokenEnumerator.MoveNext();

                if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
                {
                    throw new Exception();
                }

                alias = TokenEnumerator.Value;
                TokenEnumerator.MoveNext();
            }
            else
            {
                throw new Exception();
            }

            Stack.Peek().Children.Add(new Import
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
            TokenizerState currentStete = TokenEnumerator.GetCurrentState();

            Node lValue = ReadValue();
            if(IsLValue(lValue) == false)
            {
                TokenEnumerator.RestoreState(currentStete);
                return false;
            }
            
            if (TokenEnumerator.Type != TokenType.PUNCTUATION || TokenEnumerator.Value != "=")
            {
                TokenEnumerator.RestoreState(currentStete);
                return false;
            }

            TokenEnumerator.MoveNext();

            Node rValue = ReadValue();

            Stack.Peek().Children.Add(new Assignment
            {
                LValue = lValue,
                RValue = rValue
            });

            return true;
        }

        private bool TryAddMultipleAssignment()
        {
            TokenizerState currentStete = TokenEnumerator.GetCurrentState();

            List<Node> lValues = new List<Node>();

            while (true)
            {
                if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
                {
                    throw new Exception();
                }


                lValues.Add(ReadValue());

                if (TokenEnumerator.Type != TokenType.PUNCTUATION)
                {
                    TokenEnumerator.RestoreState(currentStete);
                    return false;
                }

                if (TokenEnumerator.Value == "=")
                {
                    TokenEnumerator.MoveNext();

                    Node rValue = ParseRValue();

                    Stack.Peek().Children.Add(new MultipleAssignment
                    {
                        LValues = lValues.Select(x => new Assignment() { LValue = x }).ToList(),
                        RValue = rValue
                    });

                    return true;
                }
                else if (TokenEnumerator.Value == ",")
                {
                    TokenEnumerator.MoveNext();
                }
                else
                {
                    TokenEnumerator.RestoreState(currentStete);
                    return false;
                }
            }
        }

        private bool TryParseBoolean(out BooleanLiteral booleanLiteral)
        {
            if (TokenEnumerator.Value == "True")
            {
                booleanLiteral = new BooleanLiteral
                {
                    Value = true
                };

                TokenEnumerator.MoveNext();

                return true;
            }
            else if (TokenEnumerator.Value == "False")
            {
                booleanLiteral = new BooleanLiteral
                {
                    Value = false
                };

                TokenEnumerator.MoveNext();

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
            if (TokenEnumerator.Value == "(")
            {
                TokenEnumerator.MoveNext();

                if (TokenEnumerator.Value == ")")
                {
                    TokenEnumerator.MoveNext();
                    return new TupleNode();
                }

                Node value = ReadValue();

                if (TokenEnumerator.Value == ")")
                {
                    if (value is Expression == false)
                    {
                        Expression expression = new Expression();
                        expression.Coefficients.Add(value);
                        value = expression;
                    }

                    TokenEnumerator.MoveNext();

                    return value;
                }
                else if (TokenEnumerator.Value == ",")
                {
                    TupleNode tuple = new TupleNode();
                    tuple.Values.Add(value);

                    TokenEnumerator.MoveNext();

                    while (true)
                    {
                        tuple.Values.Add(ReadProperty());
                        if (TokenEnumerator.Value == ")")
                        {
                            break;
                        }
                        else if (TokenEnumerator.Value == ",")
                        {
                            TokenEnumerator.MoveNext();
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }

                    TokenEnumerator.MoveNext();

                    return tuple;
                }
                else
                {
                    throw new Exception();
                }
            }
            else if(TokenEnumerator.Value == "{")
            {
                return ParseDictionary();
            }
            else if (TokenEnumerator.Value == "[")
            {
                if (TryParseListComprehension(out ListComprehension listComprehension))
                {
                    return listComprehension;
                }

                TokenEnumerator.MoveNext();
                ListLiteral list = new ListLiteral();

                if (TokenEnumerator.Value == "]")
                {
                    TokenEnumerator.MoveNext();
                    return list;
                }

                while (true)
                {
                    list.Values.Add(ReadValue());

                    if (TokenEnumerator.Value == ",")
                    {
                        TokenEnumerator.MoveNext();
                    }
                    else if (TokenEnumerator.Value == "]")
                    {
                        TokenEnumerator.MoveNext();
                        return list;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
            else if (TokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
            {
                if (TokenEnumerator.Value == "None")
                {
                    TokenEnumerator.MoveNext();
                    return new Null();
                }

                if (TryParseBoolean(out BooleanLiteral booleanLiteral))
                {
                    return booleanLiteral;
                }

                string name = TokenEnumerator.Value;
                TokenEnumerator.MoveNext();

                if (TokenEnumerator.Value == "(")
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
            else if (TokenEnumerator.Type == TokenType.STRING_LITERAL)
            {
                return ParseStringLiteral();
            }
            else if (TokenEnumerator.Type == TokenType.NUMBER)
            {
                Number number = new Number
                {
                    Value = TokenEnumerator.Value
                };

                TokenEnumerator.MoveNext();
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
                if (TokenEnumerator.Type == TokenType.PUNCTUATION)
                {
                    if (TokenEnumerator.Value == ".")
                    {
                        if (node is Property property == false)
                        {
                            property = new Property();
                            property.Values.Add(node);
                            node = property;
                        }

                        TokenEnumerator.MoveNext();

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
                else if (TokenEnumerator.Value == "[")
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
            while (TokenEnumerator.Type == TokenType.NEW_LINE)
            {
                LineNumber++;
                TokenEnumerator.MoveNext();

                if (TokenEnumerator.Type == TokenType.INDENT)
                {
                    TokenEnumerator.MoveNext();
                }
            }
        }

        private Node ReadValue()
        {
            Expression expression = new Expression();
            expression.Coefficients.Add(ReadProperty());
            while (true)
            {
                if (TokenEnumerator.Type == TokenType.PUNCTUATION)
                {
                    if ("-+*/%".Contains(TokenEnumerator.Value))
                    {
                        expression.Operators.Add(TokenEnumerator.Value);
                        TokenEnumerator.MoveNext();
                        expression.Coefficients.Add(ReadProperty());
                    }
                    else if (TokenEnumerator.Value.Length == 2)
                    {
                        if(TokenEnumerator.Value == "==")
                        {
                            expression.Operators.Add(TokenEnumerator.Value);
                            TokenEnumerator.MoveNext();
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
                else if (TokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    string _operator = null;

                    switch (TokenEnumerator.Value)
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
                        TokenEnumerator.MoveNext();
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
            if(TokenEnumerator.Value == ",")
            {
                TupleNode tupleNode = new TupleNode();
                tupleNode.Values.Add(expression);

                while(TokenEnumerator.Value == ",")
                {
                    TokenEnumerator.MoveNext();

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
            TokenEnumerator.MoveNext();
            
            DictionaryLiteral dictionary = new DictionaryLiteral();

            while (true)
            {
                if (TokenEnumerator.Value == "}")
                {
                    TokenEnumerator.MoveNext();
                    return dictionary;
                }

                Node key = ReadValue();
                if(TokenEnumerator.Value != ":")
                {
                    throw new Exception();
                }

                TokenEnumerator.MoveNext();

                Node value = ReadValue();

                dictionary.Values.Add(key, value);

                SkipOverNewLine();

                if (TokenEnumerator.Value == "}")
                {
                    TokenEnumerator.MoveNext();
                    return dictionary;
                }
                else if (TokenEnumerator.Value == ",")
                {
                    TokenEnumerator.MoveNext();
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public Node ParseStringLiteral()
        {
            string s = TokenEnumerator.Value;
            TokenEnumerator.MoveNext();

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
            TokenizerState currentState = TokenEnumerator.GetCurrentState();

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type == TokenType.CLOSE_BRACKET && TokenEnumerator.Value == "]")
            {
                TokenEnumerator.MoveNext();
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

                if (TokenEnumerator.Type == TokenType.CLOSE_BRACKET && TokenEnumerator.Value == "]")
                {
                    TokenEnumerator.MoveNext();
                    listLiteral = new ListLiteral
                    {
                        Values = values
                    };

                    return true;
                }
                else if (TokenEnumerator.Type == TokenType.PUNCTUATION && TokenEnumerator.Value == ",")
                {
                    TokenEnumerator.MoveNext();
                    continue;
                }
                else
                {
                    listLiteral = null;
                    TokenEnumerator.RestoreState(currentState);
                    return false;
                }
            }
        }

        private bool TryParseTuple(out TupleNode tuple)
        {
            TokenizerState currentState = TokenEnumerator.GetCurrentState();

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type == TokenType.CLOSE_BRACKET && TokenEnumerator.Value == ")")
            {
                TokenEnumerator.MoveNext();
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

                if (TokenEnumerator.Type == TokenType.CLOSE_BRACKET && TokenEnumerator.Value == ")")
                {
                    if (values.Count > 1)
                    {
                        TokenEnumerator.MoveNext();
                        tuple = new TupleNode
                        {
                            Values = values
                        };

                        return true;
                    }
                    else
                    {
                        tuple = null;
                        TokenEnumerator.RestoreState(currentState);
                        return false;
                    }
                }
                else if (TokenEnumerator.Type == TokenType.PUNCTUATION && TokenEnumerator.Value == ",")
                {
                    TokenEnumerator.MoveNext();
                    continue;
                }
                else
                {
                    tuple = null;
                    TokenEnumerator.RestoreState(currentState);
                    return false;
                }
            }
        }

        private Number ParseNumber()
        {
            Number number = new Number
            {
                Value = TokenEnumerator.Value
            };

            TokenEnumerator.MoveNext();
            return number;
        }

        /// <summary>
        /// tokenEnumerator.Value should be "["
        /// </summary>
        private bool TryParseListComprehension(out ListComprehension listComprehension)
        {
            TokenizerState currentState = TokenEnumerator.GetCurrentState();

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Value == "]")
            {
                listComprehension = null;
                TokenEnumerator.RestoreState(currentState);
                return false;
            }


            Node expression = ReadValue();
            if (TokenEnumerator.Value != "for")
            {
                listComprehension = null;
                TokenEnumerator.RestoreState(currentState);
                return false;
            }

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string variable = TokenEnumerator.Value;

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Value != "in")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            string collection = TokenEnumerator.Value;

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Type != TokenType.CLOSE_BRACKET || TokenEnumerator.Value != "]")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

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
            if (TokenEnumerator.Value != "(")
            {
                throw new Exception();
            }

            List<FunctionParameter> parameters = new List<FunctionParameter>();

            TokenEnumerator.MoveNext();

            if (TokenEnumerator.Value == ")")
            {
                TokenEnumerator.MoveNext();
                return parameters;
            }

            while (true)
            {
                Node value = ReadValue();

                if (TokenEnumerator.Value == "=" && value is Variable variable)
                {
                    TokenEnumerator.MoveNext();

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

                if (TokenEnumerator.Value == ",")
                {
                    TokenEnumerator.MoveNext();
                    continue;
                }
                else if (TokenEnumerator.Value == ")")
                {
                    TokenEnumerator.MoveNext();
                    return parameters;
                }

                throw new Exception();
            }
        }

        private string ParseTypeHint()
        {
            if (TokenEnumerator.Type != TokenType.ALPHA_NUMERIC)
            {
                throw new Exception();
            }

            if (TokenEnumerator.Value == "Optional")
            {
                TokenEnumerator.MoveNext();
                if (TokenEnumerator.Value != "[")
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
            else if (TokenEnumerator.Value == "List")
            {
                TokenEnumerator.MoveNext();
                if (TokenEnumerator.Value != "[")
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
                string type = TokenEnumerator.Value;
                TokenEnumerator.MoveNext();
                return type;
            }
        }

        /// <summary>
        /// the current token has a value of "def"
        /// </summary>
        private void AddFunction()
        {
            Function function = new Function();
            Stack.Peek().Children.Add(function);

            TokenEnumerator.MoveNext();
            function.Name = TokenEnumerator.Value;

            TokenEnumerator.MoveNext();
            if (TokenEnumerator.Value != "(")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();

            bool foundEnd = false;

            if (TokenEnumerator.Value == ")")
            {
                TokenEnumerator.MoveNext();
                foundEnd = true;
            }

            while (foundEnd == false)
            {
                if (TokenEnumerator.Type == TokenType.ALPHA_NUMERIC)
                {
                    FunctionParameter functionParameter = new FunctionParameter
                    {
                        Name = TokenEnumerator.Value
                    };

                    TokenEnumerator.MoveNext();

                    if (TokenEnumerator.Value == ":")
                    {
                        TokenEnumerator.MoveNext();
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

                if (TokenEnumerator.Value == ",")
                {
                    TokenEnumerator.MoveNext();
                }
                else if (TokenEnumerator.Value == ")")
                {
                    TokenEnumerator.MoveNext();
                    foundEnd = true;
                }
                else
                {
                    throw new Exception();
                }
            }

            if (TokenEnumerator.Value == "->")
            {
                TokenEnumerator.MoveNext();
                function.ReturnType = ParseTypeHint();
            }

            if (TokenEnumerator.Value != ":")
            {
                throw new Exception();
            }

            TokenEnumerator.MoveNext();
        }
    }
}
