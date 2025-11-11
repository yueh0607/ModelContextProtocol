using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// ManageGameObjectToolPackage - Prefab 结构读取部分
    /// </summary>
    public partial class ManageGameObjectToolPackage
    {
        /// <summary>
        /// 读取 Prefab 的层级结构
        /// </summary>
        [McpTool(
            Description = "Read the hierarchy structure of a Prefab asset, showing all GameObjects and their components",
            Category = "Prefab Structure"
        )]
        public async Task<CallToolResult> ReadPrefabStructure(
            [McpParameter("Path to the prefab file (supports fuzzy path matching)")]
            [FuzzyPathProcessor(FileExtension = ".prefab", AssetType = "t:Prefab", KeepOriginalIfNotFound = false)]
            string prefabPath,
            [McpParameter("Include component details in the output (default: true)")]
            bool includeComponents = true,
            [McpParameter("Maximum depth of hierarchy to display (0 = unlimited, default: 0)")]
            [RangeProcessor(Min = 0, Max = 100)]
            int maxDepth = 0,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return McpUtils.Error("Required parameter 'prefabPath' is missing or empty.");
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

                    // 构建层级结构信息
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Prefab: {prefab.name}");
                    sb.AppendLine($"Path: {prefabPath}");
                    sb.AppendLine();
                    sb.AppendLine("Hierarchy Structure:");
                    sb.AppendLine("===================");

                    BuildHierarchyTree(prefab.transform, sb, 0, includeComponents, maxDepth);

                    return McpUtils.Success(sb.ToString());
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"Failed to read prefab structure: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 递归构建层级树结构
        /// </summary>
        private void BuildHierarchyTree(
            Transform transform,
            StringBuilder sb,
            int depth,
            bool includeComponents,
            int maxDepth)
        {
            // 检查深度限制
            if (maxDepth > 0 && depth >= maxDepth)
            {
                return;
            }

            // 添加缩进
            string indent = new string(' ', depth * 2);

            // 添加 GameObject 名称和激活状态
            string activeStatus = transform.gameObject.activeSelf ? "" : " (inactive)";
            sb.AppendLine($"{indent}├─ {transform.name}{activeStatus}");

            // 如果需要显示组件信息
            if (includeComponents)
            {
                Component[] components = transform.GetComponents<Component>();
                foreach (Component comp in components)
                {
                    if (comp == null) continue;

                    // 跳过 Transform 组件（因为它总是存在）
                    if (comp is Transform) continue;

                    string componentIndent = indent + "   ";
                    string componentName = comp.GetType().Name;

                    // 为一些常见组件添加额外信息
                    string extraInfo = GetComponentExtraInfo(comp);
                    sb.AppendLine($"{componentIndent}• {componentName}{extraInfo}");
                }
            }

            // 递归处理子对象
            for (int i = 0; i < transform.childCount; i++)
            {
                BuildHierarchyTree(transform.GetChild(i), sb, depth + 1, includeComponents, maxDepth);
            }
        }

        /// <summary>
        /// 获取组件的额外信息
        /// </summary>
        private string GetComponentExtraInfo(Component comp)
        {
            switch (comp)
            {
                case MeshFilter mf:
                    return mf.sharedMesh != null ? $" ({mf.sharedMesh.name})" : " (no mesh)";

                case MeshRenderer mr:
                    return mr.sharedMaterials.Length > 0
                        ? $" ({mr.sharedMaterials.Length} material(s))"
                        : " (no materials)";

                case SkinnedMeshRenderer smr:
                    return smr.sharedMesh != null
                        ? $" ({smr.sharedMesh.name}, {smr.sharedMaterials.Length} material(s))"
                        : " (no mesh)";

                case Camera cam:
                    return $" ({(cam.orthographic ? "Orthographic" : "Perspective")})";

                case Light light:
                    return $" ({light.type})";

                case Canvas canvas:
                    return $" ({canvas.renderMode})";

                case Collider col when col.enabled:
                    return " (enabled)";

                case Collider col when !col.enabled:
                    return " (disabled)";

                case Rigidbody rb:
                    return rb.isKinematic ? " (kinematic)" : " (dynamic)";

                default:
                    return "";
            }
        }
    }
}
