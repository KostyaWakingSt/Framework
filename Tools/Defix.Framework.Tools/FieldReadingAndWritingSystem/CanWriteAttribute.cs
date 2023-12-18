using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defix.Framework.Tools.FieldReadingAndWritingSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    internal class CanWriteAttribute : Attribute
    {
        /// <summary>
        /// The key used to access the value of the variable 
        /// (if null or empty, the name of the variable on which the attribute is located will be used)
        /// </summary>
        public string Key { get; private set; } = string.Empty;

        public CanWriteAttribute() { }

        public CanWriteAttribute(string key)
        {
            Key = key;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    internal class LoadDataAttribute : Attribute
    {
        public string Key { get; private set; } = string.Empty;
        public string Header { get; private set; } = string.Empty;

        public LoadDataAttribute(string key, string header)
        {
            Key = key;
            Header = header;
        }
    }
}
