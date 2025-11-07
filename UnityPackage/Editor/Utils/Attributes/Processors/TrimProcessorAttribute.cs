using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 字符串修剪处理器
    /// 自动去除字符串首尾的空白字符
    /// </summary>
    public class TrimProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 处理字符串参数，去除首尾空白
        /// </summary>
        public override object Process(object value, Type parameterType)
        {
            if (value == null || parameterType != typeof(string))
            {
                return value;
            }

            string str = value.ToString();
            if (string.IsNullOrEmpty(str))
            {
                return value;
            }

            try
            {
                return str.Trim();
            }
            catch
            {
                return value;
            }
        }
    }
}
