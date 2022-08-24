using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace Serilog.HttpClient.Extensions
{
    public static class JsonExtension
    {
        public static bool TryGetJToken(this string text, out JToken jToken)
        {
            jToken = null;
            text = text.Trim();
            if ((text.StartsWith("{") && text.EndsWith("}")) || //For object
                (text.StartsWith("[") && text.EndsWith("]"))) //For array
            {
                try
                {
                    jToken = JToken.Parse(text);
                    return true;
                }
                catch(Exception) {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Masks specified json string using provided options
        /// </summary>
        /// <param name="json">Json to mask</param>
        /// <param name="blacklist">Fields to mask</param>
        /// <param name="mask">Mask format</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static JToken MaskFields(this JToken json, string[] blacklist, string mask)
        {
            if (blacklist == null)
                throw new ArgumentNullException(nameof(blacklist));

            if (blacklist.Any() == false)
                return json;

            if (json is JArray jArray)
            {
                foreach (var jToken in jArray)
                {
                    MaskFieldsFromJToken(jToken, blacklist, mask);
                }
            }
            else if (json is JObject jObject)
            {
                MaskFieldsFromJToken(jObject, blacklist, mask);
            }

            return json;
        }

        private static void MaskFieldsFromJToken(JToken token, string[] blacklist, string mask)
        {
            JContainer container = token as JContainer;
            if (container == null)
            {
                return; // abort recursive
            }

            List<JToken> removeList = new List<JToken>();
            foreach (JToken jtoken in container.Children())
            {
                if (jtoken is JProperty prop)
                {
                    if (IsMaskMatch(prop.Path, blacklist))
                    {
                        removeList.Add(jtoken);
                    }
                }

                // call recursive
                MaskFieldsFromJToken(jtoken, blacklist, mask);
            }

            // replace
            foreach (JToken el in removeList)
            {
                var prop = (JProperty)el;
                prop.Value = mask;
            }
        }

        /// <summary>
        /// Check whether specified path must be masked
        /// </summary>
        /// <param name="path"></param>
        /// <param name="blacklist"></param>
        /// <returns></returns>
        public static bool IsMaskMatch(string path, string[] blacklist)
        {
            return blacklist.Any(item => Regex.IsMatch(path, WildCardToRegular(item), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
        }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }

        /// <summary>
        /// Masks key-value paired items
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <param name="blacklist"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> Mask(
            this IEnumerator<KeyValuePair<string, IEnumerable<string>>> keyValuePairs, string[] blacklist,
            string mask)
        {
            var valuePairs = new List<KeyValuePair<string, IEnumerable<string>>>();
            while (keyValuePairs.MoveNext())
            {
                var item = keyValuePairs.Current;
                if (IsMaskMatch(item.Key, blacklist))
                    valuePairs.Add(new KeyValuePair<string, IEnumerable<string>>(item.Key, new []{mask}));
                else 
                    valuePairs.Add(new KeyValuePair<string, IEnumerable<string>>(item.Key, item.Value));
            }

            return valuePairs;
        }
    }
}
