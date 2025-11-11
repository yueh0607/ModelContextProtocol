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
    /// ManageGameObjectToolPackage - GameObject 删除部分
    /// </summary>
    public partial class ManageGameObjectToolPackage
    {
        /// <summary>
        /// 从 Prefab 中删除指定的 GameObject
        /// </summary>
        [McpTool(
            Description = "Delete a GameObject from a Prefab by its path in the hierarchy (e.g., 'Parent/Child/Target')",
            Category = "Prefab Modification"
        )]
        public async Task<CallToolResult> DeleteGameObjectFromPrefab(
            [McpParameter("Path to the prefab file (supports fuzzy path matching)")]
            [FuzzyPathProcessor(FileExtension = ".prefab", AssetType = "t:Prefab", KeepOriginalIfNotFound = false)]
            string prefabPath,
            [McpParameter("Hierarchy path to the GameObject to delete (e.g., 'Parent/Child/Target'). Use '/' to separate levels.")]
            [TrimProcessor]
            string gameObjectPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return McpUtils.Error("Required parameter 'prefabPath' is missing or empty.");
            }

            if (string.IsNullOrWhiteSpace(gameObjectPath))
            {
                return McpUtils.Error("Required parameter 'gameObjectPath' is missing or empty.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    // 加载 Prefab 资源
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        return McpUtils.Error($"Failed to load prefab at path: {prefabPath}");
                    }

                    // 使用 PrefabUtility 打开 Prefab 进行编辑
                    string assetPath = AssetDatabase.GetAssetPath(prefab);
                    GameObject prefabContentsRoot = PrefabUtility.LoadPrefabContents(assetPath);

                    try
                    {
                        // 查找目标 GameObject
                        Transform targetTransform = FindTransformByPath(prefabContentsRoot.transform, gameObjectPath);

                        if (targetTransform == null)
                        {
                            return McpUtils.Error(
                                $"GameObject not found at path: {gameObjectPath}\n" +
                                $"Please use ReadPrefabStructure to view the hierarchy and find the correct path.");
                        }

                        // 不允许删除根对象
                        if (targetTransform == prefabContentsRoot.transform)
                        {
                            return McpUtils.Error(
                                "Cannot delete the root GameObject of a prefab. " +
                                "If you need to delete it, please delete the prefab file directly.");
                        }

                        string deletedObjectName = targetTransform.name;
                        string parentPath = GetTransformPath(targetTransform.parent, prefabContentsRoot.transform);

                        // 删除 GameObject
                        UnityEngine.Object.DestroyImmediate(targetTransform.gameObject);

                        // 保存修改后的 Prefab
                        PrefabUtility.SaveAsPrefabAsset(prefabContentsRoot, assetPath);

                        return McpUtils.Success(
                            $"Successfully deleted GameObject '{deletedObjectName}' from prefab.\n" +
                            $"Parent path: {(string.IsNullOrEmpty(parentPath) ? "(root)" : parentPath)}\n" +
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
                    return McpUtils.Error($"Failed to delete GameObject from prefab: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 根据路径查找 Transform
        /// </summary>
        private Transform FindTransformByPath(Transform root, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return root;
            }

            // 分割路径
            string[] pathParts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            Transform current = root;

            foreach (string part in pathParts)
            {
                Transform found = null;

                // 在当前层级查找匹配的子对象
                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);
                    if (string.Equals(child.name, part, StringComparison.OrdinalIgnoreCase))
                    {
                        found = child;
                        break;
                    }
                }

                if (found == null)
                {
                    // 没找到匹配的子对象
                    return null;
                }

                current = found;
            }

            return current;
        }

        /// <summary>
        /// 获取 Transform 的完整路径
        /// </summary>
        private string GetTransformPath(Transform transform, Transform root)
        {
            if (transform == null || transform == root)
            {
                return "";
            }

            string path = transform.name;
            Transform current = transform.parent;

            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
