using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// Prefab管理工具 - 支持打开/关闭/保存Prefab编辑阶段，以及从GameObject创建Prefab
    /// </summary>
    [McpToolClass(Category = "Unity", Description = "Manage Unity Prefabs - open, close, save prefab stage, and create prefabs")]
    public class ManagePrefabs
    {
        /// <summary>
        /// 打开Prefab编辑阶段
        /// </summary>
        /// <param name="prefabPath">Prefab资产路径，例如 "Assets/Prefabs/MyPrefab.prefab"</param>
        [McpTool(
            Description = "Open a prefab in prefab stage for editing",
            Category = "Unity"
        )]
        public async Task<CallToolResult> OpenPrefabStage(
            [McpParameter("The path to the prefab asset to open (e.g., 'Assets/Prefabs/MyPrefab.prefab')")]
            string prefabPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return McpUtils.Error("'prefabPath' parameter is required.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    string sanitizedPath = SanitizeAssetPath(prefabPath);
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sanitizedPath);
                    if (prefabAsset == null)
                    {
                        return McpUtils.Error($"No prefab asset found at path '{sanitizedPath}'.");
                    }

                    PrefabStage stage = PrefabStageUtility.OpenPrefab(sanitizedPath);
                    if (stage == null)
                    {
                        return McpUtils.Error($"Failed to open prefab stage for '{sanitizedPath}'.");
                    }

                    string stageInfo = SerializeStageInfo(stage);
                    return McpUtils.Success($"Opened prefab stage for '{sanitizedPath}'.\n{stageInfo}");
                }
                catch (Exception e)
                {
                    return McpUtils.Error($"Error opening prefab stage: {e.Message}");
                }
            });
        }

        /// <summary>
        /// 关闭当前打开的Prefab编辑阶段
        /// </summary>
        /// <param name="saveBeforeClose">是否在关闭前保存修改（默认false）</param>
        [McpTool(
            Description = "Close the currently open prefab stage",
            Category = "Unity"
        )]
        public async Task<CallToolResult> ClosePrefabStage(
            [McpParameter("Whether to save changes before closing (default: false)", Required = false)]
            bool saveBeforeClose = false,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage == null)
                    {
                        return McpUtils.Success("No prefab stage is currently open.");
                    }

                    string stagePath = stage.assetPath;

                    if (saveBeforeClose && stage.scene.isDirty)
                    {
                        SaveStagePrefab(stage);
                        AssetDatabase.SaveAssets();
                    }

                    StageUtility.GoToMainStage();
                    return McpUtils.Success($"Closed prefab stage for '{stagePath}'.");
                }
                catch (Exception e)
                {
                    return McpUtils.Error($"Error closing prefab stage: {e.Message}");
                }
            });
        }

        /// <summary>
        /// 保存当前打开的Prefab编辑阶段
        /// </summary>
        [McpTool(
            Description = "Save the currently open prefab stage",
            Category = "Unity"
        )]
        public async Task<CallToolResult> SavePrefabStage(CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage == null)
                    {
                        return McpUtils.Error("No prefab stage is currently open.");
                    }

                    SaveStagePrefab(stage);
                    AssetDatabase.SaveAssets();

                    string stageInfo = SerializeStageInfo(stage);
                    return McpUtils.Success($"Saved prefab stage for '{stage.assetPath}'.\n{stageInfo}");
                }
                catch (Exception e)
                {
                    return McpUtils.Error($"Error saving prefab stage: {e.Message}");
                }
            });
        }

        /// <summary>
        /// 从场景中的GameObject创建Prefab
        /// </summary>
        /// <param name="gameObjectName">场景中GameObject的名称</param>
        /// <param name="prefabPath">要创建的Prefab路径</param>
        /// <param name="allowOverwrite">是否允许覆盖现有Prefab（默认false）</param>
        /// <param name="searchInactive">是否搜索未激活的GameObject（默认false）</param>
        [McpTool(
            Description = "Create a prefab from a GameObject in the scene",
            Category = "Unity"
        )]
        public async Task<CallToolResult> CreatePrefabFromGameObject(
            [McpParameter("The name of the GameObject in the scene to create prefab from")]
            string gameObjectName,
            [McpParameter("The path where the prefab will be created (e.g., 'Assets/Prefabs/MyPrefab.prefab')")]
            string prefabPath,
            [McpParameter("Whether to allow overwriting an existing prefab (default: false)", Required = false)]
            bool allowOverwrite = false,
            [McpParameter("Whether to include inactive GameObjects in search (default: false)", Required = false)]
            bool searchInactive = false,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(gameObjectName))
            {
                return McpUtils.Error("'gameObjectName' parameter is required.");
            }

            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return McpUtils.Error("'prefabPath' parameter is required.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    GameObject sourceObject = FindSceneObjectByName(gameObjectName, searchInactive);
                    if (sourceObject == null)
                    {
                        return McpUtils.Error($"GameObject '{gameObjectName}' not found in the active scene.");
                    }

                    if (PrefabUtility.IsPartOfPrefabAsset(sourceObject))
                    {
                        return McpUtils.Error(
                            $"GameObject '{sourceObject.name}' is part of a prefab asset. " +
                            "Open the prefab stage to save changes instead.");
                    }

                    PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(sourceObject);
                    if (status != PrefabInstanceStatus.NotAPrefab)
                    {
                        return McpUtils.Error(
                            $"GameObject '{sourceObject.name}' is already linked to an existing prefab instance.");
                    }

                    string sanitizedPath = SanitizeAssetPath(prefabPath);
                    if (!sanitizedPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        sanitizedPath += ".prefab";
                    }

                    string finalPath = sanitizedPath;
                    if (!allowOverwrite && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(finalPath) != null)
                    {
                        finalPath = AssetDatabase.GenerateUniqueAssetPath(finalPath);
                    }

                    EnsureAssetDirectoryExists(finalPath);

                    GameObject connectedInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(
                        sourceObject,
                        finalPath,
                        InteractionMode.AutomatedAction
                    );

                    if (connectedInstance == null)
                    {
                        return McpUtils.Error($"Failed to save prefab asset at '{finalPath}'.");
                    }

                    Selection.activeGameObject = connectedInstance;

                    return McpUtils.Success(
                        $"Prefab created at '{finalPath}' and instance linked.\n" +
                        $"Instance ID: {connectedInstance.GetInstanceID()}");
                }
                catch (Exception e)
                {
                    return McpUtils.Error($"Error creating prefab: {e.Message}");
                }
            });
        }

        // === 辅助方法 ===

        private static void SaveStagePrefab(PrefabStage stage)
        {
            if (stage?.prefabContentsRoot == null)
            {
                throw new InvalidOperationException("Cannot save prefab stage without a prefab root.");
            }

            bool saved = PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
            if (!saved)
            {
                throw new InvalidOperationException($"Failed to save prefab asset at '{stage.assetPath}'.");
            }
        }

        private static void EnsureAssetDirectoryExists(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            string fullDirectory = Path.Combine(Directory.GetCurrentDirectory(), directory);
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
                AssetDatabase.Refresh();
            }
        }

        private static GameObject FindSceneObjectByName(string name, bool includeInactive)
        {
            // 首先在Prefab Stage中查找
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage?.prefabContentsRoot != null)
            {
                foreach (Transform transform in stage.prefabContentsRoot.GetComponentsInChildren<Transform>(includeInactive))
                {
                    if (transform.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }

            // 在活动场景中查找
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(includeInactive))
                {
                    if (transform.gameObject.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }

        private static string SerializeStageInfo(PrefabStage stage)
        {
            if (stage == null)
            {
                return "Stage: Not open";
            }

            return $"Asset Path: {stage.assetPath}\n" +
                   $"Prefab Root: {(stage.prefabContentsRoot != null ? stage.prefabContentsRoot.name : "null")}\n" +
                   $"Mode: {stage.mode}\n" +
                   $"Is Dirty: {stage.scene.isDirty}";
        }

        private static string SanitizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // 标准化路径分隔符
            path = path.Replace('\\', '/');

            // 确保路径以 Assets/ 开头
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                path = "Assets/" + path.TrimStart('/');
            }

            return path;
        }
    }
}
