using System;
using System.Text.RegularExpressions;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 正则表达式替换处理器
    /// 使用正则表达式进行字符串替换
    /// </summary>
    public class RegexReplaceProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 正则表达式模式
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// 替换字符串
        /// </summary>
        public string Replacement { get; set; }

        /// <summary>
        /// 正则表达式选项
        /// </summary>
        public RegexOptions Options { get; set; } = RegexOptions.None;

        public RegexReplaceProcessorAttribute()
        {
        }

        public RegexReplaceProcessorAttribute(string pattern, string replacement)
        {
            Pattern = pattern;
            Replacement = replacement;
        }

        public override object Process(object value, Type parameterType)
        {
            if (value == null || parameterType != typeof(string))
            {
                return value;
            }

            if (string.IsNullOrEmpty(Pattern))
            {
                return value;
            }

            try
            {
                string str = value.ToString();
                string result = Regex.Replace(str, Pattern, Replacement ?? string.Empty, Options);

                // 如果没有发生替换，返回原值
                if (result == str)
                {
                    return value;
                }

                return result;
            }
            catch
            {
                return value;
            }
        }
    }
}
