using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;
using MapleModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// Unity工具注册器 - 将Unity Editor功能注册为MCP工具
    /// </summary>
    public static class UnityToolRegistry
    {
        /// <summary>
        /// 注册所有Unity工具到服务器选项
        /// </summary>
        public static List<SimpleMcpServerTool> CreateAllTools()
        {
            var tools = new List<SimpleMcpServerTool>();

            // GameObject工具
            tools.Add(CreateGetGameObjectTool());
            tools.Add(CreateCreateGameObjectTool());
            tools.Add(CreateDestroyGameObjectTool());

            // Transform工具
            tools.Add(CreateSetPositionTool());
            tools.Add(CreateSetRotationTool());
            tools.Add(CreateSetScaleTool());

            // Component工具
            tools.Add(CreateAddComponentTool());
            tools.Add(CreateRemoveComponentTool());

            // Scene工具
            tools.Add(CreateLoadSceneTool());
            tools.Add(CreateSaveSceneTool());

            // Editor工具
            tools.Add(CreatePlayModeTool());
            tools.Add(CreateGetProjectInfoTool());

            return tools;
        }

        #region GameObject Tools

        private static SimpleMcpServerTool CreateGetGameObjectTool()
        {
            return SimpleMcpServerTool.Create(
                name: "GetGameObject",
                description: "Get GameObject by name or path in the scene",
                handler: async (args, ct) =>
                {
                    string name = args["name"]?.ToString();
                    if (string.IsNullOrEmpty(name))
                    {
                        return CreateErrorResult("Parameter 'name' is required");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var go = GameObject.Find(name);
                        if (go == null)
                        {
                            return CreateErrorResult($"GameObject '{name}' not found");
                        }

                        var info = new
                        {
                            name = go.name,
                            activeSelf = go.activeSelf,
                            activeInHierarchy = go.activeInHierarchy,
                            tag = go.tag,
                            layer = go.layer,
                            position = go.transform.position,
                            rotation = go.transform.rotation.eulerAngles,
                            scale = go.transform.localScale
                        };

                        return CreateSuccessResult($"GameObject found:\n{JsonUtility.ToJson(info, true)}");
                    });
                }
            );
        }

        private static SimpleMcpServerTool CreateCreateGameObjectTool()
        {
            return SimpleMcpServerTool.Create(
                name: "CreateGameObject",
                description: "Create a new GameObject in the scene",
                handler: async (args, ct) =>
                {
                    string name = args["name"]?.ToString() ?? "GameObject";

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var go = new GameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                        return CreateSuccessResult($"Created GameObject '{name}'");
                    });
                }
            );
        }

        private static SimpleMcpServerTool CreateDestroyGameObjectTool()
        {
            return SimpleMcpServerTool.Create(
                name: "DestroyGameObject",
                description: "Destroy a GameObject by name",
                handler: async (args, ct) =>
                {
                    string name = args["name"]?.ToString();
                    if (string.IsNullOrEmpty(name))
                    {
                        return CreateErrorResult("Parameter 'name' is required");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var go = GameObject.Find(name);
                        if (go == null)
                        {
                            return CreateErrorResult($"GameObject '{name}' not found");
                        }

                        Undo.DestroyObjectImmediate(go);
                        return CreateSuccessResult($"Destroyed GameObject '{name}'");
                    });
                }
            );
        }

        #endregion

        #region Transform Tools

        private static SimpleMcpServerTool CreateSetPositionTool()
        {
            return SimpleMcpServerTool.Create(
                name: "SetPosition",
                description: "Set GameObject position (x, y, z)",
                handler: async (args, ct) =>
                {
                    string name = args["name"]?.ToString();
                    float x = args["x"]?.ToObject<float>() ?? 0;
                    float y = args["y"]?.ToObject<float>() ?? 0;
                    float z = args["z"]?.ToObject<float>() ?? 0;

                    if (string.IsNullOrEmpty(name))
                    {
                        return CreateErrorResult("Parameter 'name' is required");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var go = GameObject.Find(name);
                        if (go == null)
                        {
                            return CreateErrorResult($"GameObject '{name}' not found");
                        }

                        Undo.RecordObject(go.transform, "Set Position");
                        go.transform.position = new Vector3(x, y, z);
                        return CreateSuccessResult($"Set position of '{name}' to ({x}, {y}, {z})");
                    });
                }
            );
        }

        private static SimpleMcpServerTool CreateSetRotationTool()
        {
            return SimpleMcpServerTool.Create(
                name: "SetRotation",
                description: "Set GameObject rotation (euler angles: x, y, z)",
                handler: async (args, ct) =>
                {
                    string name = args["name"]?.ToString();
                    float x = args["x"]?.ToObject<float>() ?? 0;
                    float y = args["y"]?.ToObject<float>() ?? 0;
                    float z = args["z"]?.ToObject<float>() ?? 0;

                    if (string.IsNullOrEmpty(name))
                    {
                        return CreateErrorResult("Parameter 'name' is required");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var go = GameObject.Find(name);
                        if (go == null)
                        {
                            return CreateErrorResult($"GameObject '{name}' not found");
                        }

                        Undo.RecordObject(go.transform, "Set Rotation");
                        go.transform.rotation = Quaternion.Euler(x, y, z);
                        return CreateSuccessResult($"Set rotation of '{name}' to ({x}, {y}, {z})");
                    });
                }
            );
        }

        private static SimpleMcpServerTool CreateSetScaleTool()
        {
            return SimpleMcpServerTool.Create(
                name: "SetScale",
                description: "Set GameObject scale (x, y, z)",
                handler: async (args, ct) =>
                {
                    string name = args["name"]?.ToString();
                    float x = args["x"]?.ToObject<float>() ?? 1;
                    float y = args["y"]?.ToObject<float>() ?? 1;
                    float z = args["z"]?.ToObject<float>() ?? 1;

                    if (string.IsNullOrEmpty(name))
                    {
                        return CreateErrorResult("Parameter 'name' is required");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var go = GameObject.Find(name);
                        if (go == null)
                        {
                            return CreateErrorResult($"GameObject '{name}' not found");
                        }

                        Undo.RecordObject(go.transform, "Set Scale");
                        go.transform.localScale = new Vector3(x, y, z);
                        return CreateSuccessResult($"Set scale of '{name}' to ({x}, {y}, {z})");
                    });
                }
            );
        }

        #endregion

        #region Component Tools

        private static SimpleMcpServerTool CreateAddComponentTool()
        {
            return SimpleMcpServerTool.Create(
                name: "AddComponent",
                description: "Add component to GameObject (e.g., 'BoxCollider', 'Rigidbody')",
                handler: async (args, ct) =>
                {
                    string gameObjectName = args["gameObject"]?.ToString();
                    string componentType = args["componentType"]?.ToString();

                    if (string.IsNullOrEmpty(gameObjectName) || string.IsNullOrEmpty(componentType))
                    {
                        return CreateErrorResult("Parameters 'gameObject' and 'componentType' are required");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var go = GameObject.Find(gameObjectName);
                        if (go == null)
                        {
                            return CreateErrorResult($"GameObject '{gameObjectName}' not found");
                        }

                        // 尝试查找组件类型
                        var type = Type.GetType($"UnityEngine.{componentType}, UnityEngine");
                        if (type == null)
                        {
                            return CreateErrorResult($"Component type '{componentType}' not found");
                        }

                        var component = go.AddComponent(type);
                        Undo.RegisterCreatedObjectUndo(component, $"Add {componentType}");
                        return CreateSuccessResult($"Added component '{componentType}' to '{gameObjectName}'");
                    });
                }
            );
        }

        private static SimpleMcpServerTool CreateRemoveComponentTool()
        {
            return SimpleMcpServerTool.Create(
                name: "RemoveComponent",
                description: "Remove component from GameObject",
                handler: async (args, ct) =>
                {
                    string gameObjectName = args["gameObject"]?.ToString();
                    string componentType = args["componentType"]?.ToString();

                    if (string.IsNullOrEmpty(gameObjectName) || string.IsNullOrEmpty(componentType))
                    {
                        return CreateErrorResult("Parameters 'gameObject' and 'componentType' are required");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var go = GameObject.Find(gameObjectName);
                        if (go == null)
                        {
                            return CreateErrorResult($"GameObject '{gameObjectName}' not found");
                        }

                        var type = Type.GetType($"UnityEngine.{componentType}, UnityEngine");
                        if (type == null)
                        {
                            return CreateErrorResult($"Component type '{componentType}' not found");
                        }

                        var component = go.GetComponent(type);
                        if (component == null)
                        {
                            return CreateErrorResult($"GameObject '{gameObjectName}' does not have component '{componentType}'");
                        }

                        Undo.DestroyObjectImmediate(component);
                        return CreateSuccessResult($"Removed component '{componentType}' from '{gameObjectName}'");
                    });
                }
            );
        }

        #endregion

        #region Scene Tools

        private static SimpleMcpServerTool CreateLoadSceneTool()
        {
            return SimpleMcpServerTool.Create(
                name: "LoadScene",
                description: "Load a Unity scene by name or path",
                handler: async (args, ct) =>
                {
                    string sceneName = args["sceneName"]?.ToString();
                    if (string.IsNullOrEmpty(sceneName))
                    {
                        return CreateErrorResult("Parameter 'sceneName' is required");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            return CreateErrorResult("Scene load cancelled by user");
                        }

                        var scene = EditorSceneManager.OpenScene(sceneName);
                        return CreateSuccessResult($"Loaded scene: {scene.name}");
                    });
                }
            );
        }

        private static SimpleMcpServerTool CreateSaveSceneTool()
        {
            return SimpleMcpServerTool.Create(
                name: "SaveScene",
                description: "Save current scene",
                handler: async (args, ct) =>
                {
                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var scene = EditorSceneManager.GetActiveScene();
                        bool saved = EditorSceneManager.SaveScene(scene);
                        return saved
                            ? CreateSuccessResult($"Saved scene: {scene.name}")
                            : CreateErrorResult("Failed to save scene");
                    });
                }
            );
        }

        #endregion

        #region Editor Tools

        private static SimpleMcpServerTool CreatePlayModeTool()
        {
            return SimpleMcpServerTool.Create(
                name: "PlayMode",
                description: "Enter or exit play mode (action: 'enter' or 'exit')",
                handler: async (args, ct) =>
                {
                    string action = args["action"]?.ToString()?.ToLower();
                    if (action != "enter" && action != "exit")
                    {
                        return CreateErrorResult("Parameter 'action' must be 'enter' or 'exit'");
                    }

                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        if (action == "enter")
                        {
                            EditorApplication.isPlaying = true;
                            return CreateSuccessResult("Entering play mode");
                        }
                        else
                        {
                            EditorApplication.isPlaying = false;
                            return CreateSuccessResult("Exiting play mode");
                        }
                    });
                }
            );
        }

        private static SimpleMcpServerTool CreateGetProjectInfoTool()
        {
            return SimpleMcpServerTool.Create(
                name: "GetProjectInfo",
                description: "Get Unity project information",
                handler: async (args, ct) =>
                {
                    return await UnityMainThreadScheduler.ExecuteAsync(() =>
                    {
                        var info = new
                        {
                            projectPath = Application.dataPath,
                            unityVersion = Application.unityVersion,
                            platform = Application.platform.ToString(),
                            isPlaying = EditorApplication.isPlaying,
                            currentScene = EditorSceneManager.GetActiveScene().name
                        };

                        return CreateSuccessResult($"Project Info:\n{JsonUtility.ToJson(info, true)}");
                    });
                }
            );
        }

        #endregion

        #region Helper Methods

        private static CallToolResult CreateSuccessResult(string message)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = message }
                },
                IsError = false
            };
        }

        private static CallToolResult CreateErrorResult(string errorMessage)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Error: {errorMessage}" }
                },
                IsError = true
            };
        }

        #endregion
    }
}
