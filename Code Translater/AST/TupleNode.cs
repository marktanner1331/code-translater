using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.AST
{
    public class TupleNode : Node
    {
        public List<Node> Values = new List<Node> ();
    }
}
