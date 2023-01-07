using Code_Translater.AST;
using System;
using System.Collections.Generic;
using System.Text;

namespace Code_Translater.Serializers
{
    public interface ISerializer
    {
        public string Serialize(Node root);
    }
}
