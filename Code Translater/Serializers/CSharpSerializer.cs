using Code_Translater.AST;
using Code_Translater.Transformers;
using Code_Translater.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code_Translater.Serializers
{
    public class CSharpSerializer : ASTProcessor, ISerializer
    {
        private StringBuilder stringBuilder;
        private int Indent = 0;
        private bool NeedsNewLine = false;

        private Stack<List<string>> ScopedVariabies;
        private Stack<INodeContainer> scopeStack;

        public string Serialize(Node root)
        {
            new AddTypesTransformer().AddTypes(root);
            new RemoveMultipleAssignment().RemoveMultipleAssignments(root);
            new RemoveImports().Remove(root);
            MultiCleaner multiCleaner = new MultiCleaner();
            multiCleaner.RemoveTripleQuotedStrings = true;
            multiCleaner.RemoveSingleQuotedStrings = true;
            multiCleaner.DeconstructTupleFunctionParameters = true;
            multiCleaner.Clean(root);

            stringBuilder = new StringBuilder();

            ScopedVariabies = new Stack<List<string>>();
            scopeStack = new Stack<INodeContainer>();

            //outer 'file-level' scope
            ScopedVariabies.Push(new List<string>());

            Process(root);

            return stringBuilder.ToString();
        }

        private bool HasVariableInScope(string name)
        {
            if (name.StartsWith("this."))
            {
                name = name.Substring(5);
            }

            return ScopedVariabies.SelectMany(x => x).Any(x => x == name);
        }

        protected override void Process(Node node)
        {
            if (NeedsNewLine)
            {
                stringBuilder.AppendLine();
                AddIndent();
                NeedsNewLine = false;
            }

            base.Process(node);
        }

        private void AddIndent()
        {
            stringBuilder.Append(new String('\t', Indent));
        }

        protected override void ProcessExpression(Expression expression)
        {
            void processCoefficient(Node node)
            {
                if (node is Expression)
                {
                    stringBuilder.Append("(");
                    Process(node);
                    stringBuilder.Append(")");
                }
                else
                {
                    Process(node);
                }
            }

            IEnumerator<Node> coefficients = expression.Coefficients.GetEnumerator();
            coefficients.MoveNext();

            processCoefficient(coefficients.Current);

            IEnumerator<string> operators = expression.Operators.GetEnumerator();

            while (coefficients.MoveNext() && operators.MoveNext())
            {
                stringBuilder.Append(" " + operators.Current + " ");
                processCoefficient(coefficients.Current);
            }
        }

        protected override void ProcessListComprehension(ListComprehension listComprehension)
        {
            Process(listComprehension.Collection);
            stringBuilder.Append(".Select(");
            stringBuilder.Append(listComprehension.VariableName);
            stringBuilder.Append(" => ");
            Process(listComprehension.Expression);
            stringBuilder.Append(")");
        }

        protected override void ProcessReturn(Return @return)
        {
            stringBuilder.Append("return");

            if (@return.Value != null)
            {
                stringBuilder.Append(" ");
                Process(@return.Value);
            }

            NeedsNewLine = true;
        }

        protected override void ProcessVariable(Variable variable)
        {
            stringBuilder.Append(variable.Name);
        }

        private void MakeFunctionCallNonGeneric(Property property)
        {
            if (property.Values.Count < 2)
            {
                return;
            }

            var packageVariableNames = property.Values.Take(property.Values.Count - 1).ToList();
            if (packageVariableNames.All(x => x is Variable) == false)
            {
                return;
            }

            string packageName = String.Join(".", packageVariableNames.Select(x => ((Variable)x).Name));
            string newPackageName = packageName;

            FunctionCall functionCall = property.Values.Last() as FunctionCall;

            switch (packageName)
            {
                case "Enumerable":
                    switch (functionCall.FunctionName)
                    {
                        case "min":
                            functionCall.FunctionName = "Min";
                            break;
                        case "max":
                            functionCall.FunctionName = "Max";
                            break;
                    }
                    break;
                case "ParseOrCast":
                    switch (functionCall.FunctionName)
                    {
                        case "int":
                            if (functionCall.Parameters.First().Type == null)
                            {
                                //assume it's a string
                                newPackageName = "Int";
                                functionCall.FunctionName = "Parse";
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                            break;
                    }
                    break;
                case "Console":
                    switch (functionCall.FunctionName)
                    {
                        case "print":
                            functionCall.FunctionName = "WriteLine";
                            break;
                    }
                    break;
            }

            if(packageName != newPackageName)
            {
                property.Values = newPackageName.Split('.').Select(x => (Node)new Variable { Name = x }).ToList();
                property.Values.Add(functionCall);
            }
        }

        protected override void ProcessFunctionCall(FunctionCall functionCall)
        {
            stringBuilder.Append(functionCall.FunctionName);

            stringBuilder.Append('(');

            IEnumerator<FunctionParameter> parameters = functionCall.Parameters.GetEnumerator();

            if (parameters.MoveNext())
            {
                while (true)
                {
                    if(parameters.Current.Name != null)
                    {
                        stringBuilder.Append(parameters.Current.Name + " = ");
                    }

                    Process(parameters.Current.Value);

                    if (parameters.MoveNext())
                    {
                        stringBuilder.Append(", ");
                    }
                    else
                    {
                        break;
                    }
                }
            }

            stringBuilder.Append(')');
        }

        protected override void ProcessAssignment(Assignment assignment)
        {
            if (assignment.LValue is Variable variable)
            {
                if (HasVariableInScope(variable.Name) == false)
                {
                    stringBuilder.Append(assignment.Type ?? "var");
                    stringBuilder.Append(" ");
                    ScopedVariabies.Peek().Add(variable.Name);
                }
            }

            Process(assignment.LValue);

            stringBuilder.Append(" = ");

            Process(assignment.RValue);

            NeedsNewLine = true;
        }

        protected override void ProcessFunction(Function function)
        {
            if (function.ReturnType == null)
            {
                function.ReturnType = "void";
            }

            if (scopeStack.Any(x => x is Class) == false)
            {
                stringBuilder.Append("static ");
            }

            stringBuilder.Append(function.ReturnType);
            stringBuilder.Append(" ");
            stringBuilder.Append(function.Name);
            stringBuilder.Append('(');

            List<string> paramsString = new List<string>();
            foreach (FunctionParameter param in function.Parameters)
            {
                if (param.Type == null)
                {
                    param.Type = "object";
                }

                ScopedVariabies.Peek().Add(param.Name);
                paramsString.Add(param.Type + " " + param.Name);
            }

            stringBuilder.Append(string.Join(", ", paramsString));

            stringBuilder.Append(')');
            stringBuilder.AppendLine();

            ProcessNodeContainer(function);
        }

        protected override void ProcessRoot(Root root)
        {
            ProcessNodeContainer(root);
        }

        protected override void ProcessImport(Import import)
        {
            //we handle imports manually
        }

        protected override void ProcessComment(Comment comment)
        {
            stringBuilder.Append("//" + comment.Value);
            NeedsNewLine = true;
        }

        protected override void ProcessBlankLine()
        {
            NeedsNewLine = true;
        }

        protected override void ProcessNumber(Number number)
        {
            stringBuilder.Append(number.Value);
        }

        protected override void ProcessBreak()
        {
            stringBuilder.Append("break");
            NeedsNewLine = true;
        }

        protected override void ProcessIf(If @if)
        {
            stringBuilder.Append("if ");
            stringBuilder.Append('(');
            Process(@if.Expression);

            stringBuilder.Append(')');
            stringBuilder.AppendLine();

            ProcessNodeContainer(@if);
        }

        protected override void ProcessWhile(While @while)
        {
            stringBuilder.Append("while ");
            stringBuilder.Append('(');
            Process(@while.Expression);

            stringBuilder.Append(')');
            stringBuilder.AppendLine();

            ProcessNodeContainer(@while);
        }

        private void ProcessNodeContainer(INodeContainer nodeContainer)
        {
            if (nodeContainer is Root == false)
            {
                AddIndent();

                stringBuilder.Append("{");
                NeedsNewLine = true;

                Indent++;
            }

            scopeStack.Push(nodeContainer);
            ScopedVariabies.Push(new List<string>());

            foreach (Node node in nodeContainer.Children)
            {
                Process(node);

                if (node is INodeContainer == false && node is Comment == false && node is BlankLine == false)
                {
                    stringBuilder.Append(";");
                }

                if (node.InlineComment != null)
                {
                    stringBuilder.Append(" ");

                    NeedsNewLine = false;
                    Process(node.InlineComment);
                    NeedsNewLine = true;
                }

                NeedsNewLine = true;
            }

            ScopedVariabies.Pop();
            scopeStack.Pop();

            if (nodeContainer is Root == false)
            {
                stringBuilder.AppendLine();
                Indent--;

                AddIndent();
                stringBuilder.Append("}");
                NeedsNewLine = true;
            }
        }

        protected override void ProcessBooleanLiteral(BooleanLiteral booleanLiteral)
        {
            stringBuilder.Append(booleanLiteral.Value.ToString().ToLower());
        }

        protected override void ProcessMultipleAssignment(MultipleAssignment multipleAssignment)
        {
            //multiple assignment should have been stripped out by a transformer
            throw new Exception();
        }

        protected override void ProcessTupleNode(TupleNode tupleNode)
        {
            if (tupleNode.Values.Count > 0)
            {
                stringBuilder.Append("new List<object> { ");

                IEnumerator<Node> nodes = tupleNode.Values.GetEnumerator();

                nodes.MoveNext();

                while (true)
                {
                    Process(nodes.Current);

                    if (nodes.MoveNext())
                    {
                        stringBuilder.Append(", ");
                    }
                    else
                    {
                        break;
                    }
                }

                stringBuilder.Append(" }");
            }
            else
            {
                stringBuilder.Append("null");
            }
        }

        protected override void ProcessStringLiteral(StringLiteral stringLiteral)
        {
            stringBuilder.Append(stringLiteral.Value);
        }

        protected override void ProcessListLiteral(ListLiteral listLiteral)
        {
            stringBuilder.Append("new List<object>()");

            if (listLiteral.Values.Count > 0)
            {
                stringBuilder.Append(" { ");

                IEnumerator<Node> nodes = listLiteral.Values.GetEnumerator();

                nodes.MoveNext();

                while (true)
                {
                    Process(nodes.Current);

                    if (nodes.MoveNext())
                    {
                        stringBuilder.Append(", ");
                    }
                    else
                    {
                        break;
                    }
                }

                stringBuilder.Append(" }");
            }
        }

        protected override void ProcessClass(Class @class)
        {
            stringBuilder.AppendLine("class " + @class.Name);
            ProcessNodeContainer(@class);
        }

        protected override void ProcessNull()
        {
            stringBuilder.Append("null");
        }

        protected override void ProcessProperty(Property property)
        {
            if(property.Values.Last() is FunctionCall)
            {
                MakeFunctionCallNonGeneric(property);
            }

            IEnumerator<Node> nodes = property.Values.GetEnumerator();
            nodes.MoveNext();

            while (true)
            {
                if (nodes.Current is Expression)
                {
                    stringBuilder.Append("(");
                    Process(nodes.Current);
                    stringBuilder.Append(")");
                }
                else
                {
                    Process(nodes.Current);
                }

                if (nodes.MoveNext())
                {
                    if(nodes.Current is ArrayAccessor == false)
                    {
                        stringBuilder.Append(".");
                    }
                }
                else
                {
                    break;
                }
            }
        }

        protected override void ProcessArrayAccessor(ArrayAccessor arrayAccessor)
        {
            stringBuilder.Append("[");
            Process(arrayAccessor.Indexer);
            stringBuilder.Append("]");
        }

        protected override void ProcessInterpolatedStringLiteral(InterpolatedStringLiteral interpolatedStringLiteral)
        {
            stringBuilder.Append("$");
            stringBuilder.Append(interpolatedStringLiteral.Value);
        }

        protected override void ProcessForEach(ForEach forEach)
        {
            stringBuilder.Append("foreach ");
            stringBuilder.Append('(');
            stringBuilder.Append("var ");
            stringBuilder.Append(forEach.VariableName);
            stringBuilder.Append(" in ");
            Process(forEach.Collection);
            stringBuilder.Append(')');
            stringBuilder.AppendLine();

            ProcessNodeContainer(forEach);
        }

        protected override void ProcessDictionaryLiteral(DictionaryLiteral dictionaryNode)
        {
            new Dictionary<string, string>
            {
                { "a", "" } 
            };

            stringBuilder.AppendLine("new Dictionary<object, object>");
            AddIndent();

            stringBuilder.AppendLine("{");
            
            Indent++;

            var values = dictionaryNode.Values.GetEnumerator();
            values.MoveNext();

            while (true)
            {
                AddIndent();
                stringBuilder.Append("{ ");

                Process(values.Current.Key);

                stringBuilder.Append(", ");

                if(values.Current.Value is DictionaryLiteral)
                {
                    Indent++;
                    Process(values.Current.Value);
                    Indent--;
                    stringBuilder.AppendLine();
                    AddIndent();
                }
                else
                {
                    Process(values.Current.Value);
                }
                

                stringBuilder.Append(" }");

                if (values.MoveNext())
                {
                    stringBuilder.AppendLine(", ");
                }
                else
                {
                    stringBuilder.AppendLine();
                    break;
                }
            }

            Indent--;
            AddIndent();
            stringBuilder.Append("}");
            NeedsNewLine = true;
        }
    }
}
