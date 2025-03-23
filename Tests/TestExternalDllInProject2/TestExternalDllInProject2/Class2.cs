using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExternalDllInProject2
{
    public class Class2
    {
        public void DisposeDataSet(DataSet dataSet)
        {
            dataSet.Dispose();
        }
    }

}
