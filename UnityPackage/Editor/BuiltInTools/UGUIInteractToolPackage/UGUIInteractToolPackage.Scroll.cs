using System;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityAIStudio.McpServer.Tools.UGUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ModelContextProtocol.Json.Linq;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// UGUIInteractToolPackage - Scroll 工具实现
    /// 滚动 ScrollRect 组件
    /// </summary>
    public partial class UGUIInteractToolPackage
    {
        /// <summary>
        /// 滚动指定的 ScrollRect 组件
        /// 支持方向滚动和直接跳转到顶部/底部
        /// </summary>
        [McpTool(
            Description = "Scroll a ScrollRect/ScrollView element by its ID (obtained from See tool). Supports directional scrolling (up, down, left, right) and jumping to edges (top, bottom, leftmost, rightmost). Requires Unity to be in Play Mode.",
            Category = "UI Interaction"
        )]
        public async Task<CallToolResult> Scroll(
            [McpParameter("The target ScrollRect element ID (obtained from See tool)")]
            string id,
            [McpParameter("Scroll direction: up, down, left, right, top, bottom, leftmost, rightmost (default: down)")]
            string direction = "down",
            [McpParameter("Scroll amount as percentage (0.0-1.0, default: 0.3 = 30%). Ignored for edge jumping (top/bottom/leftmost/rightmost)")]
            [RangeProcessor(Min = 0.0, Max = 1.0)]
            float amount = 0.3f,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return McpUtils.Error("Required parameter 'id' is missing or empty. Use See tool first to get element IDs.");
            }

            // 验证滚动方向
            var validDirections = new[] { "up", "down", "left", "right", "top", "bottom", "leftmost", "rightmost" };
            if (!Array.Exists(validDirections, d => d.Equals(direction, StringComparison.OrdinalIgnoreCase)))
            {
                return McpUtils.Error($"不支持的滚动方向: {direction}。支持的方向: {string.Join(", ", validDirections)}");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    // 检查是否在播放模式
                    if (!Application.isPlaying)
                    {
                        return McpUtils.Error("Unity当前不在播放状态，无法执行滚动操作。请先进入Play Mode后再尝试。");
                    }

                    // 解析ID获取instanceId
                    var targetComponent = FindComponentById(id);
                    if (targetComponent == null)
                    {
                        return McpUtils.Error($"找不到ID为 {id} 的可交互对象。请使用See工具重新扫描获取最新的对象列表。");
                    }

                    // 检查是否是ScrollRect
                    if (!(targetComponent is ScrollRect))
                    {
                        return McpUtils.Error($"对象 {targetComponent.gameObject.name} 不是ScrollRect组件，无法执行滚动操作。");
                    }

                    // 检查对象是否仍然可交互
                    if (!targetComponent.gameObject.activeInHierarchy)
                    {
                        return McpUtils.Error($"对象 {targetComponent.gameObject.name} 当前不可见或已被禁用。");
                    }

                    // 构建滚动参数
                    var scrollData = new JObject();
                    scrollData["direction"] = direction.ToLower();
                    scrollData["amount"] = amount;

                    // 执行滚动交互
                    var interactResult = UIInteractHelper.Interact(targetComponent, InteractiveType.Scroll, scrollData);

                    // 构建结果
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"# 滚动交互结果");
                    sb.AppendLine($"- 目标对象: {targetComponent.gameObject.name}");
                    sb.AppendLine($"- 对象ID: {id}");
                    sb.AppendLine($"- 滚动方向: {direction}");
                    sb.AppendLine($"- 滚动量: {amount:P0}");
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
                                string message = messageProp?.GetValue(interactResult)?.ToString() ?? "滚动成功";
                                sb.AppendLine($"**结果: 成功**");
                                sb.AppendLine($"- {message}");
                            }
                            else
                            {
                                string error = errorProp?.GetValue(interactResult)?.ToString() ?? "未知错误";
                                return McpUtils.Error($"滚动操作失败: {error}");
                            }
                        }
                    }

                    // 添加使用提示
                    sb.AppendLine();
                    sb.AppendLine("**提示:** 滚动后可使用See工具重新扫描，查看新显示的UI元素。");

                    return McpUtils.Success(sb.ToString());
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"滚动操作失败: {ex.Message}");
                }
            });
        }
    }
}

