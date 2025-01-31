using Code_TranslaterTests;
using System;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Tester.SerializeTest(Tester.Language.JAVASCRIPT, Tester.Language.CSHARP, 1);
        }
    }
}
