using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// Toggle 组件包装器
    /// 支持点击切换状态交互
    /// </summary>
    public class ToggleWrapper : UIBehaviourWrapper<Toggle>
    {
        /// <summary>
        /// 执行开关交互
        /// </summary>
        public override object Interact(UnityEngine.Object target, InteractiveType type, object data)
        {
            if (!(target is Toggle toggle))
            {
                return new { success = false, error = "目标对象不是Toggle" };
            }

            switch (type)
            {
                case InteractiveType.Click:
                    try
                    {
                        bool originalState = toggle.isOn;

                        // 模拟点击事件
                        PointerEventData eventData = new PointerEventData(EventSystem.current);
                        ExecuteEvents.Execute(toggle.gameObject, eventData, ExecuteEvents.pointerClickHandler);

                        bool newState = toggle.isOn;

                        // 检查是否在ToggleGroup中
                        string groupInfo = "";
                        if (toggle.group != null)
                        {
                            groupInfo = $" (ToggleGroup: {toggle.group.name})";

                            // 如果是ToggleGroup且不允许切换到false
                            if (!toggle.group.allowSwitchOff && originalState && !newState)
                            {
                                return new { success = true, message = $"Toggle状态保持: {originalState} (ToggleGroup不允许全部关闭){groupInfo}" };
                            }
                        }

                        return new { success = true, message = $"Toggle状态切换: {originalState} -> {newState}{groupInfo}" };
                    }
                    catch (Exception ex)
                    {
                        return new { success = false, error = $"Toggle点击失败: {ex.Message}" };
                    }

                default:
                    return new { success = false, error = $"Toggle不支持交互类型: {type}" };
            }
        }

        /// <summary>
        /// 获取支持的交互类型
        /// </summary>
        public override InteractiveType[] GetInteractiveSchema()
        {
            return new InteractiveType[] { InteractiveType.Click };
        }

        /// <summary>
        /// 获取开关元数据
        /// </summary>
        public override Dictionary<string, object> GetMetadata(UnityEngine.Object target, bool includeScreenshot = false)
        {
            var metadata = new Dictionary<string, object>();

            if (!(target is Toggle toggle)) return metadata;

            // Toggle状态
            metadata["isOn"] = toggle.isOn;

            // 标签文本
            var label = toggle.GetComponentInChildren<Text>();
            if (label != null && !string.IsNullOrEmpty(label.text))
            {
                metadata["text"] = label.text;
            }

            // ToggleGroup信息
            if (toggle.group != null)
            {
                metadata["hasGroup"] = true;
                metadata["groupName"] = toggle.group.name;
                metadata["allowSwitchOff"] = toggle.group.allowSwitchOff;

                // 获取组中活动的Toggle数量
                int activeTogglesInGroup = 0;
                foreach (var groupToggle in toggle.group.ActiveToggles())
                {
                    activeTogglesInGroup++;
                }
                metadata["activeTogglesInGroup"] = activeTogglesInGroup;

                // 检查当前Toggle是否是组中唯一活动的
                if (toggle.isOn && activeTogglesInGroup == 1)
                {
                    metadata["isOnlyActiveInGroup"] = true;
                }
            }
            else
            {
                metadata["hasGroup"] = false;
            }

            // 图形状态信息
            if (toggle.graphic != null)
            {
                metadata["hasGraphic"] = true;
                if (toggle.graphic is Image image && image.sprite != null)
                {
                    metadata["spriteName"] = image.sprite.name;
                }
            }

            // 是否可交互
            metadata["interactable"] = toggle.interactable;
            metadata["toggleTransition"] = toggle.toggleTransition.ToString();

            return metadata;
        }
    }
}

