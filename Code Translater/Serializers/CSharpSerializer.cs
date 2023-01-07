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
            AddTypesTransformer addTypesTransformer = new AddTypesTransformer();
            addTypesTransformer.AddTypes(root);

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

            stringBuilder.Append(";");
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
            parameters.MoveNext();

            while(true)
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

            stringBuilder.Append(";");
            NeedsNewLine = true;
        }

        protected override void ProcessFunction(Function function)
        {
            ScopedVariabies.Push(new List<string>());
            
            stringBuilder.Append("static ");
            stringBuilder.Append(function.ReturnType);
            stringBuilder.Append(" ");
            stringBuilder.Append(function.Name);
            stringBuilder.Append('(');

            List<string> paramsString = new List<string>();
            foreach(FunctionParameter param in function.Parameters)
            {
                ScopedVariabies.Peek().Add(param.Name);
                paramsString.Add(param.Type + " " + param.Name);
            }

            stringBuilder.Append(string.Join(", ", paramsString));

            stringBuilder.Append(')');
            stringBuilder.AppendLine();
            AddIndent();

            stringBuilder.Append("{");
            NeedsNewLine = true;

            Indent++;

            foreach (Node node in function.Children)
            {
                Process(node);
            }

            stringBuilder.Append("}");
            NeedsNewLine = true;
            Indent--;

            ScopedVariabies.Pop();
        }

        protected override void ProcessRoot(Root root)
        {
            foreach(Node node in root.Children)
            {
                Process(node);
            }
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
    }
}
