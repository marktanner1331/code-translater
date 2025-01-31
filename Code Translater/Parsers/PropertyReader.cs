using System;
using System.Collections.Generic;
using Code_Translater.AST;
using Code_Translater.Tokenizers;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class PropertyReader
    {
        private readonly Parser _parser;
        private readonly Dictionary<string, string> _variableNameRemap;

        public PropertyReader(Parser parser)
        {
            _parser = parser;
            _variableNameRemap = new Dictionary<string, string>();
        }

        public void RemapVariableName(string oldName, string newName)
        {
            _variableNameRemap[oldName] = newName;
        }
        
        public Node ReadProperty()
        {
            _parser.SkipOverNewLine();

            var node = _parser.ReadUnary();
            if (node is Variable variable && _variableNameRemap.TryGetValue(variable.Name, out var newName))
            {
                variable.Name = newName;
            }

            if (node is FunctionCall functionCall)
            {
                node = _parser.MakeFunctionCallGeneric(functionCall);
            }

            while (true)
            {
                if (_parser.TokenEnumerator.Type == TokenType.PUNCTUATION)
                {
                    if (_parser.TokenEnumerator.Value == ".")
                    {
                        if (node is Property property == false)
                        {
                            property = new Property();
                            property.Values.Add(node);
                            node = property;
                        }

                        _parser.TokenEnumerator.MoveNext();

                        var node2 = _parser.ReadUnary();
                        property.Values.Add(node2);

                        if (node2 is FunctionCall)
                        {
                            _parser.MakeFunctionCallGeneric(property);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else if (_parser.TokenEnumerator.Value == "[")
                {
                    if (node is Property property == false)
                    {
                        property = new Property();
                        property.Values.Add(node);
                        node = property;
                    }

                    Node node1 = _parser.ReadUnary();
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
        
        public interface Parser
        {
            TokenEnumerator TokenEnumerator { get; }
            void SkipOverNewLine();
            Node ReadUnary();
            Node MakeFunctionCallGeneric(FunctionCall functionCall);
            void MakeFunctionCallGeneric(Property property);
        }
    }
}