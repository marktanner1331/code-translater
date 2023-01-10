using Code_Translater.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code_Translater.Transformers
{
    public class RemoveImports
    {
        public void Remove(Node node)
        {
            if(node is Root root == false)
            {
                return;
            }

            while(root.Children.First() is Import)
            {
                root.Children.RemoveAt(0);
            }
        }
    }
}
