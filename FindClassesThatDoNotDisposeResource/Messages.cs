using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindClassesThatDoNotDisposeResource
{
    public static class Messages
    {
        public static Language CurrentLanguage { get; set; } = Language.Spanish;

        public static string CsvSeparator = ";";

        private static readonly Dictionary<string, MultilangMessage> Dictionary =
            new Dictionary<string, MultilangMessage>
            {
                // Console messages
                { "DisposableClassFound", new MultilangMessage("🔹 Clase IDisposable encontrada: {0} en {1}", "🔹 IDisposable class found: {0} in {1}") },
                { "DotNetDisposableClassWarning", new MultilangMessage("⚠️ [Clase Nativa .NET no definida en el proyecto] en {0}, línea {1}: {2} implementa IDisposable y podría requerir Dispose().", "⚠️ [Native .NET class not defined in the project] in {0}, line {1}: {2} implements IDisposable and may require Dispose().") },
                { "PotentialLeak", new MultilangMessage("⚠️ [POTENCIAL FUGA] en {0}, línea {1}: Se invoca {2} que retorna IDisposable sin ser dispuesto posteriormente.", "⚠️ [POTENTIAL LEAK] in {0}, line {1}: {2} returns IDisposable and is not disposed later.") },
                { "MemoryLeak", new MultilangMessage("❌ [FUGA DE MEMORIA] en {0}, línea {1}: Instancia de {2} sin 'using' ni 'Dispose()' en método {3}.", "❌ [MEMORY LEAK] in {0}, line {1}: Instance of {2} without 'using' or 'Dispose()' in method {3}.") },
                { "DisposeFoundInMember", new MultilangMessage("⚠️ [ADVERTENCIA DE POSIBLE FUGA DE MEMORIA, DISPOSE ENCONTRADO EN MIEMBRO {0}] en {1}, línea {2}: Instancia de {3} sin 'using' ni 'Dispose()' en método {4}.", "⚠️ [POSSIBLE MEMORY LEAK WARNING, DISPOSE FOUND IN MEMBER {0}] in {1}, line {2}: Instance of {3} without 'using' or 'Dispose()' in method {4}.") },
                { "ProjectAnalysis", new MultilangMessage("📌 Analizando Proyecto: {0}", "📌 Analyzing Project: {0}") },
                { "AnalysisComplete", new MultilangMessage("✅ Análisis completado. Revisa el archivo '{0}' para más detalles.", "✅ Analysis complete. Check the file '{0}' for more details.") },
                { "PressAnyKeyToContinue", new MultilangMessage("Pulse una tecla, para continuar...", "Press any key to continue...")},

                // CSV status messages
                { "Csv_Header", new MultilangMessage($"Proyecto{CsvSeparator}Archivo{CsvSeparator}Línea{CsvSeparator}Clase{CsvSeparator}Estado;Método;Observaciones", $"Project{CsvSeparator}File{CsvSeparator}Line{CsvSeparator}Class{CsvSeparator}Status{CsvSeparator}Method{CsvSeparator}Remarks") },
                { "Csv_PotentialLeak", new MultilangMessage("⚠️ POTENCIAL FUGA", "⚠️ POTENTIAL LEAK") },
                { "Csv_MemoryLeak", new MultilangMessage("❌ FUGA DE MEMORIA", "❌ MEMORY LEAK") },
                { "Csv_DisposeInOtherMember", new MultilangMessage("⚠️ DISPOSE EN OTRO MIEMBRO", "⚠️ DISPOSE IN OTHER MEMBER") },
                { "Csv_Correct", new MultilangMessage("✅ Correcto", "✅ Correct") },
                { "ExtraInfoDisposeFoundInMember", new MultilangMessage("DISPOSE ENCONTRADO EN", "DISPOSE FOUND IN")}
            };

        public static string Get(string key, params object[] args)
        {
            MultilangMessage message;
            if (Dictionary.TryGetValue(key, out message))
            {
                var format = CurrentLanguage == Language.Spanish ? message.Spanish : message.English;
                return string.Format(format, args);
            }
            return key;
        }
    }
}

