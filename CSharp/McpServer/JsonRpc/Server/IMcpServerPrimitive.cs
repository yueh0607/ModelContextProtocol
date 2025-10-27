using System.Collections.Generic;

namespace JsonRpc.Server
{
    /// <summary>
    /// 表示 MCP 服务器原语，例如工具或提示。
    /// </summary>
    public interface IMcpServerPrimitive
    {
        /// <summary>获取原语的唯一标识符。</summary>
        string Id { get; }
        
        /// <summary>
        /// 获取此原始实例的元数据。
        /// </summary>
        /// <remarks>
        /// 包含来自关联 MethodInfo 和声明类（如果有）的属性，
        /// 类级属性出现在方法级属性之前。
        /// </remarks>
        IReadOnlyList<object> Metadata { get; }
        
    }
}