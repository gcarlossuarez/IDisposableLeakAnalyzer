using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExternalDLLInProyect1
{
    public class Program
    {
        static void Main(string[] args)
        {
            Class1 class1 = new Class1();
            class1.TestSqlcommand1();
            Console.WriteLine("Pulse una tecla, para continuar...");
            Console.ReadKey();
        }
    }
}
