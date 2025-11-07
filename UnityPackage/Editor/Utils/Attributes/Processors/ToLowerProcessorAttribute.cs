using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 转小写处理器
    /// 将字符串转换为小写
    /// </summary>
    public class ToLowerProcessorAttribute : McpParameterProcessorAttribute
    {
        public override object Process(object value, Type parameterType)
        {
            if (value == null || parameterType != typeof(string))
            {
                return value;
            }

            try
            {
                return value.ToString().ToLowerInvariant();
            }
            catch
            {
                return value;
            }
        }
    }
}
