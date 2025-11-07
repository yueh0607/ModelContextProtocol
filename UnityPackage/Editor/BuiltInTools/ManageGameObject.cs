using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
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
            [McpParameter("GameObject name to search for", Example = "Main Camera")]
            string name,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    return McpUtils.Error($"GameObject '{name}' not found");
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

                return McpUtils.Success(info);
            });
        }

        [McpTool(Description = "Create a new GameObject", Category = "Scene")]
        public async Task<CallToolResult> CreateGameObject(
            [McpParameter("Name for the new GameObject", Example = "MyObject")]
            string name,
            [McpParameter("Parent GameObject name (optional)")]
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
                return McpUtils.Success($"Created GameObject '{name}'");
            });
        }

        [McpTool(Description = "List all GameObjects in scene", Category = "Scene")]
        public async Task<CallToolResult> ListGameObjects(
            [McpParameter("Include inactive objects")]
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
                return McpUtils.Success($"Found {names.Length} GameObjects:\n{string.Join("\n", names)}");
            });
        }

        [McpTool(Description = "Delete a GameObject by name", Category = "Scene")]
        public async Task<CallToolResult> DeleteGameObject(
            [McpParameter("GameObject name to delete")]
            string name,
            [McpParameter("Include inactive objects in search")]
            bool includeInactive = false,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = FindGameObjectByName(name, includeInactive);
                if (go == null)
                {
                    return McpUtils.Error($"GameObject '{name}' not found");
                }

                Undo.DestroyObjectImmediate(go);
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                return McpUtils.Success($"Deleted GameObject '{name}'");
            });
        }

        [McpTool(Description = "Find GameObject(s) by name or tag", Category = "Scene")]
        public async Task<CallToolResult> FindGameObject(
            [McpParameter("GameObject name (optional)", Required = false)]
            string name = null,
            [McpParameter("Tag to filter by (optional)", Required = false)]
            string tag = null,
            [McpParameter("Include inactive objects")]
            bool includeInactive = false,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var results = new List<string>();
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                if (!string.IsNullOrEmpty(tag))
                {
                    var taggedObjects = GameObject.FindGameObjectsWithTag(tag);
                    foreach (var obj in taggedObjects)
                    {
                        if (string.IsNullOrEmpty(name) || obj.name == name)
                        {
                            results.Add($"- {obj.name} (Path: {GetGameObjectPath(obj)})");
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var transform in root.GetComponentsInChildren<Transform>(includeInactive))
                        {
                            if (transform.gameObject.name == name)
                            {
                                results.Add($"- {transform.gameObject.name} (Path: {GetGameObjectPath(transform.gameObject)})");
                            }
                        }
                    }
                }
                else
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        results.Add($"- {root.name}");
                    }
                }

                if (results.Count == 0)
                {
                    return McpUtils.Success("No GameObjects found matching the criteria.");
                }

                return McpUtils.Success($"Found {results.Count} GameObject(s):\n{string.Join("\n", results)}");
            });
        }

        #endregion

        #region Component Tools

        [McpTool(Description = "Add a component to a GameObject", Category = "Components")]
        public async Task<CallToolResult> AddComponent(
            [McpParameter("GameObject name")]
            string gameObjectName,
            [McpParameter("Component type name (e.g., 'BoxCollider', 'Rigidbody')")]
            string componentType,
            [McpParameter("Include inactive objects in search")]
            bool includeInactive = false,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = FindGameObjectByName(gameObjectName, includeInactive);
                if (go == null)
                {
                    return McpUtils.Error($"GameObject '{gameObjectName}' not found");
                }

                var type = FindComponentType(componentType);
                if (type == null)
                {
                    return McpUtils.Error($"Component type '{componentType}' not found. Use correct type name (e.g., 'BoxCollider', 'Rigidbody')");
                }

                if (!typeof(Component).IsAssignableFrom(type))
                {
                    return McpUtils.Error($"Type '{componentType}' is not a valid Component type");
                }

                Undo.AddComponent(go, type);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return McpUtils.Success($"Added component '{type.Name}' to GameObject '{gameObjectName}'");
            });
        }

        [McpTool(Description = "Remove a component from a GameObject", Category = "Components")]
        public async Task<CallToolResult> RemoveComponent(
            [McpParameter("GameObject name")]
            string gameObjectName,
            [McpParameter("Component type name to remove")]
            string componentType,
            [McpParameter("Include inactive objects in search")]
            bool includeInactive = false,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = FindGameObjectByName(gameObjectName, includeInactive);
                if (go == null)
                {
                    return McpUtils.Error($"GameObject '{gameObjectName}' not found");
                }

                var type = FindComponentType(componentType);
                if (type == null)
                {
                    return McpUtils.Error($"Component type '{componentType}' not found");
                }

                var component = go.GetComponent(type);
                if (component == null)
                {
                    return McpUtils.Error($"Component '{type.Name}' not found on GameObject '{gameObjectName}'");
                }

                Undo.DestroyObjectImmediate(component);
                EditorSceneManager.MarkSceneDirty(go.scene);
                return McpUtils.Success($"Removed component '{type.Name}' from GameObject '{gameObjectName}'");
            });
        }

        [McpTool(Description = "Get all components on a GameObject", Category = "Components")]
        public async Task<CallToolResult> GetComponents(
            [McpParameter("GameObject name")]
            string gameObjectName,
            [McpParameter("Include inactive objects in search")]
            bool includeInactive = false,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = FindGameObjectByName(gameObjectName, includeInactive);
                if (go == null)
                {
                    return McpUtils.Error($"GameObject '{gameObjectName}' not found");
                }

                var components = go.GetComponents<Component>();
                var componentList = components.Where(c => c != null).Select(c => $"- {c.GetType().Name}").ToList();

                if (componentList.Count == 0)
                {
                    return McpUtils.Success($"GameObject '{gameObjectName}' has no components");
                }

                return McpUtils.Success($"Components on '{gameObjectName}':\n{string.Join("\n", componentList)}");
            });
        }

        #endregion

        #region Transform Tools

        [McpTool(Description = "Set GameObject position", Category = "Transform")]
        public async Task<CallToolResult> SetPosition(
            [McpParameter("GameObject name")]
            string name,
            [McpParameter("X coordinate")]
            float x = 0,
            [McpParameter("Y coordinate")]
            float y = 0,
            [McpParameter("Z coordinate")]
            float z = 0,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = GameObject.Find(name);
                if (go == null) return McpUtils.Error($"GameObject '{name}' not found");

                Undo.RecordObject(go.transform, "Set Position");
                go.transform.position = new Vector3(x, y, z);
                return McpUtils.Success($"Set position of '{name}' to ({x}, {y}, {z})");
            });
        }

        [McpTool(Description = "Set GameObject rotation (euler angles)", Category = "Transform")]
        public async Task<CallToolResult> SetRotation(
            [McpParameter("GameObject name")]
            string name,
            [McpParameter("X rotation (degrees)")]
            float x = 0,
            [McpParameter("Y rotation (degrees)")]
            float y = 0,
            [McpParameter("Z rotation (degrees)")]
            float z = 0,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = GameObject.Find(name);
                if (go == null) return McpUtils.Error($"GameObject '{name}' not found");

                Undo.RecordObject(go.transform, "Set Rotation");
                go.transform.rotation = Quaternion.Euler(x, y, z);
                return McpUtils.Success($"Set rotation of '{name}' to ({x}, {y}, {z})");
            });
        }

        [McpTool(Description = "Set GameObject scale", Category = "Transform")]
        public async Task<CallToolResult> SetScale(
            [McpParameter("GameObject name")]
            string name,
            [McpParameter("X scale")]
            float x = 1,
            [McpParameter("Y scale")]
            float y = 1,
            [McpParameter("Z scale")]
            float z = 1,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                var go = GameObject.Find(name);
                if (go == null) return McpUtils.Error($"GameObject '{name}' not found");

                Undo.RecordObject(go.transform, "Set Scale");
                go.transform.localScale = new Vector3(x, y, z);
                return McpUtils.Success($"Set scale of '{name}' to ({x}, {y}, {z})");
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
                return McpUtils.Success(info);
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
                    return McpUtils.Error("Scene has not been saved yet. Save it manually first.");
                }

                bool saved = EditorSceneManager.SaveScene(scene);
                return saved ? McpUtils.Success($"Saved scene '{scene.name}'") : McpUtils.Error("Failed to save scene");
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
                return McpUtils.Success(info);
            });
        }

        [McpTool(Description = "Control play mode", Category = "Editor")]
        public async Task<CallToolResult> PlayMode(
            [McpParameter("Action: 'enter', 'exit', or 'toggle'", Example = "enter")]
            string action,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                switch (action.ToLower())
                {
                    case "enter":
                        if (EditorApplication.isPlaying) return McpUtils.Success("Already in play mode");
                        EditorApplication.isPlaying = true;
                        return McpUtils.Success("Entering play mode...");

                    case "exit":
                        if (!EditorApplication.isPlaying) return McpUtils.Success("Not in play mode");
                        EditorApplication.isPlaying = false;
                        return McpUtils.Success("Exiting play mode...");

                    case "toggle":
                        EditorApplication.isPlaying = !EditorApplication.isPlaying;
                        return McpUtils.Success(EditorApplication.isPlaying ? "Entering play mode..." : "Exiting play mode...");

                    default:
                        return McpUtils.Error("Invalid action. Use 'enter', 'exit', or 'toggle'");
                }
            });
        }

        #endregion

        #region Helper Methods

        private static GameObject FindGameObjectByName(string name, bool includeInactive = false)
        {
            // 先在Prefab Stage中查找
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage?.prefabContentsRoot != null)
            {
                foreach (var transform in stage.prefabContentsRoot.GetComponentsInChildren<Transform>(includeInactive))
                {
                    if (transform.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }

            // 在活动场景中查找
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;

                foreach (var transform in root.GetComponentsInChildren<Transform>(includeInactive))
                {
                    if (transform.gameObject.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            var path = obj.name;
            var current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private static System.Type FindComponentType(string typeName)
        {
            // 尝试在UnityEngine命名空间中查找
            var type = System.Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (type != null) return type;

            // 尝试在UnityEngine.UI命名空间中查找
            type = System.Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI");
            if (type != null) return type;

            // 尝试直接按名称查找
            type = System.Type.GetType(typeName);
            if (type != null) return type;

            // 在所有已加载的程序集中查找
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
                if (type != null) return type;
            }

            return null;
        }

        #endregion
    }
}
