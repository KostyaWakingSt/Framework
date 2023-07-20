using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defix.Framework.Tools.FieldReadingAndWritingSystem
{
    public sealed class FieldsReader
    {
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
            var lines = await File.ReadAllLinesAsync(_pathToReadFile);

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
            var lines = File.ReadAllLines(_pathToReadFile);

            return GetValueByNameAndHeader<ConvertClass, TypeToConvert>(lines, name, header);
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
    }

    internal class ReadLineParser
    {
        internal static TypeToConvert ConvertLine<ConvertClass, TypeToConvert>(string value) where ConvertClass : StringTypeToConvert<TypeToConvert>
        {
            string parseValue = ReadLineParser.GetValueByLine(value);

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
