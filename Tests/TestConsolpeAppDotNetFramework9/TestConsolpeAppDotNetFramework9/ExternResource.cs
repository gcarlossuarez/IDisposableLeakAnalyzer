using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ReturnDisposable1
{
    using System;

    public class ExternResource : IDisposable
    {
        private bool disposed = false;

        public ExternResource()
        {
            Console.WriteLine("ExternResource Created.");
        }

        public ExternResource GetExternResource()
        {
            return this;
        }

        public void UseResourcde()
        {
            if (disposed)
                throw new ObjectDisposedException("ExternResource");

            Console.WriteLine("Using ExternResource...");

            string xmlStr = @"<libros>
  <libro>
    <titulo>Cien años de soledad</titulo>
    <autor>Gabriel García Márquez</autor>
  </libro>
  <libro>
    <titulo>1984</titulo>
    <autor>George Orwell</autor>
  </libro>
  <libro>
    <titulo>El señor de los anillos</titulo>
    <autor>J.R.R. Tolkien</autor>
  </libro>
</libros>";
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlStr);

            XmlNodeList libros = xmlDocument.SelectNodes("/libros/libro");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Liberar recursos administrados
                    Console.WriteLine("Releasing managed resources from ExternResource.");
                }

                // Liberar recursos no administrados
                Console.WriteLine("Releasing unmanaged resources from ExternResource.");

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExternResource()
        {
            Dispose(false);
        }
    }
}
