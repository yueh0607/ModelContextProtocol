using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMCP.Examples
{
    /// <summary>
    /// 示例工具：写入Unity日志
    /// </summary>
    public class UnityLogTool : McpTool
    {
        public override string Name => "unity_log";
        public override string Description => "Write a message to Unity's console log";

        public override JObject GetInputSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["message"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The message to log"
                    },
                    ["level"] = new JObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JArray { "info", "warning", "error" },
                        ["description"] = "Log level (info, warning, or error)",
                        ["default"] = "info"
                    }
                },
                ["required"] = new JArray { "message" }
            };
        }

        public override Task<CallToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            if (!arguments.TryGetValue("message", out var messageObj))
            {
                return Task.FromResult(CreateTextResult("Missing required parameter: message", true));
            }

            var message = messageObj.ToString();
            var level = arguments.TryGetValue("level", out var levelObj) ? levelObj.ToString() : "info";

            // 在主线程执行
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                switch (level.ToLower())
                {
                    case "warning":
                        Debug.LogWarning(message);
                        break;
                    case "error":
                        Debug.LogError(message);
                        break;
                    default:
                        Debug.Log(message);
                        break;
                }
            });

            return Task.FromResult(CreateTextResult($"Logged message with level: {level}"));
        }
    }

    /// <summary>
    /// 示例工具：查找和操作GameObject
    /// </summary>
    public class UnityGameObjectTool : McpTool
    {
        public override string Name => "unity_find_gameobject";
        public override string Description => "Find GameObjects in the scene by name or tag";

        public override JObject GetInputSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["searchType"] = new JObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JArray { "name", "tag" },
                        ["description"] = "Search by name or tag"
                    },
                    ["searchValue"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The name or tag to search for"
                    }
                },
                ["required"] = new JArray { "searchType", "searchValue" }
            };
        }

        public override async Task<CallToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            if (!arguments.TryGetValue("searchType", out var searchTypeObj) ||
                !arguments.TryGetValue("searchValue", out var searchValueObj))
            {
                return CreateTextResult("Missing required parameters", true);
            }

            var searchType = searchTypeObj.ToString();
            var searchValue = searchValueObj.ToString();

            var taskCompletionSource = new TaskCompletionSource<CallToolResult>();

            UnityMainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    GameObject[] objects;

                    if (searchType.Equals("tag", StringComparison.OrdinalIgnoreCase))
                    {
                        objects = GameObject.FindGameObjectsWithTag(searchValue);
                    }
                    else
                    {
                        var allObjects = GameObject.FindObjectsOfType<GameObject>();
                        objects = allObjects.Where(go => go.name.Contains(searchValue)).ToArray();
                    }

                    if (objects.Length == 0)
                    {
                        taskCompletionSource.SetResult(CreateTextResult($"No GameObjects found with {searchType}: {searchValue}"));
                        return;
                    }

                    var result = $"Found {objects.Length} GameObject(s):\n";
                    foreach (var obj in objects)
                    {
                        var position = obj.transform.position;
                        var activeState = obj.activeInHierarchy ? "active" : "inactive";
                        result += $"- {obj.name} ({activeState}) at position ({position.x:F2}, {position.y:F2}, {position.z:F2})\n";
                    }

                    taskCompletionSource.SetResult(CreateTextResult(result));
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetResult(CreateTextResult($"Error: {ex.Message}", true));
                }
            });

            return await taskCompletionSource.Task;
        }
    }

    /// <summary>
    /// 示例工具：获取场景信息
    /// </summary>
    public class UnitySceneInfoTool : McpTool
    {
        public override string Name => "unity_scene_info";
        public override string Description => "Get information about the current Unity scene";

        public override JObject GetInputSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject()
            };
        }

        public override async Task<CallToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            var taskCompletionSource = new TaskCompletionSource<CallToolResult>();

            UnityMainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    var scene = SceneManager.GetActiveScene();
                    var rootObjects = scene.GetRootGameObjects();

                    var info = $"Scene: {scene.name}\n";
                    info += $"Path: {scene.path}\n";
                    info += $"Build Index: {scene.buildIndex}\n";
                    info += $"Root GameObjects: {rootObjects.Length}\n";
                    info += $"Total GameObjects: {GameObject.FindObjectsOfType<GameObject>().Length}\n";
                    info += $"Is Loaded: {scene.isLoaded}\n";
                    info += $"Is Dirty: {scene.isDirty}\n\n";

                    info += "Root Objects:\n";
                    foreach (var obj in rootObjects)
                    {
                        info += $"- {obj.name}\n";
                    }

                    taskCompletionSource.SetResult(CreateTextResult(info));
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetResult(CreateTextResult($"Error: {ex.Message}", true));
                }
            });

            return await taskCompletionSource.Task;
        }
    }

    /// <summary>
    /// 示例资源：Unity场景层级结构
    /// </summary>
    public class UnitySceneHierarchyResource : McpResource
    {
        public override string Uri => "unity://scene/hierarchy";
        public override string Name => "Scene Hierarchy";
        public override string Description => "The complete hierarchy of the current Unity scene";
        public override string MimeType => "text/plain";

        public override async Task<ResourceContents> ReadAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<ResourceContents>();

            UnityMainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    var scene = SceneManager.GetActiveScene();
                    var rootObjects = scene.GetRootGameObjects();
                    var hierarchy = $"Scene: {scene.name}\n\n";

                    foreach (var root in rootObjects)
                    {
                        hierarchy += BuildHierarchy(root.transform, 0);
                    }

                    taskCompletionSource.SetResult(new ResourceContents
                    {
                        Uri = Uri,
                        MimeType = MimeType,
                        Text = hierarchy
                    });
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetResult(new ResourceContents
                    {
                        Uri = Uri,
                        MimeType = "text/plain",
                        Text = $"Error reading scene hierarchy: {ex.Message}"
                    });
                }
            });

            return await taskCompletionSource.Task;
        }

        private string BuildHierarchy(Transform transform, int depth)
        {
            var indent = new string(' ', depth * 2);
            var result = $"{indent}- {transform.name}\n";

            for (int i = 0; i < transform.childCount; i++)
            {
                result += BuildHierarchy(transform.GetChild(i), depth + 1);
            }

            return result;
        }
    }

    /// <summary>
    /// Unity主线程调度器 - 用于从后台线程执行Unity API
    /// 重要：必须在某个MonoBehaviour的Update()中调用Update()方法
    /// </summary>
    public static class UnityMainThreadDispatcher
    {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 将操作排队到Unity主线程执行
        /// </summary>
        public static void Enqueue(Action action)
        {
            lock (_lock)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// 在Unity主线程中调用此方法（通常在MonoBehaviour.Update中）
        /// </summary>
        public static void Update()
        {
            lock (_lock)
            {
                while (_executionQueue.Count > 0)
                {
                    var action = _executionQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error executing main thread action: {ex.Message}");
                    }
                }
            }
        }
    }
}
