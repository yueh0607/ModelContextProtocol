using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// Button 组件包装器
    /// 支持点击交互
    /// </summary>
    public class ButtonWrapper : UIBehaviourWrapper<Button>
    {
        /// <summary>
        /// 执行按钮交互
        /// </summary>
        public override object Interact(UnityEngine.Object target, InteractiveType type, object data)
        {
            if (!(target is Button btn))
            {
                return new { success = false, error = "目标对象不是Button" };
            }

            switch (type)
            {
                case InteractiveType.Click:
                    PointerEventData eventData = new PointerEventData(EventSystem.current);
                    ExecuteEvents.Execute(btn.gameObject, eventData, ExecuteEvents.pointerClickHandler);
                    return new { success = true, message = "Button Clicked" };

                default:
                    return new { success = false, error = "Invalid InteractiveType - " + type.ToString() };
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
        /// 获取按钮元数据
        /// </summary>
        public override Dictionary<string, object> GetMetadata(UnityEngine.Object target, bool includeScreenshot = false)
        {
            var metadata = new Dictionary<string, object>();

            if (!(target is Button button)) return metadata;

            // 按钮文本内容
            var buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null && !string.IsNullOrEmpty(buttonText.text))
            {
                metadata["text"] = buttonText.text;
            }

            // 可交互状态
            metadata["interactable"] = button.interactable;

            return metadata;
        }
    }
}

