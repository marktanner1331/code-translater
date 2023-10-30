using Code_Translater.AST;
using Code_Translater.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            string new_name = string.Join("", multipleAssignment.LValues.Select(x => (x.LValue as Variable).Name));
            yield return new Assignment
            {
                InlineComment = multipleAssignment.InlineComment,
                LValue = new Variable
                {
                    Name = new_name
                },
                RValue = multipleAssignment.RValue,
                Type = null
            };

            foreach(Assignment assignment in multipleAssignment.LValues)
            {
                yield return new Assignment
                {
                    InlineComment = null,
                    LValue = assignment.LValue,
                    RValue = new Variable
                    {
                        Name = new_name + "." + (assignment.LValue as Variable).Name
                    },
                    Type = assignment.Type
                };
            }
        }
    }
}
