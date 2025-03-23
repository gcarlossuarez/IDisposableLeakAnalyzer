using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReturnDisposable1
{
    public class AnotherResource : IDisposable
    {
        private bool disposed = false;

        public AnotherResource()
        {
            Console.WriteLine("AnotherResource created.");
        }

        public void UsarOtroRecurso()
        {
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