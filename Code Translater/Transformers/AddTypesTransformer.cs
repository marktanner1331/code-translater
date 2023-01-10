using Code_Translater.AST;
using Code_Translater.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code_Translater.Transformers
{
    public class AddTypesTransformer : ASTProcessor<string>
    {
        private Dictionary<string, IHasType> UnresolvedTypes = new Dictionary<string, IHasType>();
        private Stack<Dictionary<string, IHasType>> Scope = new Stack<Dictionary<string, IHasType>>();
        private PackageMapper PackageMapper = new PackageMapper();
        private Function CurrentFunction;

        public void AddTypes(Node node)
        {
            //outer 'file-level' scope
            Scope.Push(new Dictionary<string, IHasType>());
            this.Process(node);
        }

        protected override string ProcessAssignment(Assignment assignment)
        {
            if (UnresolvedTypes.ContainsKey(assignment.Name))
            {
                throw new NotImplementedException();
            }

            Scope.Peek().Add(assignment.Name, assignment);

            string type = Process(assignment.RValue);

            if (type != null && assignment.Type == null)
            {
                assignment.Type = type;
            }
            else
            {
                UnresolvedTypes.Add(assignment.Name, assignment);
            }

            return type;
        }

        protected override string ProcessComment(Comment comment)
        {
            return null;
        }

        protected override string ProcessBinaryExpression(BinaryExpression binaryExpression)
        {
            void processOperand(Node node)
            {
                if (node is Variable variable && UnresolvedTypes.ContainsKey(variable.Name))
                {
                    UnresolvedTypes[variable.Name].Type = "float";
                    UnresolvedTypes.Remove(variable.Name);
                }
                else
                {
                    Process(node);
                }
            }

            processOperand(binaryExpression.Left);
            processOperand(binaryExpression.Right);

            return "float";
        }

        protected override string ProcessExpression(Expression expression)
        {
            return "float";
        }

        protected override string ProcessFunction(Function function)
        {
            Scope.Push(new Dictionary<string, IHasType>());

            foreach (var param in function.Parameters)
            {
                Scope.Peek().Add(param.Name, param);

                if (param.Type == null)
                {
                    UnresolvedTypes.Add(param.Name, param);
                }
            }

            CurrentFunction = function;
            foreach (var node in function.Children)
            {
                Process(node);
            }
            CurrentFunction = null;

            if (Scope.Peek().Values.Any(x => x.Type == null))
            {
                //throw new Exception();
            }

            Scope.Pop();

            return null;
        }

        protected override string ProcessFunctionCall(FunctionCall functionCall)
        {
            for (int i = 0; i < functionCall.Parameters.Count; i++)
            {
                var param = functionCall.Parameters[i];

                if (param.Value is Variable variable && UnresolvedTypes.ContainsKey(variable.Name))
                {
                    IHasType Unresolved = UnresolvedTypes[variable.Name];

                    if (PackageMapper.TryGetTypeForParameter(functionCall.PackageName, functionCall.FunctionName, i, out string returnType))
                    {
                        Unresolved.Type = returnType;
                    }
                }
            }

            if(PackageMapper.TryGetReturnType(functionCall.PackageName, functionCall.FunctionName, out string type))
            {
                return type;
            }

            return null;
        }

        protected override string ProcessImport(Import import)
        {
            return null;
        }

        protected override string ProcessListComprehension(ListComprehension listComprehension)
        {
            string expressionType = Process(listComprehension.Expression);
            return "IEnumerable<" + expressionType + ">";
        }

        protected override string ProcessReturn(Return @return)
        {
            string type = Process(@return.Value);
            if(CurrentFunction != null && CurrentFunction.ReturnType == null)
            {
                CurrentFunction.ReturnType = type;
            }

            return null;
        }

        protected override string ProcessRoot(Root root)
        {
            foreach (var node in root.Children)
            {
                Process(node);
            }

            return null;
        }

        protected override string ProcessVariable(Variable variable)
        {
            return null;
        }

        protected override string ProcessBlankLine()
        {
            return null;
        }

        protected override string ProcessNumber(Number number)
        {
            return "float";
        }

        protected override string ProcessBreak()
        {
            return null;
        }

        protected override string ProcessIf(If @if)
        {
            return null;
        }

        protected override string ProcessWhile(While @while)
        {
            return null;
        }
    }
}
