using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindClassesThatDoNotDisposeResource
{
    public class KeyResult : IEquatable<KeyResult>
    {
        public string Project { get; set; }
        public string File { get; set; }
        public int LineNumber { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }

        public KeyResult() { } // required for deserialization

        public KeyResult(string project, string file, int lineNumber, string @class, string method)
        {
            this.Project = project?.Trim();
            this.File = file?.Trim();
            this.LineNumber = lineNumber;
            this.Class = @class?.Trim();
            this.Method = method?.Trim();
        }

        // Implementación de IEquatable<KeyResult>
        public bool Equals(KeyResult other)
        {
            if (other == null)
                return false;

            return string.Equals(this.Project, other.Project, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(this.File, other.File, StringComparison.OrdinalIgnoreCase) &&
                   this.LineNumber == other.LineNumber &&
                   string.Equals(this.Class, other.Class, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(this.Method, other.Method, StringComparison.OrdinalIgnoreCase);
        }

        // Sobrescribir Equals para comparar con cualquier objeto
        public override bool Equals(object obj)
        {
            if (obj is KeyResult)
            {
                KeyResult other = (KeyResult)obj;
                return Equals(other);
            }

            return false;
        }

        // Implementación mejorada de GetHashCode
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (Project?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 31 + (File?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 31 + LineNumber.GetHashCode();
                hash = hash * 31 + (Class?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 31 + (Method?.ToLowerInvariant().GetHashCode() ?? 0);
                return hash;
            }
        }

        // Sobrecargar los operadores == y != para facilitar la comparación
        public static bool operator ==(KeyResult left, KeyResult right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(KeyResult left, KeyResult right)
        {
            return !(left == right);
        }
    }

}