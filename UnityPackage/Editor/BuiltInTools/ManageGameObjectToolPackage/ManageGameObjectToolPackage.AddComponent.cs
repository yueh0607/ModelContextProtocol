using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// ManageGameObjectToolPackage - Component 添加部分
    /// </summary>
    public partial class ManageGameObjectToolPackage
    {
        /// <summary>
        /// 向 Prefab 中的 GameObject 添加组件
        /// </summary>
        [McpTool(
            Description = "Add components to a GameObject in a Prefab. IMPORTANT: Always add multiple components in a single call using comma-separated names (e.g., 'BoxCollider,Rigidbody,MeshRenderer') for better performance. Supports all Unity built-in components and custom MonoBehaviour scripts.",
            Category = "Component Management"
        )]
        public async Task<CallToolResult> AddComponentToPrefab(
            [McpParameter("Path to the prefab file (supports fuzzy path matching)")]
            [FuzzyPathProcessor(FileExtension = ".prefab", AssetType = "t:Prefab", KeepOriginalIfNotFound = false)]
            string prefabPath,
            [McpParameter("Hierarchy path to the GameObject to add component to (e.g., 'Parent/Child'). Leave empty for root GameObject.")]
            [TrimProcessor]
            string gameObjectPath = "",
            [McpParameter("Component type names, comma-separated for batch adding (e.g., 'BoxCollider,Rigidbody,MeshRenderer'). Single component example: 'BoxCollider'. For custom scripts, use the full type name.")]
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

                        // 分割多个组件类型名称
                        string[] typeNames = componentTypeName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var addedComponents = new System.Collections.Generic.List<Component>();
                        var errors = new System.Collections.Generic.List<string>();

                        foreach (string typeName in typeNames)
                        {
                            string trimmedTypeName = typeName.Trim();
                            if (string.IsNullOrEmpty(trimmedTypeName))
                                continue;

                            // 查找组件类型
                            Type componentType = FindComponentType(trimmedTypeName);
                            if (componentType == null)
                            {
                                errors.Add($"Component type '{trimmedTypeName}' not found.");
                                continue;
                            }

                            // 检查是否已经有该组件
                            if (targetObject.GetComponent(componentType) != null)
                            {
                                errors.Add($"GameObject already has component '{componentType.Name}'.");
                                continue;
                            }

                            // 添加组件
                            Component addedComponent = targetObject.AddComponent(componentType);

                            if (addedComponent == null)
                            {
                                errors.Add($"Failed to add component '{componentType.Name}'.");
                                continue;
                            }

                            addedComponents.Add(addedComponent);
                        }

                        // 保存修改后的 Prefab
                        if (addedComponents.Count > 0)
                        {
                            PrefabUtility.SaveAsPrefabAsset(prefabContentsRoot, assetPath);
                        }

                        string targetPathDisplay = string.IsNullOrWhiteSpace(gameObjectPath) ? "(root)" : gameObjectPath;

                        // 构建结果消息
                        var resultBuilder = new System.Text.StringBuilder();

                        if (addedComponents.Count > 0)
                        {
                            resultBuilder.AppendLine($"Successfully added {addedComponents.Count} component(s) to GameObject.");
                            resultBuilder.AppendLine($"GameObject path: {targetPathDisplay}");
                            resultBuilder.AppendLine();
                            resultBuilder.AppendLine("Added components:");
                            foreach (var comp in addedComponents)
                            {
                                resultBuilder.AppendLine($"  - {comp.GetType().Name} ({comp.GetType().FullName})");
                            }
                            resultBuilder.AppendLine();
                            resultBuilder.AppendLine($"Prefab saved at: {assetPath}");
                        }

                        if (errors.Count > 0)
                        {
                            if (addedComponents.Count > 0)
                            {
                                resultBuilder.AppendLine();
                                resultBuilder.AppendLine("Warnings:");
                            }
                            foreach (var error in errors)
                            {
                                resultBuilder.AppendLine($"  - {error}");
                            }
                        }

                        if (addedComponents.Count == 0)
                        {
                            return McpUtils.Error($"Failed to add any components:\n{string.Join("\n", errors)}");
                        }

                        return McpUtils.Success(resultBuilder.ToString());
                    }
                    finally
                    {
                        // 确保卸载 Prefab 内容
                        PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
                    }
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"Failed to add component to prefab: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }



        /// <summary>
        /// 查找组件类型（支持简单名称和完整名称）
        /// </summary>
        private Type FindComponentType(string typeName)
        {
            // 尝试从 UnityEngine 命名空间查找
            Type type = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (type != null && typeof(Component).IsAssignableFrom(type))
            {
                return type;
            }

            // 尝试从 UnityEngine.UI 命名空间查找
            type = Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI");
            if (type != null && typeof(Component).IsAssignableFrom(type))
            {
                return type;
            }

            // 尝试直接使用完整类型名查找
            type = Type.GetType(typeName);
            if (type != null && typeof(Component).IsAssignableFrom(type))
            {
                return type;
            }

            // 在所有已加载的程序集中搜索
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 先尝试简单名称
                type = assembly.GetTypes().FirstOrDefault(t =>
                    t.Name == typeName && typeof(Component).IsAssignableFrom(t));
                if (type != null)
                {
                    return type;
                }

                // 再尝试完整名称
                type = assembly.GetTypes().FirstOrDefault(t =>
                    t.FullName == typeName && typeof(Component).IsAssignableFrom(t));
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
