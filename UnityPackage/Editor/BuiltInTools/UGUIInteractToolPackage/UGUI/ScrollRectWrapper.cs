using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModelContextProtocol.Json.Linq;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// ScrollRect 组件包装器
    /// 支持滚动交互
    /// </summary>
    public class ScrollRectWrapper : UIBehaviourWrapper<ScrollRect>
    {
        /// <summary>
        /// 执行滚动视图交互
        /// </summary>
        public override object Interact(UnityEngine.Object target, InteractiveType type, object data)
        {
            if (!(target is ScrollRect scrollRect))
            {
                return new { success = false, error = "目标对象不是ScrollRect" };
            }

            switch (type)
            {
                case InteractiveType.Scroll:
                    try
                    {
                        return HandleScrollInteraction(scrollRect, data);
                    }
                    catch (Exception ex)
                    {
                        return new { success = false, error = $"滚动操作失败: {ex.Message}" };
                    }

                default:
                    return new { success = false, error = $"ScrollRect不支持交互类型: {type}" };
            }
        }

        /// <summary>
        /// 处理滚动交互
        /// </summary>
        private object HandleScrollInteraction(ScrollRect scrollRect, object data)
        {
            var scrollParams = ParseScrollData(data);

            // 记录原始位置
            var originalVertical = scrollRect.verticalNormalizedPosition;
            var originalHorizontal = scrollRect.horizontalNormalizedPosition;

            bool scrolled = false;
            string scrollAction = "";

            switch (scrollParams.Direction.ToLower())
            {
                case "up":
                    if (scrollRect.vertical)
                    {
                        var newPos = Mathf.Clamp01(originalVertical + scrollParams.Amount);
                        scrollRect.verticalNormalizedPosition = newPos;
                        scrolled = newPos != originalVertical;
                        scrollAction = $"向上滚动 {scrollParams.Amount:F2}";
                    }
                    break;

                case "down":
                    if (scrollRect.vertical)
                    {
                        var newPos = Mathf.Clamp01(originalVertical - scrollParams.Amount);
                        scrollRect.verticalNormalizedPosition = newPos;
                        scrolled = newPos != originalVertical;
                        scrollAction = $"向下滚动 {scrollParams.Amount:F2}";
                    }
                    break;

                case "left":
                    if (scrollRect.horizontal)
                    {
                        var newPos = Mathf.Clamp01(originalHorizontal - scrollParams.Amount);
                        scrollRect.horizontalNormalizedPosition = newPos;
                        scrolled = newPos != originalHorizontal;
                        scrollAction = $"向左滚动 {scrollParams.Amount:F2}";
                    }
                    break;

                case "right":
                    if (scrollRect.horizontal)
                    {
                        var newPos = Mathf.Clamp01(originalHorizontal + scrollParams.Amount);
                        scrollRect.horizontalNormalizedPosition = newPos;
                        scrolled = newPos != originalHorizontal;
                        scrollAction = $"向右滚动 {scrollParams.Amount:F2}";
                    }
                    break;

                case "top":
                    if (scrollRect.vertical)
                    {
                        scrollRect.verticalNormalizedPosition = 1.0f;
                        scrolled = originalVertical != 1.0f;
                        scrollAction = "滚动到顶部";
                    }
                    break;

                case "bottom":
                    if (scrollRect.vertical)
                    {
                        scrollRect.verticalNormalizedPosition = 0.0f;
                        scrolled = originalVertical != 0.0f;
                        scrollAction = "滚动到底部";
                    }
                    break;

                case "leftmost":
                    if (scrollRect.horizontal)
                    {
                        scrollRect.horizontalNormalizedPosition = 0.0f;
                        scrolled = originalHorizontal != 0.0f;
                        scrollAction = "滚动到最左";
                    }
                    break;

                case "rightmost":
                    if (scrollRect.horizontal)
                    {
                        scrollRect.horizontalNormalizedPosition = 1.0f;
                        scrolled = originalHorizontal != 1.0f;
                        scrollAction = "滚动到最右";
                    }
                    break;

                default:
                    return new { success = false, error = $"不支持的滚动方向: {scrollParams.Direction}" };
            }

            // 强制刷新布局
            Canvas.ForceUpdateCanvases();

            var newVertical = scrollRect.verticalNormalizedPosition;
            var newHorizontal = scrollRect.horizontalNormalizedPosition;

            if (!scrolled)
            {
                return new { success = true, message = $"滚动操作完成: {scrollAction} (位置未变化)" };
            }

            return new { success = true, message = $"滚动操作完成: {scrollAction} | 位置: V({originalVertical:F2}→{newVertical:F2}) H({originalHorizontal:F2}→{newHorizontal:F2})" };
        }

        /// <summary>
        /// 解析滚动数据
        /// </summary>
        private ScrollParams ParseScrollData(object data)
        {
            var defaultParams = new ScrollParams { Direction = "down", Amount = 0.3f };

            if (data == null)
                return defaultParams;

            try
            {
                // 如果是字符串，只包含方向
                if (data is string direction)
                {
                    return new ScrollParams { Direction = direction, Amount = 0.3f };
                }

                // 如果是JObject，解析详细参数
                if (data is JObject jObj)
                {
                    var result = new ScrollParams();
                    result.Direction = jObj["direction"]?.ToString() ?? "down";

                    if (jObj["amount"] != null)
                    {
                        if (float.TryParse(jObj["amount"].ToString(), out float amount))
                        {
                            result.Amount = Mathf.Clamp01(amount);
                        }
                    }
                    else
                    {
                        result.Amount = 0.3f;
                    }

                    return result;
                }

                return defaultParams;
            }
            catch
            {
                return defaultParams;
            }
        }

        /// <summary>
        /// 获取支持的交互类型
        /// </summary>
        public override InteractiveType[] GetInteractiveSchema()
        {
            return new InteractiveType[] { InteractiveType.Scroll };
        }

        /// <summary>
        /// 获取滚动视图元数据
        /// </summary>
        public override Dictionary<string, object> GetMetadata(UnityEngine.Object target, bool includeScreenshot = false)
        {
            var metadata = new Dictionary<string, object>();

            if (!(target is ScrollRect scrollRect)) return metadata;

            // 滚动能力
            metadata["horizontal"] = scrollRect.horizontal;
            metadata["vertical"] = scrollRect.vertical;

            // 当前位置
            metadata["verticalPosition"] = Math.Round(scrollRect.verticalNormalizedPosition, 3);
            metadata["horizontalPosition"] = Math.Round(scrollRect.horizontalNormalizedPosition, 3);

            // 滚动范围和内容信息
            if (scrollRect.content != null)
            {
                var viewport = scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>();
                if (viewport != null)
                {
                    var contentSize = scrollRect.content.rect.size;
                    var viewportSize = viewport.rect.size;

                    metadata["contentWidth"] = Math.Round(contentSize.x, 1);
                    metadata["contentHeight"] = Math.Round(contentSize.y, 1);
                    metadata["viewportWidth"] = Math.Round(viewportSize.x, 1);
                    metadata["viewportHeight"] = Math.Round(viewportSize.y, 1);

                    // 可滚动性检查
                    metadata["canScrollVertically"] = scrollRect.vertical && contentSize.y > viewportSize.y;
                    metadata["canScrollHorizontally"] = scrollRect.horizontal && contentSize.x > viewportSize.x;
                }
            }

            // 滚动条信息
            if (scrollRect.verticalScrollbar != null)
            {
                metadata["hasVerticalScrollbar"] = true;
                metadata["verticalScrollbarValue"] = Math.Round(scrollRect.verticalScrollbar.value, 3);
            }

            if (scrollRect.horizontalScrollbar != null)
            {
                metadata["hasHorizontalScrollbar"] = true;
                metadata["horizontalScrollbarValue"] = Math.Round(scrollRect.horizontalScrollbar.value, 3);
            }

            // 滚动设置
            metadata["movementType"] = scrollRect.movementType.ToString();
            metadata["elasticity"] = scrollRect.elasticity;
            metadata["inertia"] = scrollRect.inertia;
            metadata["tips"] = "滚动列表可能有当前看不到的元素";

            if (scrollRect.inertia)
            {
                metadata["decelerationRate"] = scrollRect.decelerationRate;
            }

            metadata["scrollSensitivity"] = scrollRect.scrollSensitivity;

            return metadata;
        }

        /// <summary>
        /// 滚动参数结构
        /// </summary>
        private struct ScrollParams
        {
            public string Direction;
            public float Amount;
        }
    }
}

