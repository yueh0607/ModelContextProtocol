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
    /// UGUIInteractToolPackage - Click 工具实现
    /// 点击 UI 对象
    /// </summary>
    public partial class UGUIInteractToolPackage
    {
        /// <summary>
        /// 点击指定的 UGUI 对象
        /// 使用 See 工具获取的 ID 来定位目标对象
        /// </summary>
        [McpTool(
            Description = "Click a UGUI element by its ID (obtained from See tool). Supports Button, Toggle, Dropdown, and other clickable components. Requires Unity to be in Play Mode.",
            Category = "UI Interaction"
        )]
        public async Task<CallToolResult> Click(
            [McpParameter("The target element ID (obtained from See tool)")]
            string id,
            [McpParameter("Interaction type (default: Click). For Toggle: Click to toggle state.")]
            string interactionType = "Click",
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return McpUtils.Error("Required parameter 'id' is missing or empty. Use See tool first to get element IDs.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    // 检查是否在播放模式
                    if (!Application.isPlaying)
                    {
                        return McpUtils.Error("Unity当前不在播放状态，无法执行点击操作。请先进入Play Mode后再尝试。");
                    }

                    // 解析ID获取instanceId
                    var targetComponent = FindComponentById(id);
                    if (targetComponent == null)
                    {
                        return McpUtils.Error($"找不到ID为 {id} 的可交互对象。请使用See工具重新扫描获取最新的对象列表。");
                    }

                    // 解析交互类型
                    if (!Enum.TryParse<InteractiveType>(interactionType, true, out var parsedInteractionType))
                    {
                        return McpUtils.Error($"不支持的交互类型: {interactionType}。支持的类型: Click, Input, Scroll");
                    }

                    // 检查对象是否仍然可交互
                    if (!IsComponentStillInteractable(targetComponent))
                    {
                        return McpUtils.Error($"对象 {targetComponent.gameObject.name} 当前不可交互（可能已被禁用或隐藏）。");
                    }

                    // 执行交互
                    var interactResult = UIInteractHelper.Interact(targetComponent, parsedInteractionType, null);

                    // 构建结果
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"# 点击交互结果");
                    sb.AppendLine($"- 目标对象: {targetComponent.gameObject.name}");
                    sb.AppendLine($"- 对象类型: {targetComponent.GetType().Name}");
                    sb.AppendLine($"- 对象ID: {id}");
                    sb.AppendLine($"- 交互类型: {interactionType}");
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
                                string message = messageProp?.GetValue(interactResult)?.ToString() ?? "操作成功";
                                sb.AppendLine($"**结果: 成功**");
                                sb.AppendLine($"- {message}");
                            }
                            else
                            {
                                string error = errorProp?.GetValue(interactResult)?.ToString() ?? "未知错误";
                                return McpUtils.Error($"点击操作失败: {error}");
                            }
                        }
                    }

                    return McpUtils.Success(sb.ToString());
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"点击操作失败: {ex.Message}");
                }
            });
        }

        #region Click 辅助方法

        /// <summary>
        /// 根据ID查找UI组件
        /// </summary>
        private Component FindComponentById(string targetId)
        {
            try
            {
                if (!int.TryParse(targetId, out int instanceId))
                {
                    Debug.LogError($"[UGUIInteractToolPackage] 无法解析instanceId: {targetId}");
                    return null;
                }

                // 使用Unity的API根据instanceId查找对象
                var obj = EditorUtility.InstanceIDToObject(instanceId);
                if (obj is Component component)
                {
                    return component;
                }
                else if (obj is GameObject gameObject)
                {
                    // 如果找到的是GameObject，尝试获取支持的UI组件
                    var supportedTypes = UIInteractHelper.GetSupportedUITypes();
                    foreach (var type in supportedTypes)
                    {
                        var targetComponent = gameObject.GetComponent(type);
                        if (targetComponent != null)
                        {
                            return targetComponent;
                        }
                    }
                }

                Debug.LogWarning($"[UGUIInteractToolPackage] 找到对象但不是有效的UI组件: {obj?.GetType().Name}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGUIInteractToolPackage] 查找组件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查组件是否仍然可交互
        /// </summary>
        private bool IsComponentStillInteractable(Component component)
        {
            if (component == null || component.gameObject == null)
                return false;

            // 检查GameObject是否激活
            if (!component.gameObject.activeInHierarchy)
                return false;

            // 检查组件是否仍是可交互类型
            var supportedTypes = UIInteractHelper.GetSupportedUITypes();
            bool isSupported = false;
            foreach (var type in supportedTypes)
            {
                if (type.IsAssignableFrom(component.GetType()))
                {
                    isSupported = true;
                    break;
                }
            }

            if (!isSupported)
                return false;

            // 对于Selectable组件，检查interactable属性
            if (component is Selectable selectable)
            {
                return selectable.interactable;
            }

            return true;
        }

        #endregion
    }
}

