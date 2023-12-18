using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer.Internal;
using UnityEngine;

namespace Defix.Framework.Tools.FieldReadingAndWritingSystem
{
    public sealed class FieldsReader
    {
        public struct Field
        {
            public string Name;
            public string Value;

            public Field(string name, string value)
            {
                Name = name; 
                Value = value;
            }
        }

        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        /// <summary>
        /// The path to the config file
        /// </summary>
        private readonly string _pathToReadFile;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathToReadFile">The path to the config file</param>
        public FieldsReader(string pathToReadFile)
        {
            _pathToReadFile = pathToReadFile;
        }

        /// <summary>
        /// Returns the value from the config file (async)
        /// </summary>
        /// <param name="name">Key to value</param>
        /// <param name="header">Sorting header</param>
        /// <returns>A value with a supporting type</returns>
        public async Task<TypeToConvert> GetValueByNameAndHeaderAsync<ConvertClass, TypeToConvert>(string name, string header) where ConvertClass : StringTypeToConvert<TypeToConvert>
        {
            var lines = await GetAllLinesFromConfigAsync();

            return GetValueByNameAndHeader<ConvertClass, TypeToConvert>(lines, name, header);
        }

        /// <summary>
        /// Returns the value from the config file
        /// </summary>
        /// <param name="name">Key to value</param>
        /// <param name="header"></param>
        /// <returns>A value with a supporting type</returns>
        public TypeToConvert GetValueByNameAndHeader<ConvertClass, TypeToConvert>(string name, string header) where ConvertClass : StringTypeToConvert<TypeToConvert>
        {
            var lines = GetAllLinesFromConfig();

            return GetValueByNameAndHeader<ConvertClass, TypeToConvert>(lines, name, header);
        }

        private static int GetStartElementIndexByHeader(string[] lines, string header)
        {
            int startLineIndex = ReadLineParser.GetLineValueIndex(lines, $"[{header}]");

            return startLineIndex + 1;
        }

        private static int GetEndElementIndexByHeader(string[] lines, string header)
        {
            int endLineIndex = ReadLineParser.GetLineValueIndex(lines, $"[/{header}]");

            return endLineIndex - 1;
        }

        public int GetIndexFromFieldData(string header, Field field)
        {
            var lines = GetAllLinesFromConfig();

            for(int i = 0; i < GetEndElementIndexByHeader(lines, header); i++)
            {
                int startElementIndex = GetStartElementIndexByHeader(lines, header);

                if (lines[i + startElementIndex].Contains($"{field.Name}: {field.Value}"))
                {
                    return i + startElementIndex;
                }
            }

            throw new NullReferenceException("Field dont find.");
        }

        private static TypeToConvert GetValueByNameAndHeader<ConvertClass, TypeToConvert>(string[] lines, string name, string header) where ConvertClass : StringTypeToConvert<TypeToConvert>
        {
            int startLineIndex = ReadLineParser.GetLineValueIndex(lines, $"[{header}]");
            int endLineIndex = ReadLineParser.GetLineValueIndex(lines, $"[/{header}]");

            for (int i = startLineIndex; i < endLineIndex; i++)
            {
                if (lines[i].Contains($"{name}: "))
                {
                    return ReadLineParser.ConvertLine<ConvertClass, TypeToConvert>(lines[i]);
                }
            }

            throw new NullReferenceException("This name do not find.");
        }

        public void LoadDataToClassByType<ConvertClass, TypeToConvert>(object loadDataClass) where ConvertClass : StringTypeToConvert<TypeToConvert>
        {
            if (!loadDataClass.HasLoadAttributes())
                return;

            LoadFieldsToClassByType<ConvertClass, TypeToConvert>(loadDataClass);
            LoadPropertiesToClassByType<ConvertClass, TypeToConvert>(loadDataClass);
        }

        public void LoadFieldsToClassByType<ConvertClass, TypeToConvert>(object loadDataClass) where ConvertClass : StringTypeToConvert<TypeToConvert>
        {
            foreach (var field in loadDataClass.GetType().GetFields(Flags))
            {
                if (!field.IsDefined(typeof(LoadDataAttribute), false) || field.FieldType != typeof(TypeToConvert))
                    continue;

                var attribute = field.GetCustomAttributes(false).OfType<LoadDataAttribute>().First();

                if (!HasHeaderInPath(attribute.Header))
                    continue;

                var receivedData = GetValueByNameAndHeader<ConvertClass, TypeToConvert>(attribute.Key, attribute.Header);

                field.SetValue(loadDataClass, receivedData);
            }
        }

        public void LoadPropertiesToClassByType<ConvertClass, TypeToConvert>(object loadDataClass) where ConvertClass : StringTypeToConvert<TypeToConvert>
        {
            foreach (var field in loadDataClass.GetType().GetProperties(Flags))
            {
                if (!field.IsDefined(typeof(LoadDataAttribute), false) || field.PropertyType != typeof(TypeToConvert))
                    continue;

                var attribute = field.GetCustomAttributes(false).OfType<LoadDataAttribute>().First();

                if (!HasHeaderInPath(attribute.Header))
                    continue;

                var receivedData = GetValueByNameAndHeader<ConvertClass, TypeToConvert>(attribute.Key, attribute.Header);

                field.SetValue(loadDataClass, receivedData);
            }
        }

        public Field[] GetFieldsByHeader(string header)
        {
            List<Field> outputFields = new();
            string[] lines = GetAllLinesFromConfig();

            for (int i = 0; i < GetElementCountByHeader(lines, header); i++)
            {
                outputFields.Add(GetFieldByIndexAndHeader(lines, i, header));
            }

            return outputFields.ToArray();
        }

        public static Field[] GetFieldsByHeader(string[] lines, string header)
        {
            List<Field> outputFields = new();

            for (int i = 0; i < GetElementCountByHeader(lines, header); i++)
            {
                outputFields.Add(GetFieldByIndexAndHeader(lines, i, header));
            }

            return outputFields.ToArray();
        }

        public static Field GetFieldByIndexAndHeader(string[] lines, int index, string header)
        {
            int startLineIndex = ReadLineParser.GetLineValueIndex(lines, $"[{header}]") + 1;

            string name = ReadLineParser.GetNameByLine(lines[index + startLineIndex]);
            string value = ReadLineParser.GetValueByLine(lines[index + startLineIndex]);

            Field outputField = new(name, value);

            return outputField;
        }

        public Field GetFieldByIndexAndHeader(int index, string header)
        {
            return GetFieldByIndexAndHeader(GetAllLinesFromConfig(), index, header);
        }

        public int GetElementCountByHeader(string header)
        {
            return GetElementCountByHeader(GetAllLinesFromConfig(), header);
        }

        public static int GetElementCountByHeader(string[] lines, string header)
        {
            return GetEndElementIndexByHeader(lines, header) + 1 - GetStartElementIndexByHeader(lines, header);
        }

        public bool HasHeaderInPath(string header)
        {
            var lines = File.ReadAllLines(_pathToReadFile);

            if (lines.Contains($"[{header}]"))
                return true;
            else
                return false;
        }

        private string[] GetAllLinesFromConfig()
        {
            return File.ReadAllLines(_pathToReadFile);
        }

        private async Task<string[]> GetAllLinesFromConfigAsync()
        {
            return await File.ReadAllLinesAsync(_pathToReadFile);
        }
    }

    internal class ReadLineParser
    {
        internal static TypeToConvert ConvertLine<ConvertClass, TypeToConvert>(string value) where ConvertClass : StringTypeToConvert<TypeToConvert>
        {
            string parseValue = GetValueByLine(value);

            if (typeof(TypeToConvert).IsEnum)
            {
                return (TypeToConvert)Enum.Parse(typeof(TypeToConvert), parseValue);
            }

            return ((ConvertClass)Activator.CreateInstance(typeof(ConvertClass), new object[] { parseValue })!).Parse();
        }

        internal static string GetValueByLine(string line)
        {
            string result = string.Empty;

            for (int i = line.IndexOf(":") + 2; i < line.Length; i++)
            {
                result += line[i];
            }

            return result;
        }

        internal static string GetNameByLine(string line)
        {
            string result = string.Empty;

            for (int i = 0; i < line.IndexOf(":"); i++)
            {
                result += line[i];
            }

            return result;
        }

        internal static int GetLineValueIndex(string[] lines, string lineValue)
        {
            const int errorExitCode = -1;

            int resultIndex = errorExitCode;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == lineValue)
                {
                    resultIndex = i;

                    break;
                }
            }

            if (resultIndex == errorExitCode)
                throw new NullReferenceException("Line do not find.");

            return resultIndex;
        }
    }

    public abstract class StringTypeToConvert<T>
    {
        protected readonly string StringToConvert;

        public StringTypeToConvert(string stringToConvert)
        {
            StringToConvert = stringToConvert;
        }

        public abstract T Parse();
    }

    public class IntToConvert : StringTypeToConvert<int>
    {
        public IntToConvert(string stringToConvert) : base(stringToConvert) { }

        public override int Parse()
        {
            return int.Parse(StringToConvert);
        }
    }

    public class FloatToConvert : StringTypeToConvert<float>
    {
        public FloatToConvert(string stringToConvert) : base(stringToConvert) { }

        public override float Parse()
        {
            return float.Parse(StringToConvert);
        }
    }

    public class BoolToConvert : StringTypeToConvert<bool>
    {
        public BoolToConvert(string stringToConvert) : base(stringToConvert) { }

        public override bool Parse()
        {
            return bool.Parse(StringToConvert);
        }
    }
}
