using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public interface INodeContainer
    {
        List<Node> Children { get; }
    }
}
