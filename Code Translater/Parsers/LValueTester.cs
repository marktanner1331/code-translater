using System.Linq;
using Code_Translater.AST;

namespace Code_Translater.Parsers
{
    public class LValueTester
    {
        public bool IsLValue(Node node)
        {
            if(node is Variable)
            {
                return true;
            }
            else if(node is Property property)
            {
                if(property.Values.Last() is Variable || property.Values.Last() is ArrayAccessor)
                {
                    return true;
                }
            }

            return false;
        }
    }
}