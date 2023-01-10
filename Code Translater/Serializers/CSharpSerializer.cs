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

        public string Serialize(Node root)
        {
            new AddTypesTransformer().AddTypes(root);
            new RemoveMultipleAssignment().RemoveMultipleAssignments(root);
            new RemoveImports().Remove(root);

            stringBuilder = new StringBuilder();

            ScopedVariabies = new Stack<List<string>>();

            //outer 'file-level' scope
            ScopedVariabies.Push(new List<string>());

            Process(root);

            return stringBuilder.ToString();
        }

        private bool HasVariableInScope(string name)
        {
            return ScopedVariabies.SelectMany(x => x).Any(x => x == name);
        }

        protected override void Process(Node node)
        {
            if(NeedsNewLine)
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
                if (node is Expression || node is BinaryExpression)
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

            while(coefficients.MoveNext() && operators.MoveNext())
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

            if(@return.Value != null)
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

        protected override void ProcessBinaryExpression(BinaryExpression binaryExpression)
        {
            Process(binaryExpression.Left);

            stringBuilder.Append(' ');
            stringBuilder.Append(binaryExpression.Operator);
            stringBuilder.Append(' ');

            Process(binaryExpression.Right);
        }

        private void MakeFunctionCallNonGeneric(FunctionCall functionCall)
        {
            switch(functionCall.PackageName)
            {
                case "Enumerable":
                    switch(functionCall.FunctionName)
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
                    switch(functionCall.FunctionName)
                    {
                        case "int":
                            if(functionCall.Parameters.First().Type == null)
                            {
                                //assume it's a string
                                functionCall.PackageName = "Int";
                                functionCall.FunctionName = "Parse";
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                            break;
                    }
                    break;
            }
        }

        protected override void ProcessFunctionCall(FunctionCall functionCall)
        {
            MakeFunctionCallNonGeneric(functionCall);

            if(string.IsNullOrEmpty(functionCall.PackageName) == false)
            {
                stringBuilder.Append(functionCall.PackageName + ".");
            }

            stringBuilder.Append(functionCall.FunctionName);

            stringBuilder.Append('(');

            IEnumerator<FunctionParameter> parameters = functionCall.Parameters.GetEnumerator();

            if (parameters.MoveNext())
            {
                while (true)
                {
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
            if(HasVariableInScope(assignment.Name) == false)
            {
                stringBuilder.Append(assignment.Type ?? "var");
                stringBuilder.Append(" ");
                ScopedVariabies.Peek().Add(assignment.Name);
            }

            stringBuilder.Append(assignment.Name);

            stringBuilder.Append(" = ");

            Process(assignment.RValue);

            NeedsNewLine = true;
        }

        protected override void ProcessFunction(Function function)
        {            
            stringBuilder.Append("static ");
            stringBuilder.Append(function.ReturnType);
            stringBuilder.Append(" ");
            stringBuilder.Append(function.Name);
            stringBuilder.Append('(');

            List<string> paramsString = new List<string>();
            foreach(FunctionParameter param in function.Parameters)
            {
                if(param.Type == null)
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
            if(nodeContainer is Root == false)
            {
                AddIndent();

                stringBuilder.Append("{");
                NeedsNewLine = true;

                Indent++;
            }

            ScopedVariabies.Push(new List<string>());

            foreach (Node node in nodeContainer.Children)
            {
                Process(node);

                if(node is INodeContainer == false && node is Comment == false && node is BlankLine == false)
                {
                    stringBuilder.Append(";");
                }

                NeedsNewLine = true;
            }

            ScopedVariabies.Pop();

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
        }

        protected override void ProcessStringLiteral(StringLiteral stringLiteral)
        {
            stringBuilder.Append(stringLiteral.Value);
        }

        protected override void ProcessEquality(Equality equality)
        {
            Process(equality.Left);

            stringBuilder.Append(' ');
            stringBuilder.Append(equality.Operator);
            stringBuilder.Append(' ');

            Process(equality.Right);
        }
    }
}
