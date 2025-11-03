using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;
using MapleModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityAIStudio.McpServer.Tools.Attributes;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// MCP工具反射发现器 - 自动发现并注册带有特性的工具
    /// </summary>
    public static class McpToolDiscovery
    {
        /// <summary>
        /// 从所有相关程序集中发现MCP工具（内置+用户自定义）
        /// </summary>
        public static List<SimpleMcpServerTool> DiscoverAllTools()
        {
            var tools = new List<SimpleMcpServerTool>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var builtInTools = DiscoverTools(GetAssemblyByName(assemblies, "McpServer.BuiltInTools"));
            tools.AddRange(builtInTools);
            Debug.Log($"[MCP Discovery] Discovered {builtInTools.Count} built-in tools");

            try
            {
                var userAssembly = GetAssemblyByName(assemblies,"McpServer.ProjectTools");
                if (userAssembly != null)
                {
                    var userTools = DiscoverTools(userAssembly);
                    tools.AddRange(userTools);
                    Debug.Log($"[MCP Discovery] Discovered {userTools.Count} user-defined tools from {userAssembly.GetName().Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP Discovery] Failed to discover user tools: {ex.Message}");
            }

            Debug.Log($"[MCP Discovery] Total tools discovered: {tools.Count}");
            return tools;
        }

        // 获取指定名称的程序集
        private static Assembly GetAssemblyByName(IEnumerable<Assembly> assemblies, string assemblyName)
        {
            return assemblies
                .FirstOrDefault(a => !a.IsDynamic
                    &&a.GetName().Name == assemblyName);
        }


        /// <summary>
        /// 从指定程序集中发现所有MCP工具
        /// </summary>
        public static List<SimpleMcpServerTool> DiscoverTools(Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetExecutingAssembly();
            }

            var tools = new List<SimpleMcpServerTool>();

            // 查找所有带有McpToolClass特性的类
            var toolClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<McpToolClassAttribute>() != null
                    && !t.IsAbstract
                    && t.IsClass
                )
                .ToList();

            Debug.Log($"[MCP Discovery] Found {toolClasses.Count} tool classes");

            foreach (var toolClass in toolClasses)
            {
                var classAttr = toolClass.GetCustomAttribute<McpToolClassAttribute>();
                Debug.Log($"[MCP Discovery] Processing class: {toolClass.Name}, Category: {classAttr?.Category}");

                // 查找该类中所有带有McpTool特性的方法
                var methods = toolClass.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<McpToolAttribute>() != null)
                    .ToList();

                Debug.Log($"[MCP Discovery]   Found {methods.Count} tool methods in {toolClass.Name}");

                foreach (var method in methods)
                {
                    try
                    {
                        var tool = CreateToolFromMethod(toolClass, method, classAttr);
                        if (tool != null)
                        {
                            tools.Add(tool);
                            Debug.Log($"[MCP Discovery]   ✓ Registered tool: {tool.ProtocolTool.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MCP Discovery] Failed to create tool from {toolClass.Name}.{method.Name}: {ex.Message}");
                    }
                }
            }

            Debug.Log($"[MCP Discovery] Total tools discovered: {tools.Count}");
            return tools;
        }

        /// <summary>
        /// 从方法创建工具
        /// </summary>
        private static SimpleMcpServerTool CreateToolFromMethod(Type toolClass, MethodInfo method, McpToolClassAttribute classAttr)
        {
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>();

            // 确定工具名称
            string toolName = string.IsNullOrEmpty(toolAttr.Name) ? method.Name : toolAttr.Name;

            // 确定类别
            string category = toolAttr.Category ?? classAttr?.Category ?? "General";

            // 确定描述 - 追加参数信息
            string description = toolAttr.Description ?? $"Execute {method.Name}";
            string parameterInfo = BuildParameterDescription(method);
            if (!string.IsNullOrEmpty(parameterInfo))
            {
                description = $"{description}\n\n{parameterInfo}";
            }

            // 创建工具实例（如果方法不是静态的）
            object instance = null;
            if (!method.IsStatic)
            {
                try
                {
                    instance = Activator.CreateInstance(toolClass);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MCP Discovery] Failed to create instance of {toolClass.Name}: {ex.Message}");
                    return null;
                }
            }

            // 创建工具
            var simpleTool = SimpleMcpServerTool.Create(
                name: toolName,
                description: description,
                handler: async (args, ct) =>
                {
                    return await InvokeToolMethod(instance, method, args, ct);
                }
            );
            // 写入类别到协议元数据，供 UI 展示
            if (simpleTool?.ProtocolTool != null)
            {
                simpleTool.ProtocolTool.Meta = simpleTool.ProtocolTool.Meta ?? new JObject();
                simpleTool.ProtocolTool.Meta["category"] = category;
            }
            return simpleTool;
        }

        /// <summary>
        /// 构建参数描述信息
        /// </summary>
        private static string BuildParameterDescription(MethodInfo method)
        {
            var parameters = method.GetParameters()
                .Where(p => p.ParameterType != typeof(CancellationToken)) // 排除系统参数
                .ToList();

            if (parameters.Count == 0)
            {
                return string.Empty;
            }

            var descriptionParts = new List<string> { "Parameters:" };

            foreach (var param in parameters)
            {
                var paramAttr = param.GetCustomAttribute<McpParameterAttribute>();
                var paramName = param.Name;
                var paramType = GetFriendlyTypeName(param.ParameterType);

                // 构建参数行
                var parts = new List<string> { $"- {paramName} ({paramType})" };

                // 必需/可选标记：仅根据是否存在默认值判断（特性 DefaultValue 或 方法签名默认值）
                bool hasDefault = (paramAttr?.DefaultValue != null) || param.HasDefaultValue;
                bool isRequired = !hasDefault;
                parts.Add(isRequired ? "[Required]" : "[Optional]");

                // 参数描述
                if (!string.IsNullOrEmpty(paramAttr?.Description))
                {
                    parts.Add($"- {paramAttr.Description}");
                }

                // 默认值
                if (paramAttr?.DefaultValue != null)
                {
                    parts.Add($"(Default: {paramAttr.DefaultValue})");
                }
                else if (param.HasDefaultValue && param.DefaultValue != null)
                {
                    parts.Add($"(Default: {param.DefaultValue})");
                }

                // 示例值
                if (!string.IsNullOrEmpty(paramAttr?.Example))
                {
                    parts.Add($"(Example: {paramAttr.Example})");
                }

                descriptionParts.Add(string.Join(" ", parts));
            }

            return string.Join("\n", descriptionParts);
        }

        /// <summary>
        /// 获取友好的类型名称
        /// </summary>
        private static string GetFriendlyTypeName(Type type)
        {
            // 处理可空类型
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                return $"{GetFriendlyTypeName(underlyingType)}?";
            }

            // 直接使用反射生成名称（无注册表）

            // 数组类型
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var rank = type.GetArrayRank();
                var brackets = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
                return $"{GetFriendlyTypeName(elementType)}{brackets}";
            }

            // 泛型类型
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();

                // 默认泛型格式
                var genericTypeName = genericDef.Name;
                var argNamesStr = string.Join(", ", genericArgs.Select(GetFriendlyTypeName));
                return $"{genericTypeName.Split('`')[0]}<{argNamesStr}>";
            }

            // 枚举类型
            if (type.IsEnum)
            {
                return $"{type.Name} (enum)";
            }

            // 接口类型
            if (type.IsInterface)
            {
                return $"{type.Name} (interface)";
            }

            // 默认返回类型名
            return type.Name;
        }

        /// <summary>
        /// 调用工具方法
        /// </summary>
        private static async Task<CallToolResult> InvokeToolMethod(
            object instance,
            MethodInfo method,
            JToken args,
            CancellationToken ct)
        {
            try
            {
                // 获取方法参数
                var parameters = method.GetParameters();
                var paramValues = new object[parameters.Length];

                // 支持两种传参形式：
                // 1) JArray（推荐）：函数的 N 个参数按位置序列化
                // 2) JObject（兼容）：按参数名键值映射
                if (args is JArray arr)
                {
                    int positionalIndex = 0;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        var paramAttr = param.GetCustomAttribute<McpParameterAttribute>();

                        if (param.ParameterType == typeof(CancellationToken))
                        {
                            paramValues[i] = ct;
                            continue;
                        }

                        JToken token = positionalIndex < arr.Count ? arr[positionalIndex] : null;
                        positionalIndex++;

                        if (token != null && token.Type != JTokenType.Null)
                        {
                            try
                            {
                                paramValues[i] = token.ToObject(param.ParameterType);
                                continue;
                            }
                            catch (Exception ex)
                            {
                                return CreateErrorResult($"Invalid parameter '{param.Name}': {ex.Message}");
                            }
                        }

                        // 缺参时仅当没有任何默认值才报错；否则使用特性默认值或方法签名默认值
                        if (paramAttr?.DefaultValue != null)
                        {
                            paramValues[i] = paramAttr.DefaultValue;
                        }
                        else if (param.HasDefaultValue)
                        {
                            paramValues[i] = param.DefaultValue;
                        }
                        else
                        {
                            return CreateErrorResult($"Required parameter '{param.Name}' is missing");
                        }
                    }
                }
                else
                {
                    var obj = args as JObject ?? new JObject();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        var paramAttr = param.GetCustomAttribute<McpParameterAttribute>();

                        if (param.ParameterType == typeof(CancellationToken))
                        {
                            paramValues[i] = ct;
                            continue;
                        }

                        string paramName = param.Name;
                        if (obj.ContainsKey(paramName))
                        {
                            try
                            {
                                paramValues[i] = obj[paramName].ToObject(param.ParameterType);
                            }
                            catch (Exception ex)
                            {
                                return CreateErrorResult($"Invalid parameter '{paramName}': {ex.Message}");
                            }
                        }
                        else if (paramAttr?.DefaultValue != null)
                        {
                            paramValues[i] = paramAttr.DefaultValue;
                        }
                        else if (param.HasDefaultValue)
                        {
                            paramValues[i] = param.DefaultValue;
                        }
                        else
                        {
                            return CreateErrorResult($"Required parameter '{paramName}' is missing");
                        }
                    }
                }

                // 调用方法
                var result = method.Invoke(instance, paramValues);

                // 处理返回值
                if (result is Task<CallToolResult> taskResult)
                {
                    return await taskResult;
                }
                else if (result is CallToolResult callResult)
                {
                    return callResult;
                }
                else if (result is Task<string> taskString)
                {
                    string text = await taskString;
                    return CreateSuccessResult(text);
                }
                else if (result is string str)
                {
                    return CreateSuccessResult(str);
                }
                else if (result is Task task)
                {
                    await task;
                    return CreateSuccessResult("Operation completed successfully");
                }
                else if (result != null)
                {
                    return CreateSuccessResult(result.ToString());
                }
                else
                {
                    return CreateSuccessResult("Operation completed successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Tool] Error invoking {method.Name}: {ex}");
                return CreateErrorResult($"Tool execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        private static CallToolResult CreateSuccessResult(string message)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = message }
                },
                IsError = false
            };
        }

        /// <summary>
        /// 创建错误结果
        /// </summary>
        private static CallToolResult CreateErrorResult(string errorMessage)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Error: {errorMessage}" }
                },
                IsError = true
            };
        }
    }
}
