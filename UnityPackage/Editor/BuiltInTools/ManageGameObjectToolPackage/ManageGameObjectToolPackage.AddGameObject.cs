using System;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// ManageGameObjectToolPackage - GameObject 添加部分
    /// </summary>
    public partial class ManageGameObjectToolPackage
    {
        /// <summary>
        /// 向 Prefab 中添加 GameObject（从其他 Prefab 实例化或创建新的空 GameObject）
        /// </summary>
        [McpTool(
            Description = "Add a GameObject to a Prefab. You can either instantiate another prefab as a child, or create a new empty GameObject.",
            Category = "Prefab Modification"
        )]
        public async Task<CallToolResult> AddGameObjectToPrefab(
            [McpParameter("Path to the target prefab file to modify (supports fuzzy path matching)")]
            [FuzzyPathProcessor(FileExtension = ".prefab", AssetType = "t:Prefab", KeepOriginalIfNotFound = false)]
            string targetPrefabPath,
            [McpParameter("Hierarchy path to the parent GameObject where the new object will be added (e.g., 'Parent/Child'). Leave empty to add to root.")]
            [TrimProcessor]
            string parentPath = "",
            [McpParameter("Path to the prefab to instantiate as a child (supports fuzzy path matching). Leave empty to create an empty GameObject instead.")]
            [FuzzyPathProcessor(FileExtension = ".prefab", AssetType = "t:Prefab", KeepOriginalIfNotFound = false)]
            string sourcePrefabPath = null,
            [McpParameter("Name for the new GameObject. If adding a prefab, this will override the prefab's name.")]
            [TrimProcessor]
            string newObjectName = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(targetPrefabPath))
            {
                return McpUtils.Error("Required parameter 'targetPrefabPath' is missing or empty.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    // 加载目标 Prefab
                    GameObject targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetPrefabPath);
                    if (targetPrefab == null)
                    {
                        return McpUtils.Error($"Failed to load target prefab at path: {targetPrefabPath}");
                    }

                    // 打开 Prefab 进行编辑
                    string assetPath = AssetDatabase.GetAssetPath(targetPrefab);
                    GameObject prefabContentsRoot = PrefabUtility.LoadPrefabContents(assetPath);

                    try
                    {
                        // 查找父对象
                        Transform parentTransform;
                        if (string.IsNullOrWhiteSpace(parentPath))
                        {
                            parentTransform = prefabContentsRoot.transform;
                        }
                        else
                        {
                            parentTransform = FindTransformByPath(prefabContentsRoot.transform, parentPath);
                            if (parentTransform == null)
                            {
                                return McpUtils.Error(
                                    $"Parent GameObject not found at path: {parentPath}\n" +
                                    $"Please use ReadPrefabStructure to view the hierarchy.");
                            }
                        }

                        GameObject newObject;
                        string operationDescription;

                        // 判断是添加 Prefab 还是创建空对象
                        if (!string.IsNullOrWhiteSpace(sourcePrefabPath))
                        {
                            // 加载源 Prefab
                            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
                            if (sourcePrefab == null)
                            {
                                return McpUtils.Error($"Failed to load source prefab at path: {sourcePrefabPath}");
                            }

                            // 实例化源 Prefab
                            newObject = UnityEngine.Object.Instantiate(sourcePrefab, parentTransform);
                            operationDescription = $"Added prefab instance '{sourcePrefab.name}'";

                            // 如果提供了新名称，使用新名称，否则保持 prefab 原名
                            if (!string.IsNullOrWhiteSpace(newObjectName))
                            {
                                newObject.name = newObjectName;
                            }
                            else
                            {
                                // 移除 Unity 自动添加的 "(Clone)" 后缀
                                newObject.name = sourcePrefab.name;
                            }
                        }
                        else
                        {
                            // 创建空 GameObject
                            string objectName = string.IsNullOrWhiteSpace(newObjectName) ? "NewGameObject" : newObjectName;
                            newObject = new GameObject(objectName);
                            newObject.transform.SetParent(parentTransform);
                            operationDescription = $"Created new empty GameObject '{objectName}'";
                        }

                        // 保存修改后的 Prefab
                        PrefabUtility.SaveAsPrefabAsset(prefabContentsRoot, assetPath);

                        string parentPathDisplay = string.IsNullOrWhiteSpace(parentPath) ? "(root)" : parentPath;

                        return McpUtils.Success(
                            $"{operationDescription} to prefab.\n" +
                            $"Parent path: {parentPathDisplay}\n" +
                            $"New object name: {newObject.name}\n" +
                            $"Prefab saved at: {assetPath}");
                    }
                    finally
                    {
                        // 确保卸载 Prefab 内容
                        PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
                    }
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"Failed to add GameObject to prefab: {ex.Message}");
                }
            });
        }
    }
}
