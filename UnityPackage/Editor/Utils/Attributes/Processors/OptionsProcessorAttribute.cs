using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 选项验证处理器
    /// 验证参数值是否在预定义的选项列表中，支持多选（逗号分隔）
    /// </summary>
    public class OptionsProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 有效选项列表
        /// </summary>
        public string[] ValidOptions { get; set; }

        /// <summary>
        /// 是否支持多选（用逗号分隔）
        /// </summary>
        public bool AllowMultiple { get; set; } = true;

        /// <summary>
        /// 是否忽略大小写
        /// </summary>
        public bool IgnoreCase { get; set; } = true;

        /// <summary>
        /// 分隔符（用于多选）
        /// </summary>
        public char Separator { get; set; } = ',';

        /// <summary>
        /// 特殊的"全选"值，如果输入此值，则返回所有有效选项
        /// </summary>
        public string AllValue { get; set; } = "all";

        public OptionsProcessorAttribute()
        {
        }

        public OptionsProcessorAttribute(params string[] validOptions)
        {
            ValidOptions = validOptions;
        }

        public override object Process(object value, Type parameterType)
        {
            // 如果没有定义有效选项，直接返回原值
            if (ValidOptions == null || ValidOptions.Length == 0)
            {
                return value;
            }

            // 如果值为空，直接返回原值（由C#参数默认值处理）
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return value;
            }

            string inputValue = value.ToString();
            StringComparison comparison = IgnoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            // 检查是否是"全选"值
            if (!string.IsNullOrEmpty(AllValue) &&
                inputValue.Equals(AllValue, comparison))
            {
                // 返回所有有效选项，用分隔符连接
                return string.Join(Separator.ToString(), ValidOptions);
            }

            // 支持多选的情况
            if (AllowMultiple)
            {
                // 分割输入值
                string[] inputOptions = inputValue
                    .Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();

                // 验证每个选项
                List<string> validatedOptions = new List<string>();
                foreach (string option in inputOptions)
                {
                    string matchedOption = FindMatchingOption(option, comparison);
                    if (matchedOption != null)
                    {
                        validatedOptions.Add(matchedOption);
                    }
                }

                // 如果没有任何有效选项，返回原值
                if (validatedOptions.Count == 0)
                {
                    return value;
                }

                // 返回验证后的选项（去重）
                return string.Join(Separator.ToString(), validatedOptions.Distinct());
            }
            else
            {
                // 单选模式：验证输入是否是有效选项
                string matchedOption = FindMatchingOption(inputValue.Trim(), comparison);
                if (matchedOption != null)
                {
                    return matchedOption;
                }

                // 无效选项，返回原值
                return value;
            }
        }

        /// <summary>
        /// 查找匹配的有效选项
        /// </summary>
        private string FindMatchingOption(string input, StringComparison comparison)
        {
            foreach (string validOption in ValidOptions)
            {
                if (validOption.Equals(input, comparison))
                {
                    return validOption;
                }
            }
            return null;
        }
    }
}
