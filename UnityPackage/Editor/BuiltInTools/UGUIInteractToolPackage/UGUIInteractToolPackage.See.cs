using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityAIStudio.McpServer.Tools.UGUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// UGUIInteractToolPackage - See 工具实现
    /// 扫描当前可见且可交互的 UI 对象
    /// </summary>
    public partial class UGUIInteractToolPackage
    {
        /// <summary>
        /// 扫描当前可见且可交互的 UGUI 对象
        /// 返回对象列表及其属性，用于 AI 感知 UI 状态
        /// 可选择包含视口截图用于视觉验证
        /// </summary>
        [McpTool(
            Description = "Scan and list all visible and interactable UGUI elements in the current scene. Returns element IDs, types, positions, and key properties. Use this before click/input/scroll to find target elements. Requires Unity to be in Play Mode.",
            Category = "UI Perception"
        )]
        public async Task<CallToolResult> See(
            [McpParameter("Canvas name filter (optional, partial match)")]
            string canvasFilter = null,
            [McpParameter("Sampling density for raycast scanning (default: 50, range: 10-100)")]
            [RangeProcessor(Min = 10, Max = 100)]
            int sampleDensity = 50,
            [McpParameter("Include a compressed screenshot (default: false). Uses JPEG with downscaling to minimize context usage.")]
            bool includeScreenshot = false,
            [McpParameter("Screenshot scale factor (0.25-1.0, default: 0.5). Lower = smaller file. Only used when includeScreenshot=true.")]
            [RangeProcessor(Min = 0.25f, Max = 1f)]
            float screenshotScale = 0.5f,
            [McpParameter("JPEG quality (10-80, default: 40). Lower = smaller file. Only used when includeScreenshot=true.")]
            [RangeProcessor(Min = 10, Max = 80)]
            int jpegQuality = 40,
            CancellationToken ct = default)
        {
            // 如果需要截图，使用异步版本
            if (includeScreenshot)
            {
                return await SeeWithScreenshotAsync(canvasFilter, sampleDensity, screenshotScale, jpegQuality, ct);
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                return ExecuteSeeLogic(canvasFilter, sampleDensity, null);
            });
        }

        /// <summary>
        /// 带截图的异步See实现
        /// </summary>
        private async Task<CallToolResult> SeeWithScreenshotAsync(string canvasFilter, int sampleDensity, float screenshotScale, int jpegQuality, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<CallToolResult>();

            await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    // 检查是否在播放模式
                    if (!Application.isPlaying)
                    {
                        tcs.SetResult(McpUtils.Error("Unity当前不在播放状态，无法执行UI扫描。请先进入Play Mode后再尝试。"));
                        return (object)null;
                    }

                    // 先执行截图（异步，使用压缩参数减少上下文占用）
                    ScreenshotHelper.CaptureGameViewAsync(screenshotResult =>
                    {
                        try
                        {
                            // 在截图完成后执行扫描逻辑
                            var scanResult = ExecuteSeeLogic(canvasFilter, sampleDensity, screenshotResult);
                            tcs.SetResult(scanResult);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetResult(McpUtils.Error($"UI扫描失败: {ex.Message}"));
                        }
                    }, screenshotScale, jpegQuality);
                }
                catch (Exception ex)
                {
                    tcs.SetResult(McpUtils.Error($"启动截图失败: {ex.Message}"));
                }

                return (object)null;
            });

            return await tcs.Task;
        }

        /// <summary>
        /// 执行See扫描逻辑
        /// </summary>
        private CallToolResult ExecuteSeeLogic(string canvasFilter, int sampleDensity, ScreenshotResult screenshot)
        {
            try
            {
                // 检查是否在播放模式
                if (!Application.isPlaying)
                {
                    return McpUtils.Error("Unity当前不在播放状态，无法执行UI扫描。请先进入Play Mode后再尝试。");
                }

                // 检查 EventSystem
                if (EventSystem.current == null)
                {
                    return McpUtils.Error("EventSystem不存在，无法进行UI射线检测。请确保场景中有EventSystem组件。");
                }

                var result = new List<UIInteractHelper.InteractableSchema>();
                var discoveredObjects = new HashSet<int>();

                // 获取屏幕尺寸
                var screenWidth = Screen.width;
                var screenHeight = Screen.height;

                // 计算采样步长
                var stepX = screenWidth / (float)sampleDensity;
                var stepY = screenHeight / (float)sampleDensity;

                // 在屏幕上进行等距采样
                for (int x = 0; x < sampleDensity; x++)
                {
                    for (int y = 0; y < sampleDensity; y++)
                    {
                        // 计算采样点屏幕坐标
                        var screenX = x * stepX + stepX * 0.5f;
                        var screenY = y * stepY + stepY * 0.5f;
                        var screenPoint = new Vector2(screenX, screenY);

                        // 进行全局UI射线检测
                        var hitObjects = PerformGlobalUIRaycast(screenPoint);

                        foreach (var hitObject in hitObjects)
                        {
                            // 如果有Canvas过滤，检查对象所属的Canvas
                            if (!string.IsNullOrEmpty(canvasFilter))
                            {
                                var canvas = hitObject.GetComponentInParent<Canvas>();
                                if (canvas == null || !canvas.name.Contains(canvasFilter))
                                    continue;
                            }

                            // 判断是否可交互并获取组件信息
                            var interactableInfo = IsObjectInteractable(hitObject);
                            if (interactableInfo.IsInteractable)
                            {
                                var instanceId = interactableInfo.Component.GetInstanceID();
                                if (!discoveredObjects.Contains(instanceId))
                                {
                                    var wrapper = UIInteractHelper.GetWrapper(interactableInfo.ComponentType);
                                    if (wrapper != null)
                                    {
                                        discoveredObjects.Add(instanceId);

                                        var schema = new UIInteractHelper.InteractableSchema
                                        {
                                            TargetType = interactableInfo.ComponentType.Name,
                                            TargetName = interactableInfo.Component.gameObject.name,
                                            TargetId = instanceId.ToString(),
                                            SupportedInteractions = wrapper.GetInteractiveSchema().Select(t => t.ToString()).ToArray(),
                                            Description = $"{interactableInfo.Component.gameObject.name}的{interactableInfo.ComponentType.Name}组件"
                                        };

                                        // 添加组件特定的元数据
                                        AddUIObjectMetadata(interactableInfo.Component, schema, wrapper);

                                        result.Add(schema);

                                        // 向上检查并补充祖先链中的ScrollRect
                                        TryAddAncestorScrollRects(interactableInfo.Component.gameObject, canvasFilter, discoveredObjects, result);
                                    }
                                }
                            }
                        }
                    }
                }

                // 构建结果文本（精简输出）
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"UGUI扫描 | 分辨率:{screenWidth}x{screenHeight} | 对象数:{result.Count}");

                if (result.Count == 0)
                {
                    sb.AppendLine("未发现可交互UI。确保Unity在Play模式且场景有可见UI。");
                }
                else
                {
                    sb.AppendLine();
                    for (int i = 0; i < result.Count; i++)
                    {
                        var obj = result[i];

                        // 构建紧凑的对象信息行
                        var info = new List<string>();
                        info.Add($"ID:{obj.TargetId}");

                        // 添加位置信息
                        if (obj.Metadata.TryGetValue("position", out var posObj))
                        {
                            var posType = posObj.GetType();
                            var xProp = posType.GetProperty("x");
                            var yProp = posType.GetProperty("y");
                            if (xProp != null && yProp != null)
                            {
                                var x = Convert.ToSingle(xProp.GetValue(posObj));
                                var y = Convert.ToSingle(yProp.GetValue(posObj));
                                info.Add($"pos:({x:F0},{y:F0})");
                            }
                        }

                        // 添加尺寸信息
                        if (obj.Metadata.TryGetValue("size", out var sizeObj))
                        {
                            var sizeType = sizeObj.GetType();
                            var wProp = sizeType.GetProperty("width");
                            var hProp = sizeType.GetProperty("height");
                            if (wProp != null && hProp != null)
                            {
                                var w = Convert.ToSingle(wProp.GetValue(sizeObj));
                                var h = Convert.ToSingle(hProp.GetValue(sizeObj));
                                info.Add($"size:({w:F0}x{h:F0})");
                            }
                        }

                        // 添加关键属性
                        if (obj.Metadata.TryGetValue("text", out var text) && text != null && !string.IsNullOrEmpty(text.ToString()))
                            info.Add($"text:\"{text}\"");
                        if (obj.Metadata.TryGetValue("isOn", out var isOn))
                            info.Add($"isOn:{isOn}");
                        if (obj.Metadata.TryGetValue("value", out var value))
                            info.Add($"value:{value}");
                        if (obj.Metadata.TryGetValue("currentOption", out var option))
                            info.Add($"option:\"{option}\"");
                        if (obj.Metadata.TryGetValue("placeholder", out var ph) && ph != null)
                            info.Add($"placeholder:\"{ph}\"");

                        // 输出紧凑格式
                        sb.AppendLine($"[{i + 1}] {obj.TargetType}:{obj.TargetName} | {string.Join(" | ", info)}");
                    }
                }

                // 返回结果（如果有截图，以图文混排方式返回）
                if (screenshot != null && screenshot.Success)
                {
                    // 添加压缩信息到输出
                    sb.AppendLine($"\n[Screenshot: {screenshot.CompressionInfo}]");
                    return McpUtils.SuccessWithImage(sb.ToString(), screenshot.Base64Data, screenshot.Format ?? "jpeg");
                }

                return McpUtils.Success(sb.ToString());
            }
            catch (Exception ex)
            {
                return McpUtils.Error($"UI扫描失败: {ex.Message}");
            }
        }

        #region 辅助方法

        /// <summary>
        /// 在指定屏幕点进行全局UI射线检测
        /// </summary>
        private List<GameObject> PerformGlobalUIRaycast(Vector2 screenPoint)
        {
            var hitObjects = new List<GameObject>();

            try
            {
                var eventData = new PointerEventData(EventSystem.current)
                {
                    position = screenPoint
                };

                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                // 只取第一个命中的GameObject（最前面的）
                if (results.Count > 0 && results[0].gameObject != null)
                {
                    hitObjects.Add(results[0].gameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGUIInteractToolPackage] 射线检测失败: {ex.Message}");
            }

            return hitObjects;
        }

        /// <summary>
        /// 可交互对象信息
        /// </summary>
        private struct InteractableInfo
        {
            public bool IsInteractable;
            public Component Component;
            public Type ComponentType;
        }

        /// <summary>
        /// 判断对象是否可交互
        /// </summary>
        private InteractableInfo IsObjectInteractable(GameObject obj)
        {
            var supportedTypes = UIInteractHelper.GetSupportedUITypes();

            // 如果是可交互对象列表内的，直接判断为true
            foreach (var supportedType in supportedTypes)
            {
                var component = obj.GetComponent(supportedType);
                if (component != null)
                {
                    return new InteractableInfo
                    {
                        IsInteractable = true,
                        Component = component,
                        ComponentType = supportedType
                    };
                }
            }

            // 如果不在可交互列表内，沿着parent向上找
            if (obj.GetComponent<UIBehaviour>() != null)
            {
                var current = obj.transform;

                while (current != null)
                {
                    if (current.GetComponent<Canvas>() != null)
                    {
                        break;
                    }

                    foreach (var supportedType in supportedTypes)
                    {
                        var component = current.GetComponent(supportedType);
                        if (component != null)
                        {
                            if (component is Selectable selectable && !selectable.interactable)
                            {
                                continue;
                            }

                            return new InteractableInfo
                            {
                                IsInteractable = true,
                                Component = component,
                                ComponentType = supportedType
                            };
                        }
                    }

                    current = current.parent;
                }
            }

            return new InteractableInfo
            {
                IsInteractable = false,
                Component = null,
                ComponentType = null
            };
        }

        /// <summary>
        /// 添加UI对象的元数据
        /// </summary>
        private void AddUIObjectMetadata(Component component, UIInteractHelper.InteractableSchema schema, IUIBehaviourWrapper wrapper)
        {
            var gameObject = component.gameObject;
            var rectTransform = gameObject.GetComponent<RectTransform>();

            // 基础元数据
            schema.Metadata["active"] = gameObject.activeInHierarchy;
            schema.Metadata["instanceId"] = component.GetInstanceID();

            if (rectTransform != null)
            {
                // 位置信息
                var worldPos = rectTransform.position;
                schema.Metadata["position"] = new { x = worldPos.x, y = worldPos.y };

                // 尺寸信息
                var size = rectTransform.rect.size;
                schema.Metadata["size"] = new { width = size.x, height = size.y };
            }

            // 使用包装器获取组件特定的元数据
            var componentMetadata = wrapper.GetMetadata(component, false);
            foreach (var kvp in componentMetadata)
            {
                schema.Metadata[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 检查并补充祖先链中的ScrollRect
        /// </summary>
        private void TryAddAncestorScrollRects(
            GameObject source,
            string canvasFilter,
            HashSet<int> discoveredObjects,
            List<UIInteractHelper.InteractableSchema> result)
        {
            if (source == null) return;
            var current = source.transform;

            while (current != null)
            {
                if (current.GetComponent<Canvas>() != null)
                    break;

                var sr = current.GetComponent<ScrollRect>();
                if (sr != null && sr.isActiveAndEnabled)
                {
                    // Canvas过滤
                    if (!string.IsNullOrEmpty(canvasFilter))
                    {
                        var canvas = sr.GetComponentInParent<Canvas>();
                        if (canvas == null || !canvas.name.Contains(canvasFilter))
                            goto NEXT_PARENT;
                    }

                    // 检查是否可滚动
                    if (!IsScrollRectActuallyScrollable(sr))
                        goto NEXT_PARENT;

                    int id = sr.GetInstanceID();
                    if (!discoveredObjects.Contains(id))
                    {
                        var wrapper = UIInteractHelper.GetWrapper(typeof(ScrollRect));
                        if (wrapper != null)
                        {
                            discoveredObjects.Add(id);
                            var schema = new UIInteractHelper.InteractableSchema
                            {
                                TargetType = nameof(ScrollRect),
                                TargetName = sr.gameObject.name,
                                TargetId = id.ToString(),
                                SupportedInteractions = wrapper.GetInteractiveSchema().Select(t => t.ToString()).ToArray(),
                                Description = $"{sr.gameObject.name}的ScrollRect组件"
                            };
                            AddUIObjectMetadata(sr, schema, wrapper);
                            result.Add(schema);
                        }
                    }
                }

                NEXT_PARENT:
                current = current.parent;
            }
        }

        /// <summary>
        /// 判断ScrollRect是否具有实际滚动能力
        /// </summary>
        private bool IsScrollRectActuallyScrollable(ScrollRect sr)
        {
            try
            {
                if (sr == null || !sr.isActiveAndEnabled) return false;
                var viewport = sr.viewport != null ? sr.viewport : sr.GetComponent<RectTransform>();
                if (viewport == null) return false;
                var content = sr.content;
                bool canScroll = false;
                if (content != null)
                {
                    var vp = viewport.rect.size;
                    var ct = content.rect.size;
                    if (sr.vertical && ct.y > vp.y) canScroll = true;
                    if (sr.horizontal && ct.x > vp.x) canScroll = true;
                }
                if (sr.verticalScrollbar != null || sr.horizontalScrollbar != null) canScroll = true;
                return canScroll;
            }
            catch { return false; }
        }

        #endregion
    }
}

