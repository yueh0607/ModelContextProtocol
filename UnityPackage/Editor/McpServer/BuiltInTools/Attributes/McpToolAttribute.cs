using System;

namespace UnityAIStudio.McpServer.Tools.Attributes
{
    /// <summary>
    /// 标记一个方法为MCP工具
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class McpToolAttribute : Attribute
    {
        /// <summary>
        /// 工具名称（如果不指定，使用方法名）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 工具描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 工具类别
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 是否默认启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        public McpToolAttribute()
        {
        }

        public McpToolAttribute(string description)
        {
            Description = description;
        }
    }
}
