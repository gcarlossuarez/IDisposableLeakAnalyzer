using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReturnDisposable2
{
    public class Class1
    {
        protected SqlConnection Conexion = new SqlConnection("connString");

        private SqlTransaction tran;

        public void Save()
        {
            using (this.Conexion.BeginTransaction())
            {
                //this.Conexion.BeginTransaction();
            }

            using (new DataSet())
            {

            }
            using (tran = this.Conexion.BeginTransaction())
            {

            }
        }

    }
}
