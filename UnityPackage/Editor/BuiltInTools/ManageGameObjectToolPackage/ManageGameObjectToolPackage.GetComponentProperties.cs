using System;
using System.Collections.Generic;
using System.Reflection;
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
    /// ManageGameObjectToolPackage - Component 属性读取部分
    /// </summary>
    public partial class ManageGameObjectToolPackage
    {
        /// <summary>
        /// 获取 Prefab 中 GameObject 的组件属性
        /// </summary>
        [McpTool(
            Description = "Get all properties and their current values from a component in a Prefab. Shows both fields and properties.",
            Category = "Component Management"
        )]
        public async Task<CallToolResult> GetComponentProperties(
            [McpParameter("Path to the prefab file (supports fuzzy path matching)")]
            [FuzzyPathProcessor(FileExtension = ".prefab", AssetType = "t:Prefab", KeepOriginalIfNotFound = false)]
            string prefabPath,
            [McpParameter("Hierarchy path to the GameObject (e.g., 'Parent/Child'). Leave empty for root GameObject.")]
            [TrimProcessor]
            string gameObjectPath = "",
            [McpParameter("Type name of the component (e.g., 'BoxCollider', 'Transform')")]
            [TrimProcessor]
            string componentTypeName = null,
            [McpParameter("Include inherited properties from base classes (default: true)")]
            bool includeInherited = true,
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

                    // 打开 Prefab 进行读取
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

                        // 获取组件
                        Component component = targetObject.GetComponent(componentType);
                        if (component == null)
                        {
                            return McpUtils.Error(
                                $"Component '{componentType.Name}' not found on GameObject '{targetObject.name}'.\n" +
                                $"Use ReadPrefabStructure to see what components are attached.");
                        }

                        // 构建属性信息
                        StringBuilder sb = new StringBuilder();
                        string targetPathDisplay = string.IsNullOrWhiteSpace(gameObjectPath) ? "(root)" : gameObjectPath;

                        sb.AppendLine($"Component: {componentType.Name}");
                        sb.AppendLine($"Full Type: {componentType.FullName}");
                        sb.AppendLine($"GameObject: {targetObject.name}");
                        sb.AppendLine($"GameObject Path: {targetPathDisplay}");
                        sb.AppendLine($"Prefab: {prefabPath}");
                        sb.AppendLine();
                        sb.AppendLine("Properties and Fields:");
                        sb.AppendLine("======================");

                        var properties = GetComponentPropertiesInfo(component, componentType, includeInherited);

                        if (properties.Count == 0)
                        {
                            sb.AppendLine("(No public properties or fields found)");
                        }
                        else
                        {
                            foreach (var prop in properties)
                            {
                                sb.AppendLine($"  {prop.Name}");
                                sb.AppendLine($"    Type: {prop.TypeName}");
                                sb.AppendLine($"    Value: {prop.Value}");
                                sb.AppendLine($"    Access: {prop.Access}");
                                if (!string.IsNullOrEmpty(prop.DeclaringType))
                                {
                                    sb.AppendLine($"    Declared in: {prop.DeclaringType}");
                                }
                                sb.AppendLine();
                            }

                            sb.AppendLine($"Total: {properties.Count} properties/fields");
                        }

                        return McpUtils.Success(sb.ToString());
                    }
                    finally
                    {
                        // 确保卸载 Prefab 内容
                        PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
                    }
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"Failed to get component properties: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        /// <summary>
        /// 属性信息
        /// </summary>
        private class PropertyInfo_
        {
            public string Name { get; set; }
            public string TypeName { get; set; }
            public string Value { get; set; }
            public string Access { get; set; }
            public string DeclaringType { get; set; }
        }

        /// <summary>
        /// 获取组件的所有属性信息
        /// </summary>
        private List<PropertyInfo_> GetComponentPropertiesInfo(Component component, Type componentType, bool includeInherited)
        {
            var result = new List<PropertyInfo_>();
            var processedNames = new HashSet<string>();

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            if (!includeInherited)
            {
                flags |= BindingFlags.DeclaredOnly;
            }

            // 获取所有公共字段
            FieldInfo[] fields = componentType.GetFields(flags);
            foreach (var field in fields)
            {
                if (processedNames.Contains(field.Name))
                    continue;

                processedNames.Add(field.Name);

                try
                {
                    object value = field.GetValue(component);
                    result.Add(new PropertyInfo_
                    {
                        Name = field.Name,
                        TypeName = GetFriendlyTypeName(field.FieldType),
                        Value = FormatPropertyValue(value, field.FieldType),
                        Access = "Field (Read/Write)",
                        DeclaringType = includeInherited && field.DeclaringType != componentType
                            ? field.DeclaringType.Name
                            : null
                    });
                }
                catch (Exception ex)
                {
                    result.Add(new PropertyInfo_
                    {
                        Name = field.Name,
                        TypeName = GetFriendlyTypeName(field.FieldType),
                        Value = $"<Error: {ex.Message}>",
                        Access = "Field (Read/Write)",
                        DeclaringType = includeInherited && field.DeclaringType != componentType
                            ? field.DeclaringType.Name
                            : null
                    });
                }
            }

            // 获取所有公共属性
            PropertyInfo[] properties = componentType.GetProperties(flags);
            foreach (var property in properties)
            {
                if (processedNames.Contains(property.Name))
                    continue;

                // 跳过索引器属性
                if (property.GetIndexParameters().Length > 0)
                    continue;

                processedNames.Add(property.Name);

                try
                {
                    string access = "";
                    if (property.CanRead && property.CanWrite)
                        access = "Property (Read/Write)";
                    else if (property.CanRead)
                        access = "Property (Read Only)";
                    else if (property.CanWrite)
                        access = "Property (Write Only)";

                    object value = null;
                    string valueStr = "(not readable)";

                    if (property.CanRead)
                    {
                        try
                        {
                            value = property.GetValue(component);
                            valueStr = FormatPropertyValue(value, property.PropertyType);
                        }
                        catch (Exception ex)
                        {
                            valueStr = $"<Error: {ex.Message}>";
                        }
                    }

                    result.Add(new PropertyInfo_
                    {
                        Name = property.Name,
                        TypeName = GetFriendlyTypeName(property.PropertyType),
                        Value = valueStr,
                        Access = access,
                        DeclaringType = includeInherited && property.DeclaringType != componentType
                            ? property.DeclaringType.Name
                            : null
                    });
                }
                catch (Exception ex)
                {
                    result.Add(new PropertyInfo_
                    {
                        Name = property.Name,
                        TypeName = GetFriendlyTypeName(property.PropertyType),
                        Value = $"<Error: {ex.Message}>",
                        Access = "Property",
                        DeclaringType = includeInherited && property.DeclaringType != componentType
                            ? property.DeclaringType.Name
                            : null
                    });
                }
            }

            // 按名称排序
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            return result;
        }

        /// <summary>
        /// 获取友好的类型名称
        /// </summary>
        private string GetFriendlyTypeName(Type type)
        {
            if (type == null)
                return "Unknown";

            if (type.IsGenericType)
            {
                string name = type.Name.Substring(0, type.Name.IndexOf('`'));
                var args = type.GetGenericArguments();
                var argNames = new List<string>();
                foreach (var arg in args)
                {
                    argNames.Add(GetFriendlyTypeName(arg));
                }
                return $"{name}<{string.Join(", ", argNames)}>";
            }

            // 使用简短名称代替完整名称
            if (type.Namespace == "UnityEngine" || type.Namespace == "System")
            {
                return type.Name;
            }

            return type.FullName ?? type.Name;
        }

        /// <summary>
        /// 格式化属性值用于显示
        /// </summary>
        private string FormatPropertyValue(object value, Type propertyType)
        {
            if (value == null)
                return "null";

            // 基本类型
            if (propertyType.IsPrimitive || propertyType == typeof(string))
            {
                return value.ToString();
            }

            // Unity 常用类型
            if (value is Vector2 v2)
                return $"({v2.x:F2}, {v2.y:F2})";

            if (value is Vector3 v3)
                return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";

            if (value is Vector4 v4)
                return $"({v4.x:F2}, {v4.y:F2}, {v4.z:F2}, {v4.w:F2})";

            if (value is Quaternion q)
            {
                Vector3 euler = q.eulerAngles;
                return $"Euler({euler.x:F1}°, {euler.y:F1}°, {euler.z:F1}°)";
            }

            if (value is Color c)
                return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";

            if (value is Color32 c32)
                return $"RGBA({c32.r}, {c32.g}, {c32.b}, {c32.a})";

            if (value is Bounds bounds)
                return $"Center: {bounds.center}, Size: {bounds.size}";

            if (value is Rect rect)
                return $"({rect.x:F1}, {rect.y:F1}, {rect.width:F1}, {rect.height:F1})";

            // Unity Object 引用
            if (value is UnityEngine.Object unityObj)
            {
                if (unityObj == null) // Unity's null check
                    return "null";

                return $"{unityObj.GetType().Name} ({unityObj.name})";
            }

            // 枚举
            if (propertyType.IsEnum)
            {
                return value.ToString();
            }

            // 数组
            if (propertyType.IsArray)
            {
                Array array = value as Array;
                if (array == null)
                    return "null";

                return $"{propertyType.GetElementType().Name}[{array.Length}]";
            }

            // 列表
            if (value is System.Collections.IList list)
            {
                return $"List<{propertyType.GetGenericArguments()[0].Name}> ({list.Count} items)";
            }

            // 其他类型显示类型名
            return $"<{value.GetType().Name}>";
        }
    }
}
