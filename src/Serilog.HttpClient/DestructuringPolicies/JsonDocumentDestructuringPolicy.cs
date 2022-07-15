using System;
using System.Linq;
using System.Text.Json;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.HttpClient.DestructuringPolicies
{
    internal class JsonDocumentDestructuringPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory _, out LogEventPropertyValue result)
        {
            if (!(value is JsonDocument jdoc))
            {
                result = null;
                return false;
            }

            result = Destructure(jdoc.RootElement);
            return true;
        }

        static LogEventPropertyValue Destructure(in JsonElement jel)
        {
            switch (jel.ValueKind)
            {
                case JsonValueKind.Array:
                    return new SequenceValue(jel.EnumerateArray().Select(ae => Destructure(in ae)));

                case JsonValueKind.False:
                    return new ScalarValue(false);

                case JsonValueKind.True:
                    return new ScalarValue(true);

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return new ScalarValue(null);

                case JsonValueKind.Number:
                    return new ScalarValue(jel.GetDecimal());

                case JsonValueKind.String:
                    return new ScalarValue(jel.GetString());

                case JsonValueKind.Object:
                    return new StructureValue(jel.EnumerateObject().Select(jp => new LogEventProperty(jp.Name, Destructure(jp.Value))));

                default:
                    throw new ArgumentException("Unrecognized value kind " + jel.ValueKind + ".");
            }
        }
    }
}
