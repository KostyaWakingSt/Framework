using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        private void WriteFields(ref StringBuilder builder)
        {
            foreach (var fieldsData in _fieldsDatas)
            {
                if (!fieldsData.ObjectToWrite.HasWriteAttributes())
                    throw new ArgumentNullException("The class has no write attributes.");

                builder.Append($"[{fieldsData.Header}]\n");

                WriteFieldVariables(ref builder, fieldsData.ObjectToWrite);
                WritePropertyVariables(ref builder, fieldsData.ObjectToWrite);

                builder.Append($"[/{fieldsData.Header}]\n\n");
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
    }

    internal static class ClassToWriteValidator
    {
        public static bool HasWriteAttributes(this object obj)
        {
            return CheckFields(obj) || CheckProperties(obj);
        }

        private static bool CheckFields(object obj)
        {
            foreach (var field in obj.GetType().GetFields())
                if (Attribute.IsDefined(field, typeof(CanWriteAttribute)))
                    return true;

            return false;
        }

        private static bool CheckProperties(object obj)
        {
            foreach (var field in obj.GetType().GetProperties())
                if (Attribute.IsDefined(field, typeof(CanWriteAttribute)))
                    return true;

            return false;
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
