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
            Description = "Get properties from a component. If propertyPath is empty, returns all available property names. If specified, returns values for those properties. Supports multiple properties (comma-separated) and nested access (e.g., 'position.x,enabled' or 'materials[0].name').",
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
            [McpParameter("Property paths to query, comma-separated (e.g., 'position.x,enabled' or 'materials[0].name'). Leave empty to list all property names.")]
            [TrimProcessor]
            string propertyPath = "",
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

                        StringBuilder sb = new StringBuilder();
                        string targetPathDisplay = string.IsNullOrWhiteSpace(gameObjectPath) ? "(root)" : gameObjectPath;

                        sb.AppendLine($"Component: {componentType.Name}");
                        sb.AppendLine($"GameObject: {targetObject.name}");
                        sb.AppendLine($"Path: {targetPathDisplay}");
                        sb.AppendLine();

                        // 如果指定了 propertyPath，查询特定属性（支持多个，用逗号分隔）
                        if (!string.IsNullOrWhiteSpace(propertyPath))
                        {
                            // 分割多个属性路径
                            string[] paths = propertyPath.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string path in paths)
                            {
                                string trimmedPath = path.Trim();
                                if (string.IsNullOrEmpty(trimmedPath))
                                    continue;

                                try
                                {
                                    object value = GetPropertyValueByPath(component, componentType, trimmedPath, includeInherited);
                                    string valueStr = FormatPropertyValue(value, value?.GetType());
                                    sb.AppendLine($"{trimmedPath} : {valueStr}");
                                }
                                catch (Exception ex)
                                {
                                    sb.AppendLine($"{trimmedPath} : <Error: {ex.Message}>");
                                }
                            }
                        }
                        else
                        {
                            // 否则列出所有属性名
                            var propertyNames = GetComponentPropertyNames(component, componentType, includeInherited);

                            if (propertyNames.Count == 0)
                            {
                                sb.AppendLine("(No properties or fields found)");
                            }
                            else
                            {
                                sb.AppendLine("Available properties:");
                                foreach (var name in propertyNames)
                                {
                                    sb.AppendLine($"  - {name}");
                                }
                                sb.AppendLine();
                                sb.AppendLine($"Total: {propertyNames.Count} properties");
                                sb.AppendLine();
                                sb.AppendLine("Use 'propertyPath' parameter to get specific property values.");
                                sb.AppendLine("Supports multiple properties (comma-separated) and nested access.");
                                sb.AppendLine("Examples: 'enabled', 'position.x', 'position.x,position.y,enabled'");
                            }
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
            public string Value { get; set; }
        }

        /// <summary>
        /// 获取组件的所有属性信息
        /// 只返回：1. 公开的字段和属性（属性必须是公开读）2. 带SerializeFieldAttribute的字段
        /// </summary>
        private List<PropertyInfo_> GetComponentPropertiesInfo(Component component, Type componentType, bool includeInherited)
        {
            var result = new List<PropertyInfo_>();
            var processedNames = new HashSet<string>();

            BindingFlags publicFlags = BindingFlags.Public | BindingFlags.Instance;
            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (!includeInherited)
            {
                publicFlags |= BindingFlags.DeclaredOnly;
                allFlags |= BindingFlags.DeclaredOnly;
            }

            // 1. 获取所有字段（包括公开和私有）
            FieldInfo[] allFields = componentType.GetFields(allFlags);
            foreach (var field in allFields)
            {
                if (processedNames.Contains(field.Name))
                    continue;

                // 只包含：公开字段 或 带SerializeField特性的字段
                bool isPublic = field.IsPublic;
                bool hasSerializeField = field.GetCustomAttribute<SerializeField>() != null;

                if (!isPublic && !hasSerializeField)
                    continue;

                processedNames.Add(field.Name);

                try
                {
                    object value = field.GetValue(component);
                    result.Add(new PropertyInfo_
                    {
                        Name = field.Name,
                        Value = FormatPropertyValue(value, field.FieldType)
                    });
                }
                catch
                {
                    // 跳过无法读取的字段
                }
            }

            // 2. 获取所有公开属性（必须可读）
            PropertyInfo[] properties = componentType.GetProperties(publicFlags);
            foreach (var property in properties)
            {
                if (processedNames.Contains(property.Name))
                    continue;

                // 跳过索引器属性
                if (property.GetIndexParameters().Length > 0)
                    continue;

                // 必须是可读的
                if (!property.CanRead)
                    continue;

                processedNames.Add(property.Name);

                try
                {
                    object value = property.GetValue(component);
                    string valueStr = FormatPropertyValue(value, property.PropertyType);

                    result.Add(new PropertyInfo_
                    {
                        Name = property.Name,
                        Value = valueStr
                    });
                }
                catch
                {
                    // 跳过无法读取的属性
                }
            }

            // 按名称排序
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            return result;
        }

        /// <summary>
        /// 获取组件的所有可访问属性名称列表
        /// </summary>
        private List<string> GetComponentPropertyNames(Component component, Type componentType, bool includeInherited)
        {
            var result = new List<string>();
            var processedNames = new HashSet<string>();

            BindingFlags publicFlags = BindingFlags.Public | BindingFlags.Instance;
            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (!includeInherited)
            {
                publicFlags |= BindingFlags.DeclaredOnly;
                allFlags |= BindingFlags.DeclaredOnly;
            }

            // 获取字段
            FieldInfo[] allFields = componentType.GetFields(allFlags);
            foreach (var field in allFields)
            {
                if (processedNames.Contains(field.Name))
                    continue;

                bool isPublic = field.IsPublic;
                bool hasSerializeField = field.GetCustomAttribute<SerializeField>() != null;

                if (!isPublic && !hasSerializeField)
                    continue;

                try
                {
                    // 尝试读取以确保可访问
                    field.GetValue(component);
                    processedNames.Add(field.Name);
                    result.Add(field.Name);
                }
                catch
                {
                    // 跳过无法读取的字段
                }
            }

            // 获取属性
            PropertyInfo[] properties = componentType.GetProperties(publicFlags);
            foreach (var property in properties)
            {
                if (processedNames.Contains(property.Name))
                    continue;

                if (property.GetIndexParameters().Length > 0)
                    continue;

                if (!property.CanRead)
                    continue;

                try
                {
                    // 尝试读取以确保可访问
                    property.GetValue(component);
                    processedNames.Add(property.Name);
                    result.Add(property.Name);
                }
                catch
                {
                    // 跳过无法读取的属性
                }
            }

            result.Sort();
            return result;
        }

        /// <summary>
        /// 根据路径获取属性值，支持嵌套访问和数组索引
        /// 例如: "position.x", "materials[0].name"
        /// </summary>
        private object GetPropertyValueByPath(Component component, Type componentType, string path, bool includeInherited)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Property path cannot be empty");

            object currentObject = component;
            Type currentType = componentType;
            string[] parts = SplitPropertyPath(path);

            foreach (string part in parts)
            {
                if (currentObject == null)
                    throw new Exception($"Cannot access property '{part}' on null object");

                // 检查是否是数组/列表索引访问 [index]
                if (part.Contains("[") && part.EndsWith("]"))
                {
                    int bracketStart = part.IndexOf('[');
                    string propertyName = part.Substring(0, bracketStart);
                    string indexStr = part.Substring(bracketStart + 1, part.Length - bracketStart - 2);

                    if (!int.TryParse(indexStr, out int index))
                        throw new Exception($"Invalid array index: {indexStr}");

                    // 如果有属性名，先获取属性
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        currentObject = GetPropertyOrFieldValue(currentObject, currentType, propertyName, includeInherited);
                        if (currentObject == null)
                            throw new Exception($"Property '{propertyName}' is null");
                        currentType = currentObject.GetType();
                    }

                    // 访问数组/列表元素
                    if (currentObject is Array array)
                    {
                        if (index < 0 || index >= array.Length)
                            throw new Exception($"Array index {index} out of bounds (length: {array.Length})");
                        currentObject = array.GetValue(index);
                    }
                    else if (currentObject is System.Collections.IList list)
                    {
                        if (index < 0 || index >= list.Count)
                            throw new Exception($"List index {index} out of bounds (count: {list.Count})");
                        currentObject = list[index];
                    }
                    else
                    {
                        throw new Exception($"Object is not an array or list: {currentType.Name}");
                    }

                    currentType = currentObject?.GetType();
                }
                else
                {
                    // 普通属性访问
                    currentObject = GetPropertyOrFieldValue(currentObject, currentType, part, includeInherited);
                    currentType = currentObject?.GetType();
                }
            }

            return currentObject;
        }

        /// <summary>
        /// 分割属性路径，支持点号和数组访问
        /// 例如: "position.x" -> ["position", "x"]
        ///       "materials[0].name" -> ["materials[0]", "name"]
        /// </summary>
        private string[] SplitPropertyPath(string path)
        {
            var parts = new List<string>();
            var currentPart = new StringBuilder();
            int bracketDepth = 0;

            foreach (char c in path)
            {
                if (c == '[')
                {
                    bracketDepth++;
                    currentPart.Append(c);
                }
                else if (c == ']')
                {
                    bracketDepth--;
                    currentPart.Append(c);
                }
                else if (c == '.' && bracketDepth == 0)
                {
                    if (currentPart.Length > 0)
                    {
                        parts.Add(currentPart.ToString());
                        currentPart.Clear();
                    }
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
            }

            return parts.ToArray();
        }

        /// <summary>
        /// 获取对象的属性或字段值
        /// </summary>
        private object GetPropertyOrFieldValue(object obj, Type type, string name, bool includeInherited)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            if (!includeInherited)
            {
                flags |= BindingFlags.DeclaredOnly;
            }

            // 尝试作为属性
            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.CanRead)
            {
                return property.GetValue(obj);
            }

            // 尝试作为字段
            FieldInfo field = type.GetField(name, flags);
            if (field != null)
            {
                // 检查是否是公开字段或有 SerializeField 特性
                bool isPublic = field.IsPublic;
                bool hasSerializeField = field.GetCustomAttribute<SerializeField>() != null;

                if (isPublic || hasSerializeField)
                {
                    return field.GetValue(obj);
                }
            }

            throw new Exception($"Property or field '{name}' not found or not accessible on type '{type.Name}'");
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

            // 如果类型未知，使用实际值的类型
            if (propertyType == null)
                propertyType = value.GetType();

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
