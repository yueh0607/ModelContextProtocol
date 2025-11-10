using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Json.Linq;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityAIStudio.McpServer.Editor.Window.Models;
using UnityEngine;

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
                var userAssembly = GetAssemblyByName(assemblies, "McpServer.ProjectTools");
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
                    && a.GetName().Name == assemblyName);
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

                                // 应用参数处理器链
                                object processedValue = ApplyParameterProcessors(param, paramValues[i]);

                                // 如果处理器返回 CallToolResult 错误，直接返回
                                if (processedValue is CallToolResult errorResult)
                                {
                                    return errorResult;
                                }

                                paramValues[i] = processedValue;

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
                            // 对默认值也应用参数处理器链
                            object processedValue = ApplyParameterProcessors(param, paramValues[i]);

                            // 如果处理器返回 CallToolResult 错误，直接返回
                            if (processedValue is CallToolResult errorResult)
                            {
                                return errorResult;
                            }

                            paramValues[i] = processedValue;
                        }
                        else if (param.HasDefaultValue)
                        {
                            paramValues[i] = param.DefaultValue;
                            // 对默认值也应用参数处理器链
                            object processedValue = ApplyParameterProcessors(param, paramValues[i]);

                            // 如果处理器返回 CallToolResult 错误，直接返回
                            if (processedValue is CallToolResult errorResult)
                            {
                                return errorResult;
                            }

                            paramValues[i] = processedValue;
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

                                // 应用参数处理器链
                                object processedValue = ApplyParameterProcessors(param, paramValues[i]);

                                // 如果处理器返回 CallToolResult 错误，直接返回
                                if (processedValue is CallToolResult errorResult)
                                {
                                    return errorResult;
                                }

                                paramValues[i] = processedValue;
                            }
                            catch (Exception ex)
                            {
                                return CreateErrorResult($"Invalid parameter '{paramName}': {ex.Message}");
                            }
                        }
                        else if (paramAttr?.DefaultValue != null)
                        {
                            paramValues[i] = paramAttr.DefaultValue;
                            // 对默认值也应用参数处理器链
                            object processedValue = ApplyParameterProcessors(param, paramValues[i]);

                            // 如果处理器返回 CallToolResult 错误，直接返回
                            if (processedValue is CallToolResult errorResult)
                            {
                                return errorResult;
                            }

                            paramValues[i] = processedValue;
                        }
                        else if (param.HasDefaultValue)
                        {
                            paramValues[i] = param.DefaultValue;
                            // 对默认值也应用参数处理器链
                            object processedValue = ApplyParameterProcessors(param, paramValues[i]);

                            // 如果处理器返回 CallToolResult 错误，直接返回
                            if (processedValue is CallToolResult errorResult)
                            {
                                return errorResult;
                            }

                            paramValues[i] = processedValue;
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

        /// <summary>
        /// 应用参数处理器链
        /// </summary>
        /// <param name="param">参数信息</param>
        /// <param name="value">原始参数值</param>
        /// <returns>处理后的参数值</returns>
        private static object ApplyParameterProcessors(ParameterInfo param, object value)
        {
            if (value == null || param == null)
            {
                return value;
            }

            try
            {
                // 获取参数上的所有处理器特性
                var processors = param.GetCustomAttributes<McpParameterProcessorAttribute>()
                    .OrderBy(p => p.Order)
                    .ToList();

                if (processors.Count == 0)
                {
                    return value;
                }

                // 依次应用每个处理器
                object currentValue = value;
                foreach (var processor in processors)
                {
                    try
                    {
                        object processedValue = processor.Process(currentValue, param.ParameterType);

                        // 如果处理器返回 CallToolResult，直接返回（可能是错误或其他结果）
                        if (processedValue is CallToolResult)
                        {
                            return processedValue;
                        }

                        // 如果处理器返回 null，表示处理失败或不修改，继续使用当前值
                        if (processedValue != null)
                        {
                            currentValue = processedValue;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // ArgumentException 表示参数验证失败，应该向上传播
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // 其他异常：处理器执行失败，记录日志但继续使用当前值
                        Debug.LogWarning($"[MCP Tool] Parameter processor {processor.GetType().Name} failed for parameter '{param.Name}': {ex.Message}");
                    }
                }

                return currentValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Tool] Error applying parameter processors for '{param.Name}': {ex}");
                return value; // 出错时返回原始值
            }
        }

        /// <summary>
        /// 从所有相关程序集中发现ToolPackage信息（内置+用户自定义）
        /// </summary>
        public static List<McpToolPackage> DiscoverAllToolPackages()
        {
            var toolPackages = new List<McpToolPackage>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var builtInToolPackages = DiscoverToolPackages(GetAssemblyByName(assemblies, "McpServer.BuiltInTools"));
            toolPackages.AddRange(builtInToolPackages);
            Debug.Log($"[MCP Discovery] Discovered {builtInToolPackages.Count} built-in tool packages");

            try
            {
                var userAssembly = GetAssemblyByName(assemblies, "McpServer.ProjectTools");
                if (userAssembly != null)
                {
                    var userToolPackages = DiscoverToolPackages(userAssembly);
                    toolPackages.AddRange(userToolPackages);
                    Debug.Log($"[MCP Discovery] Discovered {userToolPackages.Count} user-defined tool packages");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP Discovery] Failed to discover user tool packages: {ex.Message}");
            }

            Debug.Log($"[MCP Discovery] Total tool packages discovered: {toolPackages.Count}");
            return toolPackages;
        }

        /// <summary>
        /// 从指定程序集中发现所有ToolPackage信息
        /// </summary>
        public static List<McpToolPackage> DiscoverToolPackages(Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetExecutingAssembly();
            }

            var toolPackageList = new List<McpToolPackage>();

            // 查找所有带有McpToolClass特性的类
            var toolClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<McpToolClassAttribute>() != null
                    && !t.IsAbstract
                    && t.IsClass
                )
                .ToList();

            foreach (var toolClass in toolClasses)
            {
                var classAttr = toolClass.GetCustomAttribute<McpToolClassAttribute>();

                // 查找该类中所有带有McpTool特性的方法
                var methods = toolClass.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<McpToolAttribute>() != null)
                    .ToList();

                // 获取工具名称列表
                var toolNames = methods.Select(m =>
                {
                    var toolAttr = m.GetCustomAttribute<McpToolAttribute>();
                    return string.IsNullOrEmpty(toolAttr.Name) ? m.Name : toolAttr.Name;
                }).ToList();

                // 创建McpToolPackage对象
                var className = toolClass.FullName ?? toolClass.Name;
                var displayName = toolClass.Name;
                var category = classAttr?.Category ?? "General";
                var description = classAttr?.Description ?? $"Tools from {displayName}";

                var mcpToolPackage = new McpToolPackage(className, displayName, category, description, methods.Count);
                mcpToolPackage.toolNames = toolNames;

                // 从EditorPrefs加载启用/禁用状态
                mcpToolPackage.enabled = UnityEditor.EditorPrefs.GetBool(mcpToolPackage.GetPrefsKey(), true);

                toolPackageList.Add(mcpToolPackage);
            }

            return toolPackageList;
        }

        /// <summary>
        /// 根据启用的ToolPackage过滤工具
        /// </summary>
        public static List<SimpleMcpServerTool> DiscoverAllToolsWithFilter(HashSet<string> enabledToolPackages)
        {
            var tools = new List<SimpleMcpServerTool>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var builtInTools = DiscoverToolsWithFilter(GetAssemblyByName(assemblies, "McpServer.BuiltInTools"), enabledToolPackages);
            tools.AddRange(builtInTools);
            Debug.Log($"[MCP Discovery] Discovered {builtInTools.Count} enabled built-in tools");

            try
            {
                var userAssembly = GetAssemblyByName(assemblies, "McpServer.ProjectTools");
                if (userAssembly != null)
                {
                    var userTools = DiscoverToolsWithFilter(userAssembly, enabledToolPackages);
                    tools.AddRange(userTools);
                    Debug.Log($"[MCP Discovery] Discovered {userTools.Count} enabled user-defined tools");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP Discovery] Failed to discover user tools: {ex.Message}");
            }

            Debug.Log($"[MCP Discovery] Total enabled tools discovered: {tools.Count}");
            return tools;
        }

        /// <summary>
        /// 从指定程序集中发现所有启用的工具
        /// </summary>
        private static List<SimpleMcpServerTool> DiscoverToolsWithFilter(Assembly assembly, HashSet<string> enabledToolPackages)
        {
            if (assembly == null)
            {
                return new List<SimpleMcpServerTool>();
            }

            var tools = new List<SimpleMcpServerTool>();

            // 查找所有带有McpToolClass特性的类
            var toolClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<McpToolClassAttribute>() != null
                    && !t.IsAbstract
                    && t.IsClass
                )
                .ToList();

            foreach (var toolClass in toolClasses)
            {
                var className = toolClass.FullName ?? toolClass.Name;

                // 检查这个ToolPackage是否被启用
                if (!enabledToolPackages.Contains(className))
                {
                    Debug.Log($"[MCP Discovery] Skipping disabled tool package: {className}");
                    continue;
                }

                var classAttr = toolClass.GetCustomAttribute<McpToolClassAttribute>();
                Debug.Log($"[MCP Discovery] Processing enabled class: {toolClass.Name}, Category: {classAttr?.Category}");

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

            return tools;
        }
    }
}
