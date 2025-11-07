using System;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 数值范围限制处理器
    /// 将数值限制在指定的最小值和最大值之间
    /// </summary>
    public class ClampProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public double Min { get; set; } = double.MinValue;

        /// <summary>
        /// 最大值
        /// </summary>
        public double Max { get; set; } = double.MaxValue;

        /// <summary>
        /// 处理数值参数，限制在范围内
        /// </summary>
        public override object Process(object value, Type parameterType)
        {
            if (value == null)
            {
                return value;
            }

            try
            {
                // 支持的数值类型
                if (parameterType == typeof(int) || parameterType == typeof(int?))
                {
                    int intValue = Convert.ToInt32(value);
                    int clampedValue = (int)Math.Max(Min, Math.Min(Max, intValue));
                    return clampedValue;
                }
                else if (parameterType == typeof(float) || parameterType == typeof(float?))
                {
                    float floatValue = Convert.ToSingle(value);
                    float clampedValue = (float)Math.Max(Min, Math.Min(Max, floatValue));
                    return clampedValue;
                }
                else if (parameterType == typeof(double) || parameterType == typeof(double?))
                {
                    double doubleValue = Convert.ToDouble(value);
                    double clampedValue = Math.Max(Min, Math.Min(Max, doubleValue));
                    return clampedValue;
                }
                else if (parameterType == typeof(long) || parameterType == typeof(long?))
                {
                    long longValue = Convert.ToInt64(value);
                    long clampedValue = (long)Math.Max(Min, Math.Min(Max, longValue));
                    return clampedValue;
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
