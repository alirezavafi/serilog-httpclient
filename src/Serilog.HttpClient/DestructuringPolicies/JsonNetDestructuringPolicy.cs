﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.HttpClient.DestructuringPolicies
{
    // taken from https://github.com/destructurama/json-net
    internal class JsonNetDestructuringPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            switch (value)
            {
                case JObject jo:
                    result = Destructure(jo, propertyValueFactory);
                    return true;
                case JArray ja:
                    result = Destructure(ja, propertyValueFactory);
                    return true;
                case JValue jv:
                    result = Destructure(jv, propertyValueFactory);
                    return true;
            }

            result = null;
            return false;
        }

        private static LogEventPropertyValue Destructure(JValue jv, ILogEventPropertyValueFactory propertyValueFactory)
        {
            return propertyValueFactory.CreatePropertyValue(jv.Value, true);
        }

        private static LogEventPropertyValue Destructure(JArray ja, ILogEventPropertyValueFactory propertyValueFactory)
        {
            var elems = ja.Select(t => propertyValueFactory.CreatePropertyValue(t, true));
            return new SequenceValue(elems);
        }

        private static LogEventPropertyValue Destructure(JObject jo, ILogEventPropertyValueFactory propertyValueFactory)
        {
            string typeTag = null;
            var props = new List<LogEventProperty>(jo.Count);

            foreach (var prop in jo.Properties())
            {
                if (prop.Name == "$type")
                {
                    if (prop.Value is JValue typeVal && typeVal.Value is string)
                    {
                        typeTag = (string)typeVal.Value;
                        continue;
                    }
                }
                else if (!LogEventProperty.IsValidName(prop.Name))
                {
                    return DestructureToDictionaryValue(jo, propertyValueFactory);
                }

                props.Add(new LogEventProperty(prop.Name, propertyValueFactory.CreatePropertyValue(prop.Value, true)));
            }

            return new StructureValue(props, typeTag);
        }

        private static LogEventPropertyValue DestructureToDictionaryValue(JObject jo, ILogEventPropertyValueFactory propertyValueFactory)
        {
            var elements = jo.Properties().Select(
                prop =>
                    new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                        new ScalarValue(prop.Name),
                        propertyValueFactory.CreatePropertyValue(prop.Value, true)
                    )
            );
            return new DictionaryValue(elements);
        }
    }
}