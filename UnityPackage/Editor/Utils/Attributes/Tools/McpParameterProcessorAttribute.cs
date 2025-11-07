using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// MCP 参数处理器特性基类
    /// 用于在工具调用时自动处理参数值
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public abstract class McpParameterProcessorAttribute : Attribute
    {
        /// <summary>
        /// 处理器执行顺序，数字越小越先执行
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// 处理参数值
        /// </summary>
        /// <param name="value">原始参数值</param>
        /// <param name="parameterType">参数类型</param>
        /// <returns>处理后的参数值，如果处理失败返回 null</returns>
        public abstract object Process(object value, Type parameterType);
    }
}
