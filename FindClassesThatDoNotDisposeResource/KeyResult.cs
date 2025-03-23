using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindClassesThatDoNotDisposeResource
{
    public class KeyResult : IEquatable<KeyResult>
    {
        public string Proyecto { get; set; }
        public string Archivo { get; set; }
        public int Línea { get; set; }
        public string Clase { get; set; }
        public string Método { get; set; }

        public KeyResult(string proyecto, string archivo, int línea, string clase, string método)
        {
            this.Proyecto = proyecto?.Trim();
            this.Archivo = archivo?.Trim();
            this.Línea = línea;
            this.Clase = clase?.Trim();
            this.Método = método?.Trim();
        }

        // Implementación de IEquatable<KeyResult>
        public bool Equals(KeyResult other)
        {
            if (other == null)
                return false;

            return string.Equals(this.Proyecto, other.Proyecto, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(this.Archivo, other.Archivo, StringComparison.OrdinalIgnoreCase) &&
                   this.Línea == other.Línea &&
                   string.Equals(this.Clase, other.Clase, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(this.Método, other.Método, StringComparison.OrdinalIgnoreCase);
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
                hash = hash * 31 + (Proyecto?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 31 + (Archivo?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 31 + Línea.GetHashCode();
                hash = hash * 31 + (Clase?.ToLowerInvariant().GetHashCode() ?? 0);
                hash = hash * 31 + (Método?.ToLowerInvariant().GetHashCode() ?? 0);
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