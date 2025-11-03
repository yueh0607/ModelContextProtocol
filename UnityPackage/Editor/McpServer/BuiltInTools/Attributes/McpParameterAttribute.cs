using System;

namespace UnityAIStudio.McpServer.Tools.Attributes
{
    /// <summary>
    /// 标记工具方法的参数
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class McpParameterAttribute : Attribute
    {
        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// 示例值
        /// </summary>
        public string Example { get; set; }

        public McpParameterAttribute()
        {
        }

        public McpParameterAttribute(string description)
        {
            Description = description;
        }
    }
}
