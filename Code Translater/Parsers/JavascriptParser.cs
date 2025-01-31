using System;
using System.Collections.Generic;
using System.Linq;
using Code_Translater.AST;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class JavascriptParser : IParser, PropertyReader.Parser, NewLineSkipper.Parser, ValueReader.Parser,
        FunctionParametersReader.Parser, AlphaNumericAdder.Parser, AssignmentAdder.Parser, CommentReader.Parser
    {
        public TokenEnumerator TokenEnumerator { get; }

        public Stack<INodeContainer> Stack { get; }

        public int LineNumber { get; set; } = 0;

        private readonly PropertyReader _propertyReader;
        private readonly NewLineSkipper _newLineSkipper;
        private readonly ValueReader _valueReader;
        private readonly FunctionParametersReader _functionParametersReader;
        private readonly AssignmentAdder _assignmentAdder;
        private readonly AlphaNumericAdder _alphaNumericAdder;
        private readonly LValueTester _lValueTester;
        private readonly CommentReader _commentReader;

        public JavascriptParser(string code)
        {
            Tokenizer tokenizer = new PythonTokenizer(code);
            this.TokenEnumerator = new TokenEnumerator(tokenizer);
            
            Stack = new Stack<INodeContainer>();
            
            this._propertyReader = new PropertyReader(this);
            this._newLineSkipper = new NewLineSkipper(this);
            this._valueReader = new ValueReader(this);
            this._functionParametersReader = new FunctionParametersReader(this);
            this._assignmentAdder = new AssignmentAdder(this);
            this._alphaNumericAdder = new AlphaNumericAdder(this);
            this._lValueTester = new LValueTester();
            this._commentReader = new CommentReader(this);
        }
        
        public Root Parse()
        {
            Root root = new Root();
            Stack.Push(root);

            LineNumber = 1;

            while (TokenEnumerator.Type != TokenType.END_OF_FILE)
            {
                if (TokenEnumerator.Type == TokenType.INDENT)
                {
                    TokenEnumerator.MoveNext();
                    continue;
                }

                if (TokenEnumerator.Type == TokenType.NEW_LINE)
                {
                    Stack.Peek().Children.Add(new BlankLine());
                    TokenEnumerator.MoveNext();
                    LineNumber++;
                    continue;
                }
                else
                {
                    switch (TokenEnumerator.Type)
                    {
                        case TokenType.ALPHA_NUMERIC:
                            _alphaNumericAdder.AddAlpaNumeric();
                            return;
                        case TokenType.PUNCTUATION:
                            if (TokenEnumerator.Value.StartsWith("//"))
                            {
                                Stack.Peek().Children.Add(_commentReader.ReadComment());
                                return;
                            }
                            break;
                        case TokenType.NEW_LINE:
                            throw new Exception();
                    }
                    
                    if (TokenEnumerator.Value == "//")
                    {
                        Stack.Peek().Children.Last().InlineComment = _commentReader.ReadComment();
                    }

                    if (TokenEnumerator.Type == TokenType.NEW_LINE)
                    {
                        TokenEnumerator.MoveNext();
                    }
                    else if (TokenEnumerator.Type == TokenType.END_OF_FILE)
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

        public void SkipOverNewLine()
        {
            _newLineSkipper.Skip();
        }

        public Node ReadProperty()
        {
            return _propertyReader.ReadProperty();
        }

        public Node ReadValue()
        {
            return _valueReader.ReadValue();
        }
        
        public bool IsLValue(Node node)
        {
            return _lValueTester.IsLValue(node);
        }

        public List<FunctionParameter> ReadFunctionParameters()
        {
            return _functionParametersReader.ReadFunctionParameters();
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

        public Node ReadUnary()
        {
            if (TokenEnumerator.Value == "{")
            {
                return ParseDictionary();
            }
            else if (TokenEnumerator.Value == "[")
            {
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
                        Parameters = ReadFunctionParameters()
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
        
        public Node ParseStringLiteral()
        {
            string s = TokenEnumerator.Value;
            TokenEnumerator.MoveNext();
            
            return new StringLiteral
            {
                Value = s
            };
        }
        
        private bool TryParseBoolean(out BooleanLiteral booleanLiteral)
        {
            if (TokenEnumerator.Value == "true")
            {
                booleanLiteral = new BooleanLiteral
                {
                    Value = true
                };

                TokenEnumerator.MoveNext();

                return true;
            }
            else if (TokenEnumerator.Value == "false")
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

        public Node MakeFunctionCallGeneric(FunctionCall functionCall)
        {
            return functionCall;
        }

        public void MakeFunctionCallGeneric(Property property)
        {
            
        }
        
        public bool TryAddAssignment()
        {
            return _assignmentAdder.TryAddAssignment();
        }

        public bool TryAddKeyword()
        {
            switch (TokenEnumerator.Value)
            {
                case "function":
                    AddFunction();
                    return true;
                // case "import":
                // case "from":
                //     AddImport();
                //     return true;
                // case "return":
                //     AddReturn();
                //     return true;
                // case "while":
                //     ParseWhileLoop();
                //     return true;
                // case "if":
                //     AddIfStatement();
                //     return true;
                // case "elif":
                //     AddElseIfStatement();
                //     return true;
                // case "else":
                //     AddElseStatement();
                //     return true;
                // case "break":
                //     AddBreak();
                //     return true;
                // case "class":
                //     AddClass();
                //     return true;
                // case "for":
                //     AddForLoop();
                //     return true;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// the current token has a value of "function"
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
           
                    function.Parameters.Add(functionParameter);
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

            TokenEnumerator.MoveNext();
        }
    }
}