using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Defix.Framework.Tools.FieldReadingAndWritingSystem
{
    public sealed class FieldsWriter
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        /// <summary>
        /// The path to the config file
        /// </summary>
        private readonly string _pathToWriteFile;

        /// <summary>
        /// Array of classes that will be included in the config file
        /// </summary>
        private readonly FieldsData[] _fieldsDatas;

        public FieldsWriter(string pathToWrite, params FieldsData[] objectsToWrite)
        {
            _pathToWriteFile = pathToWrite;
            _fieldsDatas = objectsToWrite;
        }

        /// <summary>
        /// Writes new data to the config file at the specified path
        /// </summary>
        /// <exception cref="ArgumentNullException">Does the class have attributes</exception>
        public void Write()
        {
            StringBuilder builder = new();

            using StreamWriter writer = new(_pathToWriteFile);

            WriteFields(ref builder);

            writer.Write(builder.ToString());
            writer.Close();
        }

        /// <summary>
        /// Writes new data to the config file at the specified path (async)
        /// </summary>
        /// <exception cref="ArgumentNullException">Does the class have attributes</exception>
        public async void WriteAsync()
        {
            StringBuilder builder = new();

            using StreamWriter writer = new(_pathToWriteFile);

            WriteFields(ref builder);

            await writer.WriteAsync(builder.ToString());
            writer.Close();
        }

        public void WriteNewFieldData(string header, FieldsReader.Field oldData, FieldsReader.Field newData)
        {
            FieldsReader reader = new(_pathToWriteFile);
            int currentFieldIndex = reader.GetIndexFromFieldData(header, oldData);

            string[] allLines = File.ReadAllLines(_pathToWriteFile);
            allLines[currentFieldIndex] = allLines[currentFieldIndex].Replace($"{oldData.Name}: {oldData.Value}", $"{newData.Name}: {newData.Value}");

            File.WriteAllLines(_pathToWriteFile, allLines);
        }

        public void ReWrite()
        {
            Dictionary<string, List<FieldsReader.Field>> fieldsToCompare = new();
            Dictionary<string, List<FieldsReader.Field>> fieldsToWrite = new();

            WriteAllFieldsToDictionary(ref fieldsToWrite);
            WriteAllCompareFieldsToDictionary(ref fieldsToCompare);
            ReWriteDataBetween(ref fieldsToWrite, ref fieldsToCompare);

            foreach (var field in _fieldsDatas)
            {
                WriteFieldsToHeader(fieldsToWrite[field.Header].ToArray(), field.Header);
            }
        }

        private void WriteAllFieldsToDictionary(ref Dictionary<string, List<FieldsReader.Field>> fieldsToWrite)
        {
            foreach (var fieldData in _fieldsDatas)
            {
                List<FieldsReader.Field> localFields = new();

                foreach (var fields in FieldsReader.GetFieldsByHeader(GetFieldsArray(), fieldData.Header))
                {
                    localFields.Add(fields);
                }

                fieldsToWrite.Add(fieldData.Header, localFields);
            }
        }

        private void WriteAllCompareFieldsToDictionary(ref Dictionary<string, List<FieldsReader.Field>> fieldsToCompare)
        {
            FieldsReader reader = new(_pathToWriteFile);

            foreach (var fieldData in _fieldsDatas)
            {
                fieldsToCompare.Add(fieldData.Header, reader.GetFieldsByHeader(fieldData.Header).ToList());
            }
        }

        private void ReWriteDataBetween(ref Dictionary<string, List<FieldsReader.Field>> from, ref Dictionary<string, List<FieldsReader.Field>> to)
        {
            foreach (var fieldData in _fieldsDatas)
            {
                for (int i = 0; i < to.Count; i++)
                {
                    for (int j = 0; j < to[fieldData.Header].Count; j++)
                    {
                        from[fieldData.Header][j] = to[fieldData.Header][j];
                    }
                }
            }
        }

        private string[] GetFieldsArray()
        {
            StringBuilder builder = new();

            WriteFields(ref builder);

            return builder.ToString().ToLinesArray();
        }

        private void WriteFieldsToHeader(FieldsReader.Field[] fields, string header)
        {
            StringBuilder builder = new();
            FieldsReader reader = new(_pathToWriteFile);
            bool append = false;

            if (!reader.HasHeaderInPath(header))
                append = true;

            StreamWriter writer = new(_pathToWriteFile, append)
            {
                AutoFlush = true
            };

            builder.Append(GetStartHeaderFormat(header));
            foreach (var field in fields)
            {
                WriteInfoInBuilder(ref builder, field.Name, field.Value);
            }
            builder.Append(GetEndHeaderFormat(header));

            writer.WriteAsync(builder.ToString());
        }

        private void WriteFields(ref StringBuilder builder)
        {
            foreach (var fieldsData in _fieldsDatas)
            {
                if (!fieldsData.ObjectToWrite.HasWriteAttributes())
                    throw new ArgumentNullException("The class has no write attributes.");

                builder.Append(GetStartHeaderFormat(fieldsData.Header));

                WriteFieldVariables(ref builder, fieldsData.ObjectToWrite);
                WritePropertyVariables(ref builder, fieldsData.ObjectToWrite);

                builder.Append(GetEndHeaderFormat(fieldsData.Header));
            }
        }

        private static void WriteFieldVariables(ref StringBuilder builder, object obj)
        {
            foreach (var field in obj.GetType().GetFields(Flags))
            {
                if (Attribute.IsDefined(field, typeof(CanWriteAttribute)))
                {
                    WriteInfoInBuilder(ref builder, GetVariableKey(field), field.GetValue(obj)!.ToString()!);
                }
            }
        }

        private static void WritePropertyVariables(ref StringBuilder builder, object obj)
        {
            foreach (var property in obj.GetType().GetProperties(Flags))
            {
                if (Attribute.IsDefined(property, typeof(CanWriteAttribute)))
                {
                    WriteInfoInBuilder(ref builder, GetVariableKey(property), property.GetValue(obj)!.ToString()!);
                }
            }
        }

        private static string GetVariableKey(MemberInfo memberInfo)
        {
            var customAttribute = memberInfo.GetCustomAttribute<CanWriteAttribute>();

            return string.IsNullOrEmpty(customAttribute!.Key) ? memberInfo.Name : customAttribute.Key;
        }

        private static void WriteInfoInBuilder(ref StringBuilder builder, string name, string value)
        {
            builder.Append($"{name}: {value}\n");
        }

        private static string GetStartHeaderFormat(string header)
        {
            return $"[{header}]\n";
        }

        private static string GetEndHeaderFormat(string header)
        {
            return $"[/{header}]\n\n";
        }
    }

    internal static class ClassToWriteValidator
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        public static bool HasLoadAttributes(this object obj)
        {
            return CheckFields(obj, typeof(LoadDataAttribute)) || CheckProperties(obj, typeof(LoadDataAttribute));
        }

        public static bool HasWriteAttributes(this object obj)
        {
            return CheckFields(obj, typeof(CanWriteAttribute)) || CheckProperties(obj, typeof(CanWriteAttribute));
        }

        private static bool CheckFields(object obj, Type attribute)
        {
            foreach (var field in obj.GetType().GetFields(Flags))
                if (Attribute.IsDefined(field, attribute))
                    return true;

            return false;
        }

        private static bool CheckProperties(object obj, Type attribute)
        {
            foreach (var field in obj.GetType().GetProperties(Flags))
                if (Attribute.IsDefined(field, attribute))
                    return true;

            return false;
        }
    }

    internal static class StringExtension
    {
        public static string[] ToLinesArray(this string value)
        {
            List<string> outputLines = new();
            char[] charsArray = value.ToCharArray();

            string line = string.Empty;
            for (int i = 0; i < charsArray.Length; i++)
            {
                if (charsArray[i] == '\n')
                {
                    outputLines.Add(line);
                    line = string.Empty;
                }
                else
                {
                    line += charsArray[i];
                }
            }

            return outputLines.ToArray();
        }
    }

    public class FieldsData
    {
        /// <summary>
        /// Sorting header (key)
        /// </summary>
        public string Header { get; private set; }

        /// <summary>
        /// A class that must have at least one CanWrite attribute inside it
        /// </summary>
        public object ObjectToWrite { get; private set; }

        public FieldsData(string header, object objectToWrite)
        {
            Header = header;
            ObjectToWrite = objectToWrite;
        }
    }
}
