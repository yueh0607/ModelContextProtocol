using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 字符串替换处理器
    /// 将字符串中的指定内容替换为新内容
    /// </summary>
    public class ReplaceProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 要查找的字符串
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// 替换后的字符串
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// 是否忽略大小写
        /// </summary>
        public bool IgnoreCase { get; set; } = false;

        public ReplaceProcessorAttribute()
        {
        }

        public ReplaceProcessorAttribute(string oldValue, string newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public override object Process(object value, Type parameterType)
        {
            if (value == null || parameterType != typeof(string))
            {
                return value;
            }

            if (string.IsNullOrEmpty(OldValue))
            {
                return value;
            }

            try
            {
                string str = value.ToString();
                StringComparison comparison = IgnoreCase
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

                if (str.IndexOf(OldValue, comparison) >= 0)
                {
                    // 手动实现忽略大小写的替换
                    if (IgnoreCase)
                    {
                        int index = 0;
                        while ((index = str.IndexOf(OldValue, index, comparison)) >= 0)
                        {
                            str = str.Remove(index, OldValue.Length);
                            str = str.Insert(index, NewValue ?? string.Empty);
                            index += (NewValue ?? string.Empty).Length;
                        }
                        return str;
                    }
                    else
                    {
                        return str.Replace(OldValue, NewValue ?? string.Empty);
                    }
                }

                return value;
            }
            catch
            {
                return value;
            }
        }
    }
}
