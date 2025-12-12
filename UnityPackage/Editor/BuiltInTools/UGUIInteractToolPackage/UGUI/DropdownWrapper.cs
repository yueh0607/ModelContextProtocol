using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// Dropdown 组件包装器
    /// 支持点击展开和直接选择交互
    /// </summary>
    public class DropdownWrapper : UIBehaviourWrapper<Dropdown>
    {
        /// <summary>
        /// 执行下拉框交互
        /// </summary>
        public override object Interact(UnityEngine.Object target, InteractiveType type, object data)
        {
            if (!(target is Dropdown dropdown))
            {
                return new { success = false, error = "目标对象不是Dropdown" };
            }

            switch (type)
            {
                case InteractiveType.Click:
                    try
                    {
                        return HandleDropdownClick(dropdown);
                    }
                    catch (Exception ex)
                    {
                        return new { success = false, error = $"Dropdown点击失败: {ex.Message}" };
                    }

                case InteractiveType.Input:
                    try
                    {
                        return HandleDropdownInput(dropdown, data);
                    }
                    catch (Exception ex)
                    {
                        return new { success = false, error = $"Dropdown设置失败: {ex.Message}" };
                    }

                default:
                    return new { success = false, error = $"Dropdown不支持交互类型: {type}" };
            }
        }

        /// <summary>
        /// 处理下拉框点击
        /// </summary>
        private object HandleDropdownClick(Dropdown dropdown)
        {
            string currentOption = dropdown.options.Count > 0 && dropdown.value >= 0 && dropdown.value < dropdown.options.Count
                ? dropdown.options[dropdown.value].text
                : "无选项";

            // 模拟真实点击事件
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(dropdown.gameObject, eventData, ExecuteEvents.pointerClickHandler);

            // 强制刷新Canvas
            Canvas.ForceUpdateCanvases();

            int optionCount = dropdown.options.Count;
            string optionsList = GetOptionsPreview(dropdown);

            return new { success = true, message = $"Dropdown点击已执行 | 当前选项: '{currentOption}' | 可用选项({optionCount}): {optionsList}" };
        }

        /// <summary>
        /// 处理下拉框输入选择
        /// </summary>
        private object HandleDropdownInput(Dropdown dropdown, object data)
        {
            if (data == null)
            {
                return new { success = false, error = "输入数据不能为空" };
            }

            if (dropdown.options.Count == 0)
            {
                return new { success = false, error = "Dropdown没有可选项" };
            }

            string inputText = data.ToString();
            int oldValue = dropdown.value;
            string oldOption = oldValue >= 0 && oldValue < dropdown.options.Count
                ? dropdown.options[oldValue].text
                : "无效选项";

            // 方式1: 尝试按索引设置
            if (int.TryParse(inputText, out int index))
            {
                if (index >= 0 && index < dropdown.options.Count)
                {
                    dropdown.value = index;
                    dropdown.onValueChanged?.Invoke(index);

                    string newOption = dropdown.options[index].text;
                    return new { success = true, message = $"Dropdown选项设置成功: '{oldOption}' -> '{newOption}' (索引: {oldValue} -> {index})" };
                }
                else
                {
                    return new { success = false, error = $"索引超出范围: {index}，有效范围: 0-{dropdown.options.Count - 1}" };
                }
            }

            // 方式2: 按文本匹配设置
            string searchText = inputText.ToLower().Trim();

            // 精确匹配
            for (int i = 0; i < dropdown.options.Count; i++)
            {
                var optionText = dropdown.options[i].text;
                if (optionText.ToLower().Trim() == searchText)
                {
                    dropdown.value = i;
                    dropdown.onValueChanged?.Invoke(i);
                    return new { success = true, message = $"Dropdown选项设置成功: '{oldOption}' -> '{optionText}' (精确匹配)" };
                }
            }

            // 模糊匹配
            var matchedOptions = new List<(int index, string text)>();
            for (int i = 0; i < dropdown.options.Count; i++)
            {
                var optionText = dropdown.options[i].text;
                if (optionText.ToLower().Contains(searchText))
                {
                    matchedOptions.Add((i, optionText));
                }
            }

            if (matchedOptions.Count == 1)
            {
                var (matchIndex, matchText) = matchedOptions[0];
                dropdown.value = matchIndex;
                dropdown.onValueChanged?.Invoke(matchIndex);
                return new { success = true, message = $"Dropdown选项设置成功: '{oldOption}' -> '{matchText}' (模糊匹配)" };
            }
            else if (matchedOptions.Count > 1)
            {
                var candidates = matchedOptions.Select(m => $"{m.index}: {m.text}").ToList();
                return new { success = false, error = $"找到多个匹配选项: {string.Join(", ", candidates)}" };
            }

            // 没有匹配
            var allOptions = dropdown.options.Select((o, i) => $"{i}: {o.text}").ToList();
            return new { success = false, error = $"找不到匹配的选项: '{inputText}'。可用选项: {string.Join(", ", allOptions)}" };
        }

        /// <summary>
        /// 获取选项预览
        /// </summary>
        private string GetOptionsPreview(Dropdown dropdown)
        {
            if (dropdown.options.Count == 0)
            {
                return "无选项";
            }

            var preview = new List<string>();
            int showCount = Math.Min(3, dropdown.options.Count);

            for (int i = 0; i < showCount; i++)
            {
                var optionText = dropdown.options[i].text;
                if (i == dropdown.value)
                {
                    preview.Add($"[{optionText}]");
                }
                else
                {
                    preview.Add(optionText);
                }
            }

            if (dropdown.options.Count > 3)
            {
                preview.Add("...");
            }

            return string.Join(", ", preview);
        }

        /// <summary>
        /// 获取支持的交互类型
        /// </summary>
        public override InteractiveType[] GetInteractiveSchema()
        {
            return new InteractiveType[] { InteractiveType.Click, InteractiveType.Input };
        }

        /// <summary>
        /// 获取下拉框元数据
        /// </summary>
        public override Dictionary<string, object> GetMetadata(UnityEngine.Object target, bool includeScreenshot = false)
        {
            var metadata = new Dictionary<string, object>();

            if (!(target is Dropdown dropdown)) return metadata;

            // 当前选中项
            metadata["value"] = dropdown.value;

            // 选项总数
            metadata["optionsCount"] = dropdown.options.Count;

            // 当前选中的选项文本
            if (dropdown.options.Count > 0 && dropdown.value >= 0 && dropdown.value < dropdown.options.Count)
            {
                metadata["currentOption"] = dropdown.options[dropdown.value].text;
            }

            // 是否可交互
            metadata["interactable"] = dropdown.interactable;

            // 所有选项列表
            if (dropdown.options.Count > 0)
            {
                var allOptions = dropdown.options.Select(o => o.text).ToList();
                metadata["allOptions"] = allOptions;
            }

            // 模板信息
            if (dropdown.template != null)
            {
                metadata["hasTemplate"] = true;
                metadata["templateActive"] = dropdown.template.gameObject.activeInHierarchy;
            }

            // 显示当前选中项的文本组件
            var labelText = dropdown.captionText;
            if (labelText != null && !string.IsNullOrEmpty(labelText.text))
            {
                metadata["captionText"] = labelText.text;
            }

            return metadata;
        }
    }
}

