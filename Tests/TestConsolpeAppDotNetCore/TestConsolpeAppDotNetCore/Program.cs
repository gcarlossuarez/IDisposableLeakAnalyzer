using System;
using System.IO;
using System.Text;
using System.Xml;
using ReturnDisposable1;

namespace TestConsolpeAppDotNetCore
{
    internal class Program
    {
        static void Main(string[] args)
        {
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

        }
    }
}
