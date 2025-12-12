using System;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityAIStudio.McpServer.Tools.UGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// UGUIInteractToolPackage - Input 工具实现
    /// 向 UI 对象输入文本或数值
    /// </summary>
    public partial class UGUIInteractToolPackage
    {
        /// <summary>
        /// 向指定的 UGUI 对象输入文本或数值
        /// 支持 InputField、Slider、Dropdown 等可输入组件
        /// </summary>
        [McpTool(
            Description = "Input text or value into a UGUI element by its ID (obtained from See tool). Supports InputField (text input), Slider (numeric value), and Dropdown (option index or text). Requires Unity to be in Play Mode.",
            Category = "UI Interaction"
        )]
        public async Task<CallToolResult> Input(
            [McpParameter("The target element ID (obtained from See tool)")]
            string id,
            [McpParameter("The text or value to input. For InputField: text content; For Slider: numeric value (e.g., '0.5'); For Dropdown: option index (e.g., '2') or option text (e.g., 'Option A')")]
            string inputText,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return McpUtils.Error("Required parameter 'id' is missing or empty. Use See tool first to get element IDs.");
            }

            if (inputText == null)
            {
                return McpUtils.Error("Required parameter 'inputText' is missing. Please provide the text or value to input.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    // 检查是否在播放模式
                    if (!Application.isPlaying)
                    {
                        return McpUtils.Error("Unity当前不在播放状态，无法执行输入操作。请先进入Play Mode后再尝试。");
                    }

                    // 解析ID获取instanceId
                    var targetComponent = FindComponentById(id);
                    if (targetComponent == null)
                    {
                        return McpUtils.Error($"找不到ID为 {id} 的可交互对象。请使用See工具重新扫描获取最新的对象列表。");
                    }

                    // 检查对象是否仍然可交互
                    if (!IsComponentStillInteractable(targetComponent))
                    {
                        return McpUtils.Error($"对象 {targetComponent.gameObject.name} 当前不可交互（可能已被禁用或隐藏）。");
                    }

                    // 清理输入文本
                    string cleanedInput = CleanInputText(inputText);

                    // 执行输入交互
                    var interactResult = UIInteractHelper.Interact(targetComponent, InteractiveType.Input, cleanedInput);

                    // 构建结果
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"# 输入交互结果");
                    sb.AppendLine($"- 目标对象: {targetComponent.gameObject.name}");
                    sb.AppendLine($"- 对象类型: {targetComponent.GetType().Name}");
                    sb.AppendLine($"- 对象ID: {id}");
                    sb.AppendLine($"- 输入内容: \"{cleanedInput}\"");
                    sb.AppendLine($"- 执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();

                    // 处理返回结果
                    if (interactResult != null)
                    {
                        var resultType = interactResult.GetType();
                        var successProp = resultType.GetProperty("success");
                        var messageProp = resultType.GetProperty("message");
                        var errorProp = resultType.GetProperty("error");

                        if (successProp != null)
                        {
                            bool success = (bool)successProp.GetValue(interactResult);
                            if (success)
                            {
                                string message = messageProp?.GetValue(interactResult)?.ToString() ?? "输入成功";
                                sb.AppendLine($"**结果: 成功**");
                                sb.AppendLine($"- {message}");
                            }
                            else
                            {
                                string error = errorProp?.GetValue(interactResult)?.ToString() ?? "未知错误";
                                return McpUtils.Error($"输入操作失败: {error}");
                            }
                        }
                    }

                    return McpUtils.Success(sb.ToString());
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"输入操作失败: {ex.Message}");
                }
            });
        }

        #region Input 辅助方法

        /// <summary>
        /// 清理和标准化输入文本
        /// </summary>
        private string CleanInputText(string inputValue)
        {
            try
            {
                if (inputValue == null)
                    return "";

                string textStr = inputValue;

                // 处理外层的双引号或单引号
                if (textStr.Length >= 2)
                {
                    if ((textStr.StartsWith("\"") && textStr.EndsWith("\"")) ||
                        (textStr.StartsWith("'") && textStr.EndsWith("'")))
                    {
                        textStr = textStr.Substring(1, textStr.Length - 2);
                    }
                }

                // 处理转义字符
                textStr = textStr.Replace("\\\"", "\"");
                textStr = textStr.Replace("\\'", "'");
                textStr = textStr.Replace("\\n", "\n");
                textStr = textStr.Replace("\\t", "\t");

                return textStr;
            }
            catch
            {
                return inputValue ?? "";
            }
        }

        #endregion
    }
}

