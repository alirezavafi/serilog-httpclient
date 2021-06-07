// https://github.com/ThiagoBarradas/jsonmasking/blob/master/JsonMasking/JsonMasking.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Serilog.HttpClient
{
    /// <summary>
    /// Masking extension for json strings
    /// </summary>
    public static class MaskHelper
    {
        /// <summary>
        /// Masks specified json string using provided options
        /// </summary>
        /// <param name="json">Json to mask</param>
        /// <param name="blacklist">Fields to mask</param>
        /// <param name="mask">Mask format</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
     public static string MaskFields(this string json, string[] blacklist, string mask)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            if (blacklist == null)
            {
                throw new ArgumentNullException(nameof(blacklist));
            }

            if (blacklist.Any() == false)
            {
                return json;
            }

            var jsonObject = JsonConvert.DeserializeObject(json);
            if (jsonObject is JArray jArray)
            {
                foreach (var jToken in jArray)
                {
                    MaskFieldsFromJToken(jToken, blacklist, mask);
                }
            }
            else if (jsonObject is JObject jObject)
            {
                MaskFieldsFromJToken(jObject, blacklist, mask);
            }

            return jsonObject.ToString();
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
                var prop = (JProperty) el;
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
        public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> Mask(this IEnumerable<KeyValuePair<string, IEnumerable<string>>> keyValuePairs, string[] blacklist,
            string mask)
        {
            return keyValuePairs.Select(pair => IsMaskMatch(pair.Key, blacklist)
                    ? new KeyValuePair<string, IEnumerable<string>>(pair.Key, new[] {mask} )
                    : new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value))
                .ToList();
        }
    }
}