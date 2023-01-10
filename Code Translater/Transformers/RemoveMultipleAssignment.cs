using Code_Translater.AST;
using Code_Translater.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Code_Translater.Transformers
{
    public class RemoveMultipleAssignment
    {
        public void RemoveMultipleAssignments(Node node)
        {
            this.Process(node);
        }

        private void Process(Node node)
        {
            if(node is INodeContainer nodeContainer)
            {
                List<Node> newChildren = new List<Node>();

                foreach(Node child in nodeContainer.Children)
                {
                    if(child is MultipleAssignment multipleAssignment)
                    {
                        newChildren.AddRange(RewriteMultipleAssignment(multipleAssignment));
                    }
                    else
                    {
                        newChildren.Add(child);
                    }
                    
                    Process(child);
                }

                nodeContainer.Children = newChildren;
            }
        }

        private IEnumerable<Node> RewriteMultipleAssignment(MultipleAssignment multipleAssignment)
        {
            string new_name = string.Join("", multipleAssignment.VariableNames);
            yield return new Assignment
            {
                InlineComment = multipleAssignment.InlineComment,
                Name = new_name,
                RValue = multipleAssignment.RValue,
                Type = multipleAssignment.Type
            };

            foreach(string variable in multipleAssignment.VariableNames)
            {
                yield return new Assignment
                {
                    InlineComment = null,
                    Name = variable,
                    RValue = new Variable
                    {
                        Name = new_name + "." + variable
                    },
                    Type = null
                };
            }
        }
    }
}
