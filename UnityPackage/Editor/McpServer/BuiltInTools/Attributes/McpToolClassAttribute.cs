using System;

namespace UnityAIStudio.McpServer.Tools.Attributes
{
    /// <summary>
    /// 标记一个类包含MCP工具方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class McpToolClassAttribute : Attribute
    {
        /// <summary>
        /// 工具类别（用于分组）
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 工具类描述
        /// </summary>
        public string Description { get; set; }

        public McpToolClassAttribute()
        {
        }

        public McpToolClassAttribute(string category)
        {
            Category = category;
        }
    }
}
