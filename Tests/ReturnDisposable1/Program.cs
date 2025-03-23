using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ReturnDisposable1
{
    class Program
    {
        static void Main(string[] args)
        {
            //using (var manager = new ResourceManager())
            //{
            //    manager.UsingResource();
            //}

            //using (var manager = new ResourceManager())
            //{
            //    var x = new OtherResource();
            //}

            XmlWriterSettings xwSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "\t",
                ConformanceLevel = ConformanceLevel.Auto,
                NewLineHandling = NewLineHandling.None
            };

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter xw = XmlWriter.Create(ms, xwSettings))
                {

                }
            }

            var manager = new ResourceManager();
            using (manager)
            {
                var x = new AnotherResource();
            }

            using (var resourceManager = new ResourceManager())
            {
                using (var anotherResource = resourceManager.GetAnotherResource())
                {
                    var x = new AnotherResource();
                }
            }

            //using (var anotherResource = new ResourceManager().GetAnotherResource())
            //{
            //    var x = new AnotherResource();
            //}

            //var anotherResource = new ResourceManager().GetAnotherResource();
            //var x = new AnotherResource();
            //anotherResource.Dispose();

            Console.WriteLine("Recursos liberados y programa terminado.");
            Console.WriteLine("Pulse una tecla, para continuar...");
            Console.ReadKey();
        }
    }
}
