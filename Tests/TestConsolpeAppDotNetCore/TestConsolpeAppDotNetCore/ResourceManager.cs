using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReturnDisposable1
{
    public class ResourceManager : IDisposable
    {
        private ExternResource externResource;
        private AnotherResource otherResource;
        private bool disposed = false;

        public bool IsCreatedWithMethodInAnotherClass
        {
            get
            {
                using (ExternResource resource = externResource.GetExternResource())
                {
                    Console.WriteLine("ExternalResource created.");
                    return true;
                }
            }
        }

        public bool IsCreatedWithUsing
        {
            get
            {
                using (ExternResource resource = new ExternResource())
                {
                    Console.WriteLine("ExternalResource created.");
                    return true;
                }
            }
        }

        public bool IsCreated
        {
            get
            {
                ExternResource resource = new ExternResource();
                Console.WriteLine("ExternalResource created.");
                resource.Dispose();
                return true;
            }
        } 

        public ResourceManager()
        {
            ExternResource externalResource = new ExternResource();
            externResource = new ExternResource();
            otherResource = new AnotherResource();
            using (externalResource)
            {
                Console.WriteLine("Object 'recursoExternoLiberado', is going to be free.");
                externResource.Dispose();
            }
        }

        public void UsingResource()
        {
            if (disposed)
                throw new ObjectDisposedException("ResourceManager");

            externResource.UseResourcde();
            otherResource.UsarOtroRecurso();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Release managed resources
                    externResource?.Dispose();
                    otherResource?.Dispose();
                    Console.WriteLine("ManagedResources resources released.");
                }

                // Release unmanaged resources
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ResourceManager()
        {
            Dispose(false);
        }

        public AnotherResource GetAnotherResource()
        {
            return new AnotherResource();
        }

        public AnotherResource GetAnotherResource2()
        {
            var resource = new AnotherResource();
            return resource;
        }
    }
}
