using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExternalDllInProject2
{
    public class Class1
    {
        public DataSet DataSet => new DataSet();

        //public DataSet _dataSet => new DataSet();
        public DataSet _dataSet;

        public Class2 Class2 = new Class2();

        public void TestDisposeDataSet()
        {
            Class2.DisposeDataSet(_dataSet);
            Console.WriteLine();
        }

        public void TestSqlcommand1()
        {
            _dataSet = DataSet;
            DataSet.Tables.Add("Cabecera");
            DataSet.Tables["Cabecera"].NewRow();
            DataSet.Tables["Cabecera"].Columns.Add("Id");
            SqlCommand comm =
                new SqlCommand(
                    "SELECT * FROM BOLFE_Compras WHERE CuitId = @CuitId AND PuntoDeVentaId = @PuntoDeVentaId AND CodigoSistema = @CodigoSistema");
        }

        

        public void DisposeDataSet()
        {
            DataSet.Dispose();
            _dataSet.Dispose();
        }
    }

}
