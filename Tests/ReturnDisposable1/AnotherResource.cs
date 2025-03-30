using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReturnDisposable1
{
    public class Aux
    {
        public DataSet GetDataSet()
        {
            return new DataSet();
        }
    }
    public class AnotherResource : IDisposable
    {
        private bool disposed = false;

        public AnotherResource()
        {
            DataSet ds = null;
            try
            {
                //ds = new DataSet();
                Aux a = new Aux();
                ds = a.GetDataSet();
            }
            finally
            {
                ds?.Dispose();
            }
            //ds.Dispose();
            Console.WriteLine("AnotherResource created.");
        }

        public void UsarOtroRecurso()
        {
            DataSet ds = null;
            try
            {
                ds = new DataSet();
            }
            finally
            {
                ds?.Dispose();
            }
            //ds.Dispose();

            if (disposed)
                throw new ObjectDisposedException("AnotherResource");

            Console.WriteLine("Using AnotherResource...");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Liberar recursos administrados
                    Console.WriteLine("Releasing managed resources from AnotherResource.");
                }

                // Liberar recursos no administrados
                Console.WriteLine("Releasing unmanaged resources from AnotherResource.");

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AnotherResource()
        {
            Dispose(false);
        }
    }
}