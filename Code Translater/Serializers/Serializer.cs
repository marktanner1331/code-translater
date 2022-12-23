using Code_Translater.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code_Translater.Serializers
{
    public abstract class Serializer
    {
        private StringBuilder stringBuilder;

        private int Indent = 0;
        private bool IsNewLine = true;

        private Stack<List<string>> ScopedVariabies = new Stack<List<string>>();

        public string Serialize(Node root)
        {
            stringBuilder = new StringBuilder();

            ProcessNode(root);

            return stringBuilder.ToString();
        }

        private bool HasVariableInScope(string name)
        {
            return ScopedVariabies.SelectMany(x => x).Any(x => x == name);
        }

        private void ProcessNode(Node node)
        {
            if (IsNewLine)
            {
                AddIndent();
                IsNewLine = false;
            }

            switch (node)
            {
                case Root root:
                    ProcessRoot(root);
                    break;
                case Import import:
                    //we will build imports manually
                    break;
                case Function function:
                    ProcessFunction(function);
                    break;
                case Assignment assignment:
                    ProcessAssignment(assignment);
                    break;
                case FunctionCall functionCall:
                    ProcessFunctionCall(functionCall);
                    break;
                case BinaryExpression binaryExpression:
                    ProcessBinaryExpression(binaryExpression);
                    break;
                case Variable variable:
                    ProcessVariable(variable);
                    break;
                case Return _return:
                    ProcessReturn(_return);
                    break;
                case ListComprehension listComprehension:
                    ProcessListComprehension(listComprehension);
                    break;
                case Expression expression:
                    ProcessExpression(expression);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void AddIndent()
        {
            stringBuilder.Append(new String('\t', Indent));
        }

        private void ProcessExpression(Expression expression)
        {
            void processCoefficient(Node node)
            {
                if (node is Expression || node is BinaryExpression)
                {
                    stringBuilder.Append("(");
                    ProcessNode(node);
                    stringBuilder.Append(")");
                }
                else
                {
                    ProcessNode(node);
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

        private void ProcessListComprehension(ListComprehension listComprehension)
        {
            ProcessNode(listComprehension.Collection);
            stringBuilder.Append(".Select(");
            stringBuilder.Append(listComprehension.VariableName);
            stringBuilder.Append(" => ");
            ProcessNode(listComprehension.Expression);
            stringBuilder.Append(")");
        }

        private void ProcessReturn(Return @return)
        {
            stringBuilder.Append("return");

            if (@return.Value != null)
            {
                stringBuilder.Append(" ");
                ProcessNode(@return.Value);
            }

            stringBuilder.AppendLine(";");
            IsNewLine = true;
        }

        private void ProcessVariable(Variable variable)
        {
            stringBuilder.Append(variable.Name);
        }

        private void ProcessBinaryExpression(BinaryExpression binaryExpression)
        {
            ProcessNode(binaryExpression.Left);

            stringBuilder.Append(' ');
            stringBuilder.Append(binaryExpression.Operator);
            stringBuilder.Append(' ');

            ProcessNode(binaryExpression.Right);
        }

        private void MakeFunctionCallNonGeneric(FunctionCall functionCall)
        {
            switch (functionCall.PackageName)
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
            }
        }

        private void ProcessFunctionCall(FunctionCall functionCall)
        {
            MakeFunctionCallNonGeneric(functionCall);

            if (string.IsNullOrEmpty(functionCall.PackageName) == false)
            {
                stringBuilder.Append(functionCall.PackageName + ".");
            }

            stringBuilder.Append(functionCall.FunctionName);

            stringBuilder.Append('(');

            List<string> paramsString = new List<string>();
            foreach (FunctionCallParameter param in functionCall.Parameters)
            {
                paramsString.Add(param.Value);
            }

            stringBuilder.Append(string.Join(", ", paramsString));

            stringBuilder.Append(')');
        }

        private void ProcessAssignment(Assignment assignment)
        {
            if (HasVariableInScope(assignment.Name) == false)
            {
                stringBuilder.Append(assignment.Type);
                stringBuilder.Append(" ");
                ScopedVariabies.Peek().Add(assignment.Name);
            }

            stringBuilder.Append(assignment.Name);

            stringBuilder.Append(" = ");

            ProcessNode(assignment.RValue);

            stringBuilder.AppendLine(";");
            IsNewLine = true;
        }

        protected abstract string FormatFunctionSignature(Function function);

        private void ProcessFunction(Function function)
        {
            ScopedVariabies.Push(new List<string>());
            stringBuilder.AppendLine(FormatFunctionSignature(function));

            AddIndent();
            stringBuilder.AppendLine("{");
            IsNewLine = true;

            Indent++;

            foreach (Node node in function.Children)
            {
                ProcessNode(node);
            }

            stringBuilder.AppendLine("}");
            IsNewLine = true;
            Indent--;

            ScopedVariabies.Pop();
        }

        private void ProcessRoot(Root root)
        {
            foreach (Node node in root.Children)
            {
                ProcessNode(node);
            }
        }
    }
}
