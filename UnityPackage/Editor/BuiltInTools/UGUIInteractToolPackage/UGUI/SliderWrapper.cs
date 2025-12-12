using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// Slider 组件包装器
    /// 支持数值输入和点击交互
    /// </summary>
    public class SliderWrapper : UIBehaviourWrapper<Slider>
    {
        /// <summary>
        /// 执行滑块交互
        /// </summary>
        public override object Interact(UnityEngine.Object target, InteractiveType type, object data)
        {
            if (!(target is Slider slider))
            {
                return new { success = false, error = "目标对象不是Slider" };
            }

            switch (type)
            {
                case InteractiveType.Input:
                    try
                    {
                        if (data == null)
                        {
                            return new { success = false, error = "输入数据不能为空" };
                        }

                        // 解析输入值
                        if (!float.TryParse(data.ToString(), out float newValue))
                        {
                            return new { success = false, error = $"无法解析数值: {data}" };
                        }

                        float originalValue = slider.value;
                        float minValue = slider.minValue;
                        float maxValue = slider.maxValue;

                        // 限制值在有效范围内
                        float clampedValue = Mathf.Clamp(newValue, minValue, maxValue);

                        // 设置新值
                        slider.value = clampedValue;

                        // 触发值变化事件
                        slider.onValueChanged?.Invoke(clampedValue);

                        // 检查值是否被限制
                        string rangeInfo = "";
                        if (newValue != clampedValue)
                        {
                            rangeInfo = $" (限制在范围 {minValue}-{maxValue} 内)";
                        }

                        return new { success = true, message = $"Slider值设置成功: {originalValue:F2} -> {clampedValue:F2}{rangeInfo}" };
                    }
                    catch (Exception ex)
                    {
                        return new { success = false, error = $"设置Slider值失败: {ex.Message}" };
                    }

                case InteractiveType.Click:
                    try
                    {
                        float originalValue = slider.value;
                        float midValue = (slider.minValue + slider.maxValue) / 2f;

                        slider.value = midValue;
                        slider.onValueChanged?.Invoke(midValue);

                        return new { success = true, message = $"Slider重置到中间值: {originalValue:F2} -> {midValue:F2}" };
                    }
                    catch (Exception ex)
                    {
                        return new { success = false, error = $"Slider点击失败: {ex.Message}" };
                    }

                default:
                    return new { success = false, error = $"Slider不支持交互类型: {type}" };
            }
        }

        /// <summary>
        /// 获取支持的交互类型
        /// </summary>
        public override InteractiveType[] GetInteractiveSchema()
        {
            return new InteractiveType[] { InteractiveType.Input, InteractiveType.Click };
        }

        /// <summary>
        /// 获取滑块元数据
        /// </summary>
        public override Dictionary<string, object> GetMetadata(UnityEngine.Object target, bool includeScreenshot = false)
        {
            var metadata = new Dictionary<string, object>();

            if (!(target is Slider slider)) return metadata;

            // 当前值
            metadata["value"] = Math.Round(slider.value, 3);

            // 值范围
            metadata["minValue"] = slider.minValue;
            metadata["maxValue"] = slider.maxValue;

            // 计算百分比
            float range = slider.maxValue - slider.minValue;
            if (range > 0)
            {
                float percentage = (slider.value - slider.minValue) / range * 100f;
                metadata["percentage"] = Math.Round(percentage, 1);
            }

            // 是否为整数滑块
            metadata["wholeNumbers"] = slider.wholeNumbers;

            // 滑块方向
            metadata["direction"] = slider.direction.ToString();

            // 是否可交互
            metadata["interactable"] = slider.interactable;

            // Handle和FillRect信息
            if (slider.handleRect != null)
            {
                metadata["hasHandle"] = true;
            }

            if (slider.fillRect != null)
            {
                metadata["hasFill"] = true;
            }

            // 查找关联的文本标签
            var labels = slider.GetComponentsInChildren<Text>();
            foreach (var label in labels)
            {
                if (!string.IsNullOrEmpty(label.text))
                {
                    // 尝试识别是否是值显示标签
                    if (label.text.Contains(slider.value.ToString()) ||
                        label.text.Contains(slider.value.ToString("F1")) ||
                        label.text.Contains(slider.value.ToString("F2")))
                    {
                        metadata["valueLabel"] = label.text;
                        break;
                    }

                    // 如果没找到值标签，使用第一个非空文本作为标题
                    if (!metadata.ContainsKey("text"))
                    {
                        metadata["text"] = label.text;
                    }
                }
            }

            return metadata;
        }
    }
}

