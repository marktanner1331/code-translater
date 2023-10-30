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
            if(assignment.LValue is Variable variable)
            {
                if (UnresolvedTypes.ContainsKey(variable.Name))
                {
                    //throw new NotImplementedException();
                }
                else
                {
                    Scope.Peek().Add(variable.Name, assignment);
                }
            }

            string type = Process(assignment.RValue);

            if (type != null && assignment.Type == null)
            {
                assignment.Type = type;
            }
            else
            {
                if (assignment.LValue is Variable variable2 && UnresolvedTypes.ContainsKey(variable2.Name) == false)
                {
                    UnresolvedTypes.Add(variable2.Name, assignment);
                }
            }

            return type;
        }

        protected override string ProcessComment(Comment comment)
        {
            return null;
        }

        protected override string ProcessListLiteral(ListLiteral listLiteral)
        {
            foreach (Node value in listLiteral.Values)
            {
                Process(value);
            }

            return "List<Object>";
        }

        protected override string ProcessExpression(Expression expression)
        {
            foreach (Node coefficient in expression.Coefficients)
            {
                if (coefficient is Variable variable && UnresolvedTypes.ContainsKey(variable.Name))
                {
                    UnresolvedTypes[variable.Name].Type = "float";
                    UnresolvedTypes.Remove(variable.Name);
                }
                else
                {
                    Process(coefficient);
                }
            }

            return "float";
        }

        protected override string ProcessClass(Class @class)
        {
            Scope.Push(new Dictionary<string, IHasType>());

            foreach (var node in @class.Children)
            {
                Process(node);
            }

            Scope.Pop();

            return null;
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
            //for (int i = 0; i < functionCall.Parameters.Count; i++)
            //{
            //    var param = functionCall.Parameters[i];

            //    if (param.Value is Variable variable && UnresolvedTypes.ContainsKey(variable.Name))
            //    {
            //        IHasType Unresolved = UnresolvedTypes[variable.Name];

            //        if (PackageMapper.TryGetTypeForParameter(functionCall.PackageName, functionCall.FunctionName, i, out string returnType))
            //        {
            //            Unresolved.Type = returnType;
            //        }
            //    }
            //}

            //if (PackageMapper.TryGetReturnType(functionCall.PackageName, functionCall.FunctionName, out string type))
            //{
            //    return type;
            //}

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
            if (CurrentFunction != null && CurrentFunction.ReturnType == null)
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

        protected override string ProcessTuple(TupleNode tupleNode)
        {
            foreach (Node value in tupleNode.Values)
            {
                Process(value);
            }

            return null;
        }

        protected override string ProcessStringLiteral(StringLiteral stringLiteral)
        {
            return "string";
        }

        protected override string ProcessMultipleAssignment(MultipleAssignment multipleAssignment)
        {
            foreach(var lValue in multipleAssignment.LValues)
            {
                if(lValue.LValue is Variable variable)
                {
                    if (Scope.Peek().ContainsKey(variable.Name) == false && UnresolvedTypes.ContainsKey(variable.Name) == false)
                    {
                        Scope.Peek().Add(variable.Name, lValue);
                        UnresolvedTypes.Add(variable.Name, lValue);
                    }
                }
            }

            Process(multipleAssignment.RValue);
            return null;
        }

        protected override string ProcessNull()
        {
            return null;
        }

        protected override string ProcessProperty(Property property)
        {
            if(property.IsVariablePlusFunctionCall(out string variable, out FunctionCall functionCall))
            {
                for (int i = 0; i < functionCall.Parameters.Count; i++)
                {
                    var param = functionCall.Parameters[i];

                    if (param.Value is Variable parameter && UnresolvedTypes.ContainsKey(parameter.Name))
                    {
                        IHasType Unresolved = UnresolvedTypes[parameter.Name];

                        if (PackageMapper.TryGetTypeForParameter(variable, functionCall.FunctionName, i, out string returnType))
                        {
                            Unresolved.Type = returnType;
                        }
                    }
                }

                if (PackageMapper.TryGetReturnType(variable, functionCall.FunctionName, out string type))
                {
                    return type;
                }
            }



            return null;
        }

        protected override string ProcessArrayAccessor(ArrayAccessor arrayAccessor)
        {
            return null;
        }

        protected override string ProcessBooleanLiteral(BooleanLiteral booleanLiteral)
        {
            return "bool";
        }

        protected override string ProcessInterpolatedStringLiteral(InterpolatedStringLiteral interpolatedStringLiteral)
        {
            return "string";
        }

        protected override string ProcessForEach(ForEach forEach)
        {
            return null;
        }

        protected override string ProcessDictionaryNode(DictionaryLiteral dictionaryNode)
        {
            return "Dictionary<object, object>";
        }
    }
}
