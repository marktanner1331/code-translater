using Code_TranslaterTests;
using System;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Tester.SerializeTest(Tester.Language.PYTHON, Tester.Language.CSHARP, 10);

            //for (int i = 1; i <= 8; i++)
            //{
            //    Tester.SerializeTest(Tester.Language.PYTHON, Tester.Language.CSHARP, i);
            //}
        }
    }
}
