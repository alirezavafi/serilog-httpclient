using Serilog.Events;

namespace Serilog.HttpClient.Tests.Support
{
    public static class Extensions
    {
        public static object ToScalar(this LogEventPropertyValue @this)
        {
            if (@this is not ScalarValue)
                throw new ArgumentException("Must be a ScalarValue", nameof(@this));
            return ((ScalarValue)@this).Value;
        }

        public static IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> ToDictionary(this LogEventPropertyValue @this)
        {
            if (@this is not DictionaryValue)
                throw new ArgumentException("Must be a DictionaryValue", nameof(@this));

            return ((DictionaryValue)@this).Elements;
        }

        public static IReadOnlyList<LogEventPropertyValue> ToSequence(this LogEventPropertyValue @this)
        {
            if (@this is not SequenceValue)
                throw new ArgumentException("Must be a SequenceValue", nameof(@this));

            return ((SequenceValue)@this).Elements;
        }
    }
}
