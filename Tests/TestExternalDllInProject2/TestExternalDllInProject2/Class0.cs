using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExternalDllInProject2
{
    public class Class0
    {
        public Class0()
        {
            using (IDbCommand dbCommand = this.ComandoXId("ClaveId"))
            {

            }
            DataSet Prueba()
            {
                return new DataSet();
            }

            int dd = 0;
            if (dd == 1)
            {
                Prueba();
                Prueba();
            }
            using (var d = CreateDataset(""))
            {
                Console.WriteLine();
            }
        }

        protected IDbCommand ComandoXId(string id)
        {
            string[] clavesPuntoDeVenta = id.Split('_');
            SqlCommand sqlComm = new SqlCommand("SELECT * FROM PuntoDeVenta WHERE CuitId = @CuitId AND PuntoDeVentaId = @PuntoDeVentaId");
            sqlComm.Parameters.Add("@CuitId", SqlDbType.BigInt).Value = long.Parse(clavesPuntoDeVenta[0]);
            sqlComm.Parameters.Add("@PuntoDeVentaId", SqlDbType.Int).Value = Convert.ToInt32(clavesPuntoDeVenta[1]);
            return sqlComm;
        }

        private DataSet CreateDataset(string empty)
        {
            return new DataSet();
        }
    }
}
