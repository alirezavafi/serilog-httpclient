using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Serilog.HttpClient.Extensions
{
    /// <summary>
    /// LogContext extensions for pushing multiple properties
    /// </summary>
    public static class LogContext
    {
        /// <summary>
        /// Pushes list of key/value pair to LogContext
        /// </summary>
        /// <param name="propertyValuePair">list of key/value pair</param>
        /// <param name="destructureObjects">destructure property value</param>
        /// <returns></returns>
        public static IDisposable PushProperties(IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> propertyValuePair, bool destructureObjects = false)
        {
            var disposables = propertyValuePair.Select(pair => Serilog.Context.LogContext.PushProperty(pair.Key, pair.Value, destructureObjects));
            return new AggregatedDisposable(disposables);
        }
        
        /// <summary>
        /// Pushes object properties to LogContext
        /// </summary>
        /// <param name="values">object to push properties</param>
        /// <param name="destructureObjects">destructure property value</param>
        /// <returns></returns>
        public static IDisposable PushProperties(object values, bool destructureObjects = false)
        {
            var disposables = values.FlattenAsDictionary().Select(pair => Serilog.Context.LogContext.PushProperty(pair.Key, pair.Value, destructureObjects));
            return new AggregatedDisposable(disposables);
        }

        private static IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> FlattenAsDictionary(this object values)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (values != null)
            {
                foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(values))
                {
                    object obj = propertyDescriptor.GetValue(values);
                    dict.Add(propertyDescriptor.Name, obj);
                }
            }

            return dict;
        }
    }
}