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
    /// ManageGameObjectToolPackage - Component 移除部分
    /// </summary>
    public partial class ManageGameObjectToolPackage
    {
        /// <summary>
        /// 从 Prefab 中的 GameObject 移除组件
        /// </summary>
        [McpTool(
            Description = "Remove a component from a GameObject in a Prefab. You can remove any component except Transform.",
            Category = "Component Management"
        )]
        public async Task<CallToolResult> RemoveComponentFromPrefab(
            [McpParameter("Path to the prefab file (supports fuzzy path matching)")]
            [FuzzyPathProcessor(FileExtension = ".prefab", AssetType = "t:Prefab", KeepOriginalIfNotFound = false)]
            string prefabPath,
            [McpParameter("Hierarchy path to the GameObject to remove component from (e.g., 'Parent/Child'). Leave empty for root GameObject.")]
            [TrimProcessor]
            string gameObjectPath = "",
            [McpParameter("Type name of the component to remove (e.g., 'BoxCollider', 'Rigidbody'). For custom scripts, use the full type name.")]
            [TrimProcessor]
            string componentTypeName = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return McpUtils.Error("Required parameter 'prefabPath' is missing or empty.");
            }

            if (string.IsNullOrWhiteSpace(componentTypeName))
            {
                return McpUtils.Error("Required parameter 'componentTypeName' is missing or empty.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    // 加载 Prefab
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        return McpUtils.Error($"Failed to load prefab at path: {prefabPath}");
                    }

                    // 打开 Prefab 进行编辑
                    string assetPath = AssetDatabase.GetAssetPath(prefab);
                    GameObject prefabContentsRoot = PrefabUtility.LoadPrefabContents(assetPath);

                    try
                    {
                        // 查找目标 GameObject
                        Transform targetTransform;
                        if (string.IsNullOrWhiteSpace(gameObjectPath))
                        {
                            targetTransform = prefabContentsRoot.transform;
                        }
                        else
                        {
                            targetTransform = FindTransformByPath(prefabContentsRoot.transform, gameObjectPath);
                            if (targetTransform == null)
                            {
                                return McpUtils.Error(
                                    $"GameObject not found at path: {gameObjectPath}\n" +
                                    $"Please use ReadPrefabStructure to view the hierarchy.");
                            }
                        }

                        GameObject targetObject = targetTransform.gameObject;

                        // 查找组件类型
                        Type componentType = FindComponentType(componentTypeName);
                        if (componentType == null)
                        {
                            return McpUtils.Error(
                                $"Component type '{componentTypeName}' not found.\n" +
                                $"Make sure the type name is correct and the assembly is loaded.");
                        }

                        // 不允许删除 Transform 组件
                        if (componentType == typeof(Transform))
                        {
                            return McpUtils.Error(
                                "Cannot remove Transform component. Transform is required for all GameObjects.");
                        }

                        // 查找要删除的组件
                        Component componentToRemove = targetObject.GetComponent(componentType);
                        if (componentToRemove == null)
                        {
                            return McpUtils.Error(
                                $"Component '{componentType.Name}' not found on GameObject '{targetObject.name}'.\n" +
                                $"Use ReadPrefabStructure to see what components are attached.");
                        }

                        // 删除组件
                        UnityEngine.Object.DestroyImmediate(componentToRemove);

                        // 保存修改后的 Prefab
                        PrefabUtility.SaveAsPrefabAsset(prefabContentsRoot, assetPath);

                        string targetPathDisplay = string.IsNullOrWhiteSpace(gameObjectPath) ? "(root)" : gameObjectPath;

                        return McpUtils.Success(
                            $"Successfully removed component '{componentType.Name}' from GameObject.\n" +
                            $"GameObject path: {targetPathDisplay}\n" +
                            $"Component type: {componentType.FullName}\n" +
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
                    return McpUtils.Error($"Failed to remove component from prefab: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }
    }
}
