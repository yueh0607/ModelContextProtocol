using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Json;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// ManageGameObjectToolPackage - Component 属性设置部分
    /// </summary>
    public partial class ManageGameObjectToolPackage
    {
        /// <summary>
        /// 设置 Prefab 中 GameObject 的组件属性（支持批量设置）
        /// </summary>
        [McpTool(
            Description = "Set one or multiple property values on a component in a Prefab. Supports batch operations via JSON and nested properties (e.g., 'material.color').",
            Category = "Component Management"
        )]
        public async Task<CallToolResult> SetComponentProperty(
            [McpParameter("Path to the prefab file (supports fuzzy path matching)")]
            [FuzzyPathProcessor(FileExtension = ".prefab", AssetType = "t:Prefab", KeepOriginalIfNotFound = false)]
            string prefabPath,
            [McpParameter("Hierarchy path to the GameObject (e.g., 'Parent/Child'). Leave empty for root GameObject.")]
            [TrimProcessor]
            string gameObjectPath = "",
            [McpParameter("Type name of the component (e.g., 'BoxCollider', 'Transform')")]
            [TrimProcessor]
            string componentTypeName = null,
            [McpParameter("JSON object containing property-value pairs for batch setting. Format: {\"propertyName1\": \"value1\", \"propertyName2\": \"value2\"}. Leave empty to use single property mode.")]
            [TrimProcessor]
            string propertiesJson = null,
            [McpParameter("(Single mode) Name of the property to set. Supports nested properties (e.g., 'size.x'). Only used if propertiesJson is empty.")]
            [TrimProcessor]
            string propertyName = null,
            [McpParameter("(Single mode) Value to set. Format:\n- Numbers: '123', '3.14'\n- Booleans: 'true', 'false'\n- Strings: 'Hello'\n- Vector3: '1,2,3'\n- Color: '#FF0000' or '1,0,0,1'\nOnly used if propertiesJson is empty.")]
            [TrimProcessor]
            string propertyValue = null,
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

            // 决定使用批量模式还是单个属性模式
            bool batchMode = !string.IsNullOrWhiteSpace(propertiesJson);

            if (!batchMode)
            {
                // 单个属性模式验证
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    return McpUtils.Error("Either 'propertiesJson' or 'propertyName' must be provided.");
                }

                if (propertyValue == null)
                {
                    return McpUtils.Error("Required parameter 'propertyValue' is missing when using single property mode.");
                }
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

                        // 获取组件
                        Component component = targetObject.GetComponent(componentType);
                        if (component == null)
                        {
                            return McpUtils.Error(
                                $"Component '{componentType.Name}' not found on GameObject '{targetObject.name}'.\n" +
                                $"Use ReadPrefabStructure to see what components are attached.");
                        }

                        List<SetPropertyResult> results = new List<SetPropertyResult>();

                        // 批量模式或单个属性模式
                        if (batchMode)
                        {
                            // 解析JSON
                            var parseResult = ParsePropertiesJson(propertiesJson);
                            if (!parseResult.Success)
                            {
                                return McpUtils.Error(parseResult.ErrorMessage);
                            }

                            // 批量设置属性
                            foreach (var kvp in parseResult.Properties)
                            {
                                var setResult = SetPropertyValue(component, kvp.Key, kvp.Value);
                                setResult.PropertyName = kvp.Key;
                                results.Add(setResult);
                            }
                        }
                        else
                        {
                            // 单个属性设置
                            var setResult = SetPropertyValue(component, propertyName, propertyValue);
                            setResult.PropertyName = propertyName;
                            results.Add(setResult);
                        }

                        // 检查是否有失败的属性设置
                        var failedResults = results.Where(r => !r.Success).ToList();
                        if (failedResults.Count > 0)
                        {
                            StringBuilder errorMsg = new StringBuilder();
                            errorMsg.AppendLine($"Failed to set {failedResults.Count} of {results.Count} properties:");
                            foreach (var failed in failedResults)
                            {
                                errorMsg.AppendLine($"  - {failed.PropertyName}: {failed.ErrorMessage}");
                            }

                            if (failedResults.Count < results.Count)
                            {
                                errorMsg.AppendLine($"\n{results.Count - failedResults.Count} properties were set successfully.");
                            }

                            return McpUtils.Error(errorMsg.ToString());
                        }

                        // 保存修改后的 Prefab
                        PrefabUtility.SaveAsPrefabAsset(prefabContentsRoot, assetPath);

                        string targetPathDisplay = string.IsNullOrWhiteSpace(gameObjectPath) ? "(root)" : gameObjectPath;

                        // 构建成功消息
                        StringBuilder successMsg = new StringBuilder();
                        successMsg.AppendLine($"Successfully set {results.Count} property(ies) on component '{componentType.Name}'.");
                        successMsg.AppendLine($"GameObject path: {targetPathDisplay}");
                        successMsg.AppendLine();
                        successMsg.AppendLine("Updated properties:");
                        foreach (var result in results)
                        {
                            successMsg.AppendLine($"  - {result.PropertyName} = {result.FormattedValue}");
                        }
                        successMsg.AppendLine();
                        successMsg.AppendLine($"Prefab saved at: {assetPath}");

                        return McpUtils.Success(successMsg.ToString());
                    }
                    finally
                    {
                        // 确保卸载 Prefab 内容
                        PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
                    }
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"Failed to set component property: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        /// <summary>
        /// 设置属性值的结果
        /// </summary>
        private class SetPropertyResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public string FormattedValue { get; set; }
            public string PropertyName { get; set; }
        }

        /// <summary>
        /// JSON 解析结果
        /// </summary>
        private class ParseJsonResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public Dictionary<string, string> Properties { get; set; }
        }

        /// <summary>
        /// 解析属性JSON字符串
        /// </summary>
        private ParseJsonResult ParsePropertiesJson(string jsonString)
        {
            try
            {
                var properties = new Dictionary<string, string>();

                // 使用项目的 JSON 库解析
                var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

                if (jsonObj == null || jsonObj.Count == 0)
                {
                    return new ParseJsonResult
                    {
                        Success = false,
                        ErrorMessage = "JSON is empty or invalid. Expected format: {\"property1\": \"value1\", \"property2\": \"value2\"}"
                    };
                }

                // 将所有值转换为字符串
                foreach (var kvp in jsonObj)
                {
                    properties[kvp.Key] = kvp.Value?.ToString() ?? "";
                }

                return new ParseJsonResult
                {
                    Success = true,
                    Properties = properties
                };
            }
            catch (Exception ex)
            {
                return new ParseJsonResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to parse JSON: {ex.Message}\nExpected format: {{\"property1\": \"value1\", \"property2\": \"value2\"}}"
                };
            }
        }

        /// <summary>
        /// 设置对象的属性值（支持嵌套属性）
        /// </summary>
        private SetPropertyResult SetPropertyValue(object obj, string propertyPath, string valueString)
        {
            try
            {
                // 分割属性路径（支持嵌套属性如 "material.color"）
                string[] pathParts = propertyPath.Split('.');

                object currentObject = obj;
                MemberInfo lastMember = null;
                object parentObject = null;

                // 遍历属性路径
                for (int i = 0; i < pathParts.Length; i++)
                {
                    string partName = pathParts[i];
                    Type currentType = currentObject.GetType();

                    // 查找字段或属性
                    FieldInfo field = currentType.GetField(partName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    PropertyInfo property = currentType.GetProperty(partName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    if (field == null && property == null)
                    {
                        return new SetPropertyResult
                        {
                            Success = false,
                            ErrorMessage = $"Property or field '{partName}' not found on type '{currentType.Name}'."
                        };
                    }

                    // 如果是最后一个部分，设置值
                    if (i == pathParts.Length - 1)
                    {
                        lastMember = (MemberInfo)field ?? property;
                        parentObject = currentObject;

                        Type memberType = field != null ? field.FieldType : property.PropertyType;
                        object convertedValue = ConvertValueFromString(valueString, memberType);

                        if (convertedValue == null && !IsNullableType(memberType))
                        {
                            return new SetPropertyResult
                            {
                                Success = false,
                                ErrorMessage = $"Failed to convert value '{valueString}' to type '{memberType.Name}'."
                            };
                        }

                        if (field != null)
                        {
                            field.SetValue(parentObject, convertedValue);
                        }
                        else if (property != null)
                        {
                            if (!property.CanWrite)
                            {
                                return new SetPropertyResult
                                {
                                    Success = false,
                                    ErrorMessage = $"Property '{partName}' is read-only."
                                };
                            }
                            property.SetValue(parentObject, convertedValue);
                        }

                        return new SetPropertyResult
                        {
                            Success = true,
                            FormattedValue = FormatValue(convertedValue)
                        };
                    }
                    else
                    {
                        // 不是最后一个部分，继续遍历
                        object nextObject = field != null ? field.GetValue(currentObject) : property.GetValue(currentObject);

                        if (nextObject == null)
                        {
                            return new SetPropertyResult
                            {
                                Success = false,
                                ErrorMessage = $"Property '{partName}' is null. Cannot access nested property."
                            };
                        }

                        currentObject = nextObject;
                    }
                }

                return new SetPropertyResult
                {
                    Success = false,
                    ErrorMessage = "Unknown error occurred while setting property."
                };
            }
            catch (Exception ex)
            {
                return new SetPropertyResult
                {
                    Success = false,
                    ErrorMessage = $"Error setting property: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 将字符串转换为指定类型的值
        /// </summary>
        private object ConvertValueFromString(string valueString, Type targetType)
        {
            try
            {
                // 处理 null 值
                if (string.IsNullOrWhiteSpace(valueString) && IsNullableType(targetType))
                {
                    return null;
                }

                // 基本类型
                if (targetType == typeof(string))
                    return valueString;

                if (targetType == typeof(int))
                    return int.Parse(valueString);

                if (targetType == typeof(float))
                    return float.Parse(valueString);

                if (targetType == typeof(double))
                    return double.Parse(valueString);

                if (targetType == typeof(bool))
                    return bool.Parse(valueString);

                if (targetType == typeof(long))
                    return long.Parse(valueString);

                // Unity 特定类型
                if (targetType == typeof(Vector2))
                    return ParseVector2(valueString);

                if (targetType == typeof(Vector3))
                    return ParseVector3(valueString);

                if (targetType == typeof(Vector4))
                    return ParseVector4(valueString);

                if (targetType == typeof(Quaternion))
                    return ParseQuaternion(valueString);

                if (targetType == typeof(Color))
                    return ParseColor(valueString);

                if (targetType == typeof(Color32))
                    return (Color32)ParseColor(valueString);

                // 枚举类型
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, valueString, true);
                }

                // 尝试使用 Convert
                if (targetType.IsPrimitive || targetType == typeof(decimal))
                {
                    return Convert.ChangeType(valueString, targetType);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 Vector2
        /// </summary>
        private Vector2 ParseVector2(string value)
        {
            string[] parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            }
            throw new FormatException($"Invalid Vector2 format: {value}");
        }

        /// <summary>
        /// 解析 Vector3
        /// </summary>
        private Vector3 ParseVector3(string value)
        {
            string[] parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            }
            throw new FormatException($"Invalid Vector3 format: {value}");
        }

        /// <summary>
        /// 解析 Vector4
        /// </summary>
        private Vector4 ParseVector4(string value)
        {
            string[] parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4)
            {
                return new Vector4(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
            }
            throw new FormatException($"Invalid Vector4 format: {value}");
        }

        /// <summary>
        /// 解析 Quaternion
        /// </summary>
        private Quaternion ParseQuaternion(string value)
        {
            string[] parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4)
            {
                return new Quaternion(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
            }
            throw new FormatException($"Invalid Quaternion format: {value}");
        }

        /// <summary>
        /// 解析 Color
        /// </summary>
        private Color ParseColor(string value)
        {
            // 支持十六进制格式 (#RRGGBB 或 #RRGGBBAA)
            if (value.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString(value, out Color color))
                {
                    return color;
                }
            }

            // 支持逗号分隔的 RGBA 格式 (0-1 或 0-255)
            string[] parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                float r = float.Parse(parts[0]);
                float g = float.Parse(parts[1]);
                float b = float.Parse(parts[2]);
                float a = parts.Length >= 4 ? float.Parse(parts[3]) : 1f;

                // 如果值大于1，假定是 0-255 范围
                if (r > 1 || g > 1 || b > 1)
                {
                    r /= 255f;
                    g /= 255f;
                    b /= 255f;
                    if (a > 1) a /= 255f;
                }

                return new Color(r, g, b, a);
            }

            throw new FormatException($"Invalid Color format: {value}");
        }

        /// <summary>
        /// 检查类型是否可为 null
        /// </summary>
        private bool IsNullableType(Type type)
        {
            return !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);
        }

        /// <summary>
        /// 格式化值用于显示
        /// </summary>
        private string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is Vector2 v2)
                return $"({v2.x}, {v2.y})";

            if (value is Vector3 v3)
                return $"({v3.x}, {v3.y}, {v3.z})";

            if (value is Vector4 v4)
                return $"({v4.x}, {v4.y}, {v4.z}, {v4.w})";

            if (value is Quaternion q)
                return $"({q.x}, {q.y}, {q.z}, {q.w})";

            if (value is Color c)
                return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";

            return value.ToString();
        }
    }
}
