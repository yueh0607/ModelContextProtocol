using System;
using ModelContextProtocol.Protocol;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 数值范围验证处理器
    /// 验证数值是否在指定范围内，超出范围则返回错误
    /// </summary>
    public class RangeProcessorAttribute : McpParameterProcessorAttribute
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
        /// 验证数值参数是否在范围内
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
                double numValue;
                if (parameterType == typeof(int) || parameterType == typeof(int?))
                {
                    numValue = Convert.ToInt32(value);
                }
                else if (parameterType == typeof(float) || parameterType == typeof(float?))
                {
                    numValue = Convert.ToSingle(value);
                }
                else if (parameterType == typeof(double) || parameterType == typeof(double?))
                {
                    numValue = Convert.ToDouble(value);
                }
                else if (parameterType == typeof(long) || parameterType == typeof(long?))
                {
                    numValue = Convert.ToInt64(value);
                }
                else
                {
                    // 不是数值类型，不处理
                    return value;
                }

                // 验证范围（注意：用户修改为 <= 和 >=，这意味着边界值也不允许）
                if (numValue <= Min || numValue >= Max)
                {
                    return McpUtils.Error(
                        $"Value {numValue} is out of range. Expected value between {Min} and {Max}.");
                }

                return value;
            }
            catch
            {
                // 转换失败，返回原值
                return value;
            }
        }
    }
}
