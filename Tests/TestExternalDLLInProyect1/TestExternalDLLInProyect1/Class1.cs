using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExternalDLLInProyect1
{
    public class Class1
    {
        public DataSet DataSet => new DataSet();

        public void TestSqlcommand1()
        {
            DataSet.Tables["Cabecera"].NewRow();
            DataSet.Tables["Cabecera"].Columns.Add("Id");
            SqlCommand comm =
                   new SqlCommand(
                       "SELECT * FROM BOLFE_Compras WHERE CuitId = @CuitId AND PuntoDeVentaId = @PuntoDeVentaId AND CodigoSistema = @CodigoSistema");
        }
    }

}
