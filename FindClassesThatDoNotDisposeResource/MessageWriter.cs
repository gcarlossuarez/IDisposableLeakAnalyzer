    using System;
    using System.IO;

    namespace FindClassesThatDoNotDisposeResource
{
        public static class MessageWriter
        {
            /// <summary>
            /// Writes a message to the console and a CSV file with language support.
            /// </summary>
            public static void WriteMessage(
                StreamWriter csvWriter,
                string messageKey, // Console message key
                ConsoleColor? color, // Optional console color
                string csvStatusKey, // Message key for the "Status" field in the CSV
                string project, // Project name
                string filePath, // File where it occurs
                int lineNumber, // Line in the file
                string className, // Class name
                string methodSignature, // Method signature
                string extraInfo = "") // Optional CSV remarks
            {
                // Console message
                if (color.HasValue)
                {
                    Console.ForegroundColor = color.Value;
                }

                if (string.IsNullOrEmpty(extraInfo))
                {
                    Console.WriteLine(Messages.Get(messageKey, filePath, lineNumber, className, methodSignature));
                }
                else
                {
                    Console.WriteLine(Messages.Get(messageKey, filePath, lineNumber, className, methodSignature, extraInfo));
                }

                if (color.HasValue)
                {
                    Console.ResetColor();
                }

                // Message for CSV (with translated status)
                if (!string.IsNullOrEmpty(csvStatusKey))
                {
                    string status = Messages.Get(csvStatusKey);
                    string observations = string.IsNullOrEmpty(extraInfo) ? "" : extraInfo;

                    csvWriter.WriteLine($"{project}{Messages.CsvSeparator}{filePath}{Messages.CsvSeparator}{lineNumber}{Messages.CsvSeparator}{className}{Messages.CsvSeparator}{status}{Messages.CsvSeparator}{methodSignature}{Messages.CsvSeparator}{observations}");
                }
            }
        }
    }

