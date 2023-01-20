using Code_Translater.AST;
using Code_Translater.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code_Translater.Transformers
{
    public class MultiCleaner : ASTProcessor<IEnumerable<Node>>
    {
        public bool RemoveTripleQuotedStrings = false;

        public void Clean(Node root)
        {
            this.Process(root).Single();
        }

        private void ProcessNodeContainer(INodeContainer nodeContainer)
        {
            List<Node> newChildren = new List<Node>();
            foreach(Node child in nodeContainer.Children)
            {
                newChildren.AddRange(this.Process(child));
            }

            nodeContainer.Children = newChildren;
        }

        protected override IEnumerable<Node> ProcessAssignment(Assignment assignment)
        {
            assignment.RValue = this.Process(assignment.RValue).Single();

            yield return assignment;
        }

        protected override IEnumerable<Node> ProcessBlankLine()
        {
            yield return new BlankLine();
        }

        protected override IEnumerable<Node> ProcessBreak()
        {
            yield return new Break();
        }

        protected override IEnumerable<Node> ProcessClass(Class @class)
        {
            ProcessNodeContainer(@class);
            yield return @class;
        }

        protected override IEnumerable<Node> ProcessComment(Comment comment)
        {
            yield return comment;
        }

        protected override IEnumerable<Node> ProcessExpression(Expression expression)
        {
            List<Node> newCoefficients = new List<Node>();
            foreach(Node coefficient in expression.Coefficients)
            {
                newCoefficients.AddRange(this.Process(coefficient));
            }

            if(expression.Coefficients.Count != newCoefficients.Count)
            {
                throw new Exception();
            }

            expression.Coefficients = newCoefficients;

            yield return expression;
        }

        protected override IEnumerable<Node> ProcessFunction(Function function)
        {
            ProcessNodeContainer(function);
            yield return function;
        }

        protected override IEnumerable<Node> ProcessFunctionCall(FunctionCall functionCall)
        {
            foreach (FunctionParameter functionParameter in functionCall.Parameters)
            {
                functionParameter.Value = this.Process(functionParameter.Value).Single();
            }

            yield return functionCall;
        }

        protected override IEnumerable<Node> ProcessIf(If @if)
        {
            @if.Expression = this.Process(@if.Expression).Single();

            ProcessNodeContainer(@if);
            yield return @if;
        }

        protected override IEnumerable<Node> ProcessImport(Import import)
        {
            yield return import;
        }

        protected override IEnumerable<Node> ProcessListComprehension(ListComprehension listComprehension)
        {
            listComprehension.Collection = this.Process(listComprehension.Collection).Single();
            listComprehension.Expression = this.Process(listComprehension.Expression).Single();

            yield return listComprehension;
        }

        protected override IEnumerable<Node> ProcessListLiteral(ListLiteral literal)
        {
            List<Node> newValues = new List<Node>();
            foreach(Node value in literal.Values)
            {
                newValues.AddRange(this.Process(value));
            }

            if(literal.Values.Count != newValues.Count)
            {
                throw new Exception();
            }

            literal.Values = newValues;

            yield return literal;
        }

        protected override IEnumerable<Node> ProcessMultipleAssignment(MultipleAssignment multipleAssignment)
        {
            multipleAssignment.RValue = this.Process(multipleAssignment.RValue).Single();
            yield return multipleAssignment;
        }

        protected override IEnumerable<Node> ProcessNull()
        {
            yield return new Null();
        }

        protected override IEnumerable<Node> ProcessNumber(Number number)
        {
            yield return number;
        }

        protected override IEnumerable<Node> ProcessReturn(Return @return)
        {
            if(@return.Value != null)
            {
                @return.Value = this.Process(@return.Value).Single();
            }

            yield return @return;
        }

        protected override IEnumerable<Node> ProcessRoot(Root root)
        {
            ProcessNodeContainer(root);
            yield return root;
        }

        protected override IEnumerable<Node> ProcessStringLiteral(StringLiteral stringLiteral)
        {
            if(RemoveTripleQuotedStrings)
            {
                if(stringLiteral.Value.StartsWith("\"\"\""))
                {
                    stringLiteral.Value = stringLiteral.Value.Substring(2, stringLiteral.Value.Length - 4);
                    stringLiteral.Value = stringLiteral.Value.Replace("\r", "\\r").Replace("\n", "\\n");
                }
            }

            yield return stringLiteral;
        }

        protected override IEnumerable<Node> ProcessTuple(TupleNode tupleNode)
        {
            List<Node> newValues = new List<Node>();
            foreach (Node value in tupleNode.Values)
            {
                newValues.AddRange(this.Process(value));
            }

            if (tupleNode.Values.Count != newValues.Count)
            {
                throw new Exception();
            }

            tupleNode.Values = newValues;

            yield return tupleNode;
        }

        protected override IEnumerable<Node> ProcessVariable(Variable variable)
        {
            yield return variable;
        }

        protected override IEnumerable<Node> ProcessWhile(While @while)
        {
            @while.Expression = this.Process(@while.Expression).Single();

            ProcessNodeContainer(@while);
            yield return @while;
        }
    }
}
