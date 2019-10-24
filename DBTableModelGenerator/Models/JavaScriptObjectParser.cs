using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace DBTableModelGenerator.Models
{
    public class JavaScriptObjectParser
    {

        public static string Parse(object obj)
        {
            if (IsEnumeric(obj))
            {
                return ParseArray((IEnumerable)obj);
            }

            List<string> strProps = new List<string>();
            var props = GetProperties(obj);
            foreach (var prop in props)
            {
                if (!prop.CanRead) { continue; }
                // if (!prop.CanWrite) { continue; }
                strProps.Add(string.Format("{0}: {1}", prop.Name, GetValue(prop.GetValue(obj))));
            }

            return "{" + string.Join(", ", strProps) + "}";
        }

        private static string ParseArray(IEnumerable array)
        {
            List<string> strProps = new List<string>();
            foreach (var obj in array)
            {
                strProps.Add(GetValue(obj));
            }

            return "[" + string.Join(", ", strProps) + "]";
        }

        private static PropertyInfo[] GetProperties(object obj)
        {
            return obj.GetType().GetProperties();
        }

        private static string GetValue(object value)
        {
            if (value == null) { return "''"; }
            if (value.GetType() == typeof(string))
            {
                return string.Format("\"{0}\"", value);
            }
            if (IsNumericType(value.GetType()))
            {
                return string.Format("{0}", value);
            }
            if (value.GetType() == typeof(bool))
            {
                return ((bool)value).ToString().ToLower();
            }

            return Parse(value);
        }

        private static bool IsNumericType(Type propType)
        {
            return Type.GetTypeCode(propType) == TypeCode.Byte ||
                Type.GetTypeCode(propType) == TypeCode.SByte ||
                Type.GetTypeCode(propType) == TypeCode.UInt16 ||
                Type.GetTypeCode(propType) == TypeCode.UInt32 ||
                Type.GetTypeCode(propType) == TypeCode.UInt64 ||
                Type.GetTypeCode(propType) == TypeCode.Int16 ||
                Type.GetTypeCode(propType) == TypeCode.Int32 ||
                Type.GetTypeCode(propType) == TypeCode.Int64 ||
                Type.GetTypeCode(propType) == TypeCode.Decimal ||
                Type.GetTypeCode(propType) == TypeCode.Double ||
                Type.GetTypeCode(propType) == TypeCode.Single;
        }

        private static bool IsEnumeric(object obj)
        {
            return obj.GetType()
                     .GetInterfaces()
                     .Any(t => t.IsGenericType
                            && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) && obj.GetType() != typeof(string);
        }
    }

}