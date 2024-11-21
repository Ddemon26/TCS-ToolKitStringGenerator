using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TCS.UIToolKitUtils {
    internal static class GeneratorUtils {
        const string FILEPATH = "Assets/UI Toolkit/StringLibrary/";

        /// <summary>
        /// Saves the generated static class to a file.
        /// </summary>
        /// <param name="namespaceName">The namespace for the generated class.</param>
        /// <param name="className">The name of the generated class.</param>
        /// <param name="nameValuePairs">A dictionary mapping variable names to string values.</param>
        /// <param name="filePath">The file path where the class file will be saved. Defaults to FILEPATH.</param>
        /// <param name="length">The maximum length of the variable name before it gets abbreviated.</param>
        /// <exception cref="ArgumentException">Thrown when the namespace or class name is null or whitespace.</exception>
        /// <seealso cref="GenerateStaticClass"/>
        public static void SaveToFile(string namespaceName, string className, Dictionary<string, string> nameValuePairs, string filePath = FILEPATH, int length = 25) {
            if (string.IsNullOrWhiteSpace(namespaceName) || string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Namespace and class name must be provided.");

            // Generate the static class code as a string
            string classCode = GenerateStaticClass(namespaceName, className, nameValuePairs, length);

            // Combine the file path and class name to get the full file path
            string fullFilePath = Path.Combine(filePath, $"{className}.cs");
            string directoryPath = Path.GetDirectoryName(fullFilePath);

            // Create the directory if it does not exist
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            // Write the generated class code to the file
            File.WriteAllText(fullFilePath, classCode);
        }

        /// <summary>
        /// Generates a static class definition as a string, containing constant string fields
        /// based on the provided dictionary of variable names and string values.
        /// </summary>
        /// <param name="namespaceName">The namespace for the generated class.</param>
        /// <param name="className">The name of the generated class.</param>
        /// <param name="nameValuePairs">A dictionary mapping variable names to string values.</param>
        /// <param name="length">The maximum length of the variable name before it gets abbreviated.</param>
        /// <returns>A string containing the generated static class definition.</returns>
        static string GenerateStaticClass(string namespaceName, string className, Dictionary<string, string> nameValuePairs, int length = 25) {
            int estimatedSize = (nameValuePairs.Count * 50) + 200;
            var classBuilder = new StringBuilder(estimatedSize);

            classBuilder.AppendLine("// This file was generated using TCS Generation");
            classBuilder.AppendLine($"namespace {namespaceName}");
            classBuilder.AppendLine("{");
            classBuilder.AppendLine($"    public static class {className}");
            classBuilder.AppendLine("    {");

            const string fieldTemplate = "        public const string {0} = \"{1}\";";

            foreach (KeyValuePair<string, string> kvp in nameValuePairs) {
                string variableName = kvp.Key;
                string stringValue = kvp.Value;

                if (string.IsNullOrWhiteSpace(variableName) || string.IsNullOrWhiteSpace(stringValue)) continue;

                // Abbreviate the variable name if it exceeds the maximum length
                variableName = variableName.AbbreviateString(length);

                classBuilder.AppendLine(string.Format(fieldTemplate, variableName, stringValue));
            }

            classBuilder.AppendLine("    }");
            classBuilder.AppendLine("}");

            return classBuilder.ToString();
        }

        /// <summary>
        /// Converts the specified string to an alphanumeric format, optionally allowing periods.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="allowPeriods">If set to true, periods are allowed in the output string.</param>
        /// <returns>The converted alphanumeric string with optional periods.</returns>
        public static string ConvertToAlphanumeric(this string input, bool allowPeriods = false) {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            List<char> filteredChars = new List<char>();
            int lastValidIndex = -1;

            // Iterate over the input string, filtering and determining valid start/end indices
            foreach (char character in input
                         .Where
                         (
                             character => char
                                 .IsLetterOrDigit(character) || character == '_' || (allowPeriods && character == '.')
                         )
                         .Where(character => filteredChars.Count != 0 || (!char.IsDigit(character) && character != '.'))) {

                filteredChars.Add(character);
                lastValidIndex = filteredChars.Count - 1; // Update lastValidIndex for valid characters
            }

            // Remove trailing periods
            while (lastValidIndex >= 0 && filteredChars[lastValidIndex] == '.') {
                lastValidIndex--;
            }

            // Return the filtered string
            return lastValidIndex >= 0
                ? new string(filteredChars.ToArray(), 0, lastValidIndex + 1) : string.Empty;
        }
    }
}