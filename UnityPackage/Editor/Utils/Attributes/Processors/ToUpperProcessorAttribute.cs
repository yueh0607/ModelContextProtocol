using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 转大写处理器
    /// 将字符串转换为大写
    /// </summary>
    public class ToUpperProcessorAttribute : McpParameterProcessorAttribute
    {
        public override object Process(object value, Type parameterType)
        {
            if (value == null || parameterType != typeof(string))
            {
                return value;
            }

            try
            {
                return value.ToString().ToUpperInvariant();
            }
            catch
            {
                return value;
            }
        }
    }
}
