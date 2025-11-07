using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 前缀/后缀处理器
    /// 为字符串添加前缀或后缀
    /// </summary>
    public class PrefixSuffixProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 要添加的前缀
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 要添加的后缀
        /// </summary>
        public string Suffix { get; set; }

        public PrefixSuffixProcessorAttribute()
        {
        }

        public PrefixSuffixProcessorAttribute(string prefix = null, string suffix = null)
        {
            Prefix = prefix;
            Suffix = suffix;
        }

        public override object Process(object value, Type parameterType)
        {
            if (value == null || parameterType != typeof(string))
            {
                return value;
            }

            try
            {
                string str = value.ToString();

                if (!string.IsNullOrEmpty(Prefix))
                {
                    str = Prefix + str;
                }

                if (!string.IsNullOrEmpty(Suffix))
                {
                    str = str + Suffix;
                }

                // 如果没有添加任何前缀或后缀，返回原值
                if (string.IsNullOrEmpty(Prefix) && string.IsNullOrEmpty(Suffix))
                {
                    return value;
                }

                return str;
            }
            catch
            {
                return value;
            }
        }
    }
}
