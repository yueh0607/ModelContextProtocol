using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 默认值处理器
    /// 当参数值为 null 或空字符串时，使用指定的默认值
    /// </summary>
    public class DefaultValueProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue { get; set; }

        public DefaultValueProcessorAttribute()
        {
        }

        public DefaultValueProcessorAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public override object Process(object value, Type parameterType)
        {
            // 如果值为 null，使用默认值
            if (value == null)
            {
                return DefaultValue;
            }

            // 如果是字符串且为空，使用默认值
            if (parameterType == typeof(string) && string.IsNullOrEmpty(value.ToString()))
            {
                return DefaultValue;
            }

            // 否则保持原值
            return value;
        }
    }
}
