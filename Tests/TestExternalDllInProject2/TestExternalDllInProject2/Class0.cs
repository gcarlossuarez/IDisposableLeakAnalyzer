using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExternalDllInProject2
{
    public class Class0
    {
        public Class0()
        {
            using (var d = CreateDataset(""))
            {
                Console.WriteLine();
            }
        }

        private DataSet CreateDataset(string empty)
        {
            return new DataSet();
        }
    }
}
