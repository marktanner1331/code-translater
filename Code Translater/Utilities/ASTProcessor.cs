using Code_Translater.AST;
using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.Utilities
{
    public abstract class ASTProcessor
    {
        protected virtual void Process(Node node)
        {
            switch (node)
            {
                case Root root:
                    ProcessRoot(root);
                    break;
                case Import import:
                    ProcessImport(import);
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
                case Comment comment:
                    ProcessComment(comment);
                    break;
                case BlankLine blankLine:
                    ProcessBlankLine();
                    break;
                case Number number:
                    ProcessNumber(number);
                    break;
                case While @while:
                    ProcessWhile(@while);
                    break;
                case If @if:
                    ProcessIf(@if);
                    break;
                case Break _:
                    ProcessBreak();
                    break;
                case BooleanLiteral booleanLiteral:
                    ProcessBooleanLiteral(booleanLiteral);
                    break;
                case MultipleAssignment multipleAssignment:
                    ProcessMultipleAssignment(multipleAssignment);
                    break;
                case TupleNode tupleNode:
                    ProcessTupleNode(tupleNode);
                    break;
                case StringLiteral stringLiteral:
                    ProcessStringLiteral(stringLiteral);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected abstract void ProcessStringLiteral(StringLiteral stringLiteral);
        protected abstract void ProcessTupleNode(TupleNode tupleNode);
        protected abstract void ProcessMultipleAssignment(MultipleAssignment multipleAssignment);
        protected abstract void ProcessBooleanLiteral(BooleanLiteral booleanLiteral);
        protected abstract void ProcessBreak();
        protected abstract void ProcessIf(If @if);
        protected abstract void ProcessWhile(While @while);
        protected abstract void ProcessNumber(Number number);
        protected abstract void ProcessBlankLine();
        protected abstract void ProcessComment(Comment comment);
        protected abstract void ProcessExpression(Expression expression);
        protected abstract void ProcessListComprehension(ListComprehension listComprehension);
        protected abstract void ProcessReturn(Return @return);
        protected abstract void ProcessVariable(Variable variable);
        protected abstract void ProcessFunctionCall(FunctionCall functionCall);
        protected abstract void ProcessFunction(Function function);
        protected abstract void ProcessAssignment(Assignment assignment);
        protected abstract void ProcessImport(Import import);
        protected abstract void ProcessRoot(Root root);
    }

    public abstract class ASTProcessor<T>
    {
        protected virtual T Process(Node node)
        {
            switch (node)
            {
                case Root root:
                    return ProcessRoot(root);
                case Import import:
                    return ProcessImport(import);
                case Function function:
                    return ProcessFunction(function);
                case Assignment assignment:
                    return ProcessAssignment(assignment);
                case FunctionCall functionCall:
                    return ProcessFunctionCall(functionCall);
                case Variable variable:
                    return ProcessVariable(variable);
                case Return _return:
                    return ProcessReturn(_return);
                case ListComprehension listComprehension:
                    return ProcessListComprehension(listComprehension);
                case Expression expression:
                    return ProcessExpression(expression);
                case Comment comment:
                    return ProcessComment(comment);
                case BlankLine _:
                    return ProcessBlankLine();
                case Number number:
                    return ProcessNumber(number);
                case While @while:
                    return ProcessWhile(@while);
                case If @if:
                    return ProcessIf(@if);
                case Break _:
                    return ProcessBreak();
                default:
                    throw new NotImplementedException();
            }
        }

        protected abstract T ProcessBreak();
        protected abstract T ProcessIf(If @if);
        protected abstract T ProcessWhile(While @while);
        protected abstract T ProcessNumber(Number number);
        protected abstract T ProcessBlankLine();
        protected abstract T ProcessComment(Comment comment);
        protected abstract T ProcessExpression(Expression expression);
        protected abstract T ProcessListComprehension(ListComprehension listComprehension);
        protected abstract T ProcessReturn(Return @return);
        protected abstract T ProcessVariable(Variable variable);
        protected abstract T ProcessFunctionCall(FunctionCall functionCall);
        protected abstract T ProcessFunction(Function function);
        protected abstract T ProcessAssignment(Assignment assignment);
        protected abstract T ProcessImport(Import import);
        protected abstract T ProcessRoot(Root root);
    }
}
