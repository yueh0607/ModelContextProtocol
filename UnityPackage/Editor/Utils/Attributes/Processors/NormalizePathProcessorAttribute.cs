using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 路径规范化处理器
    /// 统一路径分隔符为正斜杠 (/)
    /// </summary>
    public class NormalizePathProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 处理路径参数，统一使用正斜杠
        /// </summary>
        public override object Process(object value, Type parameterType)
        {
            if (value == null || parameterType != typeof(string))
            {
                return value;
            }

            string path = value.ToString();
            if (string.IsNullOrEmpty(path))
            {
                return value;
            }

            try
            {
                // 将反斜杠替换为正斜杠
                return path.Replace('\\', '/');
            }
            catch
            {
                return value;
            }
        }
    }
}
