using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityAIStudio.McpServer.Tools.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 内置通用工具 - 提供基础的Unity操作功能
    /// 这些工具随包自带，用户无需修改
    /// </summary>
    [McpToolClass(Category = "General", Description = "Built-in Unity tools")]
    public class GeneralTools
    {
        #region GameObject Tools

        [McpTool(Description = "Get GameObject information by name", Category = "Scene")]
        public async Task<CallToolResult> GetGameObject(
            [McpParameter("GameObject name to search for", Required = true, Example = "Main Camera")]
            string name,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    return CreateError($"GameObject '{name}' not found");
                }

                var components = go.GetComponents<Component>().Select(c => c.GetType().Name);
                var info = $@"GameObject Info:
- Name: {go.name}
- Tag: {go.tag}
- Layer: {LayerMask.LayerToName(go.layer)}
- Active: {go.activeSelf}
- Position: {go.transform.position}
- Rotation: {go.transform.rotation.eulerAngles}
- Scale: {go.transform.localScale}
- Components: {string.Join(", ", components)}";

                return CreateSuccess(info);
            });
        }

        [McpTool(Description = "Create a new GameObject", Category = "Scene")]
        public async Task<CallToolResult> CreateGameObject(
            [McpParameter("Name for the new GameObject", Required = true, Example = "MyObject")]
            string name,
            [McpParameter("Parent GameObject name (optional)", Required = false)]
            string parent = null,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = new GameObject(name);

                if (!string.IsNullOrEmpty(parent))
                {
                    var parentObj = GameObject.Find(parent);
                    if (parentObj != null)
                    {
                        go.transform.SetParent(parentObj.transform);
                    }
                }

                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                return CreateSuccess($"Created GameObject '{name}'");
            });
        }

        [McpTool(Description = "List all GameObjects in scene", Category = "Scene")]
        public async Task<CallToolResult> ListGameObjects(
            [McpParameter("Include inactive objects", Required = false)]
            bool includeInactive = false,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var objects = includeInactive
                    ? Resources.FindObjectsOfTypeAll<GameObject>()
                        .Where(go => go.scene.isLoaded)
                        .ToArray()
                    : Object.FindObjectsOfType<GameObject>();
                var names = objects.Select(go => go.name).OrderBy(n => n).ToArray();
                return CreateSuccess($"Found {names.Length} GameObjects:\n{string.Join("\n", names)}");
            });
        }

        #endregion

        #region Transform Tools

        [McpTool(Description = "Set GameObject position", Category = "Transform")]
        public async Task<CallToolResult> SetPosition(
            [McpParameter("GameObject name", Required = true)]
            string name,
            [McpParameter("X coordinate", Required = false)]
            float x = 0,
            [McpParameter("Y coordinate", Required = false)]
            float y = 0,
            [McpParameter("Z coordinate", Required = false)]
            float z = 0,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = GameObject.Find(name);
                if (go == null) return CreateError($"GameObject '{name}' not found");

                Undo.RecordObject(go.transform, "Set Position");
                go.transform.position = new Vector3(x, y, z);
                return CreateSuccess($"Set position of '{name}' to ({x}, {y}, {z})");
            });
        }

        [McpTool(Description = "Set GameObject rotation (euler angles)", Category = "Transform")]
        public async Task<CallToolResult> SetRotation(
            [McpParameter("GameObject name", Required = true)]
            string name,
            [McpParameter("X rotation (degrees)", Required = false)]
            float x = 0,
            [McpParameter("Y rotation (degrees)", Required = false)]
            float y = 0,
            [McpParameter("Z rotation (degrees)", Required = false)]
            float z = 0,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = GameObject.Find(name);
                if (go == null) return CreateError($"GameObject '{name}' not found");

                Undo.RecordObject(go.transform, "Set Rotation");
                go.transform.rotation = Quaternion.Euler(x, y, z);
                return CreateSuccess($"Set rotation of '{name}' to ({x}, {y}, {z})");
            });
        }

        [McpTool(Description = "Set GameObject scale", Category = "Transform")]
        public async Task<CallToolResult> SetScale(
            [McpParameter("GameObject name", Required = true)]
            string name,
            [McpParameter("X scale", Required = false)]
            float x = 1,
            [McpParameter("Y scale", Required = false)]
            float y = 1,
            [McpParameter("Z scale", Required = false)]
            float z = 1,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = GameObject.Find(name);
                if (go == null) return CreateError($"GameObject '{name}' not found");

                Undo.RecordObject(go.transform, "Set Scale");
                go.transform.localScale = new Vector3(x, y, z);
                return CreateSuccess($"Set scale of '{name}' to ({x}, {y}, {z})");
            });
        }

        #endregion

        #region Scene Tools

        [McpTool(Description = "Get current scene information", Category = "Scene")]
        public async Task<CallToolResult> GetSceneInfo(CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                var info = $@"Scene Info:
- Name: {scene.name}
- Path: {scene.path}
- Is Dirty: {scene.isDirty}
- Is Loaded: {scene.isLoaded}
- Root Object Count: {scene.rootCount}";
                return CreateSuccess(info);
            });
        }

        [McpTool(Description = "Save current scene", Category = "Scene")]
        public async Task<CallToolResult> SaveScene(CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(scene.path))
                {
                    return CreateError("Scene has not been saved yet. Save it manually first.");
                }

                bool saved = EditorSceneManager.SaveScene(scene);
                return saved ? CreateSuccess($"Saved scene '{scene.name}'") : CreateError("Failed to save scene");
            });
        }

        #endregion

        #region Editor Tools

        [McpTool(Description = "Get Unity project information", Category = "Project")]
        public async Task<CallToolResult> GetProjectInfo(CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var info = $@"Project Info:
- Project Name: {Application.productName}
- Unity Version: {Application.unityVersion}
- Platform: {Application.platform}
- Data Path: {Application.dataPath}
- Is Playing: {EditorApplication.isPlaying}
- Is Compiling: {EditorApplication.isCompiling}";
                return CreateSuccess(info);
            });
        }

        [McpTool(Description = "Control play mode", Category = "Editor")]
        public async Task<CallToolResult> PlayMode(
            [McpParameter("Action: 'enter', 'exit', or 'toggle'", Required = true, Example = "enter")]
            string action,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                switch (action.ToLower())
                {
                    case "enter":
                        if (EditorApplication.isPlaying) return CreateSuccess("Already in play mode");
                        EditorApplication.isPlaying = true;
                        return CreateSuccess("Entering play mode...");

                    case "exit":
                        if (!EditorApplication.isPlaying) return CreateSuccess("Not in play mode");
                        EditorApplication.isPlaying = false;
                        return CreateSuccess("Exiting play mode...");

                    case "toggle":
                        EditorApplication.isPlaying = !EditorApplication.isPlaying;
                        return CreateSuccess(EditorApplication.isPlaying ? "Entering play mode..." : "Exiting play mode...");

                    default:
                        return CreateError("Invalid action. Use 'enter', 'exit', or 'toggle'");
                }
            });
        }

        #endregion

        #region Helper Methods

        private CallToolResult CreateSuccess(string message)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock> { new TextContentBlock { Text = message } },
                IsError = false
            };
        }

        private CallToolResult CreateError(string message)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock> { new TextContentBlock { Text = $"Error: {message}" } },
                IsError = true
            };
        }

        #endregion
    }
}
