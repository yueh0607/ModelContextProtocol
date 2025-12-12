using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// InputField 组件包装器
    /// 支持文本输入交互
    /// </summary>
    public class InputFieldWrapper : UIBehaviourWrapper<InputField>
    {
        /// <summary>
        /// 执行输入框交互
        /// </summary>
        public override object Interact(UnityEngine.Object target, InteractiveType type, object data)
        {
            if (!(target is InputField inputField))
            {
                return new { success = false, error = "目标对象不是InputField" };
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

                        string inputText = data.ToString();
                        string originalText = inputField.text;

                        // 自动获得焦点
                        if (!inputField.isFocused)
                        {
                            inputField.Select();
                            inputField.ActivateInputField();
                        }

                        // 设置新文本
                        inputField.text = inputText;

                        // 触发值变化事件
                        inputField.onValueChanged?.Invoke(inputText);
                        inputField.onEndEdit?.Invoke(inputText);

                        return new { success = true, message = $"文本输入成功: '{originalText}' -> '{inputText}'" };
                    }
                    catch (Exception ex)
                    {
                        return new { success = false, error = $"输入文本失败: {ex.Message}" };
                    }

                default:
                    return new { success = false, error = $"InputField不支持交互类型: {type}" };
            }
        }

        /// <summary>
        /// 获取支持的交互类型
        /// </summary>
        public override InteractiveType[] GetInteractiveSchema()
        {
            return new InteractiveType[] { InteractiveType.Input };
        }

        /// <summary>
        /// 获取输入框元数据
        /// </summary>
        public override Dictionary<string, object> GetMetadata(UnityEngine.Object target, bool includeScreenshot = false)
        {
            var metadata = new Dictionary<string, object>();

            if (!(target is InputField inputField)) return metadata;

            // 当前文本内容
            if (!string.IsNullOrEmpty(inputField.text))
            {
                metadata["text"] = inputField.text;
            }

            // 占位符文本
            if (inputField.placeholder != null)
            {
                var placeholderText = inputField.placeholder.GetComponent<Text>();
                if (placeholderText != null && !string.IsNullOrEmpty(placeholderText.text))
                {
                    metadata["placeholder"] = placeholderText.text;
                }
            }

            // 输入限制信息
            if (inputField.characterLimit > 0)
            {
                metadata["characterLimit"] = inputField.characterLimit;
                metadata["currentLength"] = inputField.text?.Length ?? 0;
            }

            // 输入类型
            metadata["contentType"] = inputField.contentType.ToString();
            metadata["inputType"] = inputField.inputType.ToString();

            // 是否为密码字段
            if (inputField.contentType == InputField.ContentType.Password ||
                inputField.inputType == InputField.InputType.Password)
            {
                metadata["isPassword"] = true;
            }

            // 是否为多行输入
            if (inputField.lineType == InputField.LineType.MultiLineNewline ||
                inputField.lineType == InputField.LineType.MultiLineSubmit)
            {
                metadata["multiline"] = true;
            }

            // 是否只读
            metadata["readOnly"] = inputField.readOnly;
            metadata["interactable"] = inputField.interactable;

            return metadata;
        }
    }
}

