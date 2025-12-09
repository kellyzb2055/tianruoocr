using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TrOCR.Properties;

namespace TrOCR.Helper
{
    public class HanToPinyin
    {
        private static readonly Dictionary<string, string> WordsDictionary;
        private static readonly int MaxWordLength;
        static HanToPinyin()
        {
            var text = Resources.pinyin;
            WordsDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
            MaxWordLength = WordsDictionary.Keys.Any() ? WordsDictionary.Keys.Max(k => k.Length) : 0;
        }

        public static string GetFirstLetter(string input)
        {
            input = input.Split(new[] { ':', '-' }, StringSplitOptions.RemoveEmptyEntries)[0];
            input = Regex.Replace(input, @"[^\u4e00-\u9fa5]", "");
            var strArr = GetFullPinyin(input).Split(new[] {'\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
            return strArr.Aggregate("", (current, s) => current + s[0]).ToUpper();
        }

        public static string GetFullPinyin(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }

            var builder = new StringBuilder();
            var index = 0;
            while (index < input.Length)
            {
                var lengthToCheck = Math.Min(MaxWordLength, input.Length - index);
                var matchedLength = 0;
                string matchedValue = null;

                // 采用“最长匹配”策略，减少拆字错误
                for (var i = lengthToCheck; i > 0; i--)
                {
                    var candidate = input.Substring(index, i);
                    if (WordsDictionary.TryGetValue(candidate, out matchedValue))
                    {
                        matchedLength = i;
                        break;
                    }
                }

                if (matchedLength > 0)
                {
                    builder.Append(matchedValue);
                    index += matchedLength;
                    continue;
                }

                // 字典中不存在的字符直接原样附加，避免抛异常导致整段失败
                builder.Append(input[index]);
                index++;
            }
            return builder.ToString();
        }
    }
}