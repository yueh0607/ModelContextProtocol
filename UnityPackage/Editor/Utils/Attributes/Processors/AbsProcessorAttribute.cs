using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 绝对值处理器
    /// 将数值转换为其绝对值
    /// </summary>
    public class AbsProcessorAttribute : McpParameterProcessorAttribute
    {
        public override object Process(object value, Type parameterType)
        {
            if (value == null)
            {
                return value;
            }

            try
            {
                if (parameterType == typeof(int) || parameterType == typeof(int?))
                {
                    return Math.Abs(Convert.ToInt32(value));
                }
                else if (parameterType == typeof(float) || parameterType == typeof(float?))
                {
                    return Math.Abs(Convert.ToSingle(value));
                }
                else if (parameterType == typeof(double) || parameterType == typeof(double?))
                {
                    return Math.Abs(Convert.ToDouble(value));
                }
                else if (parameterType == typeof(long) || parameterType == typeof(long?))
                {
                    return Math.Abs(Convert.ToInt64(value));
                }
                else if (parameterType == typeof(decimal) || parameterType == typeof(decimal?))
                {
                    return Math.Abs(Convert.ToDecimal(value));
                }

                return value;
            }
            catch
            {
                return value;
            }
        }
    }
}
