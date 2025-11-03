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

            // 1. 从内置工具程序集发现（当前程序集）
            var builtInTools = DiscoverTools(Assembly.GetExecutingAssembly());
            tools.AddRange(builtInTools);
            Debug.Log($"[MCP Discovery] Discovered {builtInTools.Count} built-in tools");

            // 2. 从用户程序集发现（没有 asmdef 的默认程序集）
            try
            {
                // 查找包含 UnityAIStudio.McpServer 命名空间但不是当前程序集的程序集
                var userAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => !a.IsDynamic
                        && a != Assembly.GetExecutingAssembly()
                        && a.GetName().Name.Contains("Assembly-CSharp-Editor"));

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
                .Where(t => t.GetCustomAttribute<McpToolClassAttribute>() != null)
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

            // 确定描述
            string description = toolAttr.Description ?? $"Execute {method.Name}";

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
            return SimpleMcpServerTool.Create(
                name: toolName,
                description: description,
                handler: async (args, ct) =>
                {
                    return await InvokeToolMethod(instance, method, args, ct);
                }
            );
        }

        /// <summary>
        /// 调用工具方法
        /// </summary>
        private static async Task<CallToolResult> InvokeToolMethod(
            object instance,
            MethodInfo method,
            JObject args,
            CancellationToken ct)
        {
            try
            {
                // 获取方法参数
                var parameters = method.GetParameters();
                var paramValues = new object[parameters.Length];

                // 构建参数值
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var paramAttr = param.GetCustomAttribute<McpParameterAttribute>();

                    // 特殊处理CancellationToken
                    if (param.ParameterType == typeof(CancellationToken))
                    {
                        paramValues[i] = ct;
                        continue;
                    }

                    // 从args中获取参数值
                    string paramName = param.Name;
                    if (args.ContainsKey(paramName))
                    {
                        try
                        {
                            paramValues[i] = args[paramName].ToObject(param.ParameterType);
                        }
                        catch (Exception ex)
                        {
                            return CreateErrorResult($"Invalid parameter '{paramName}': {ex.Message}");
                        }
                    }
                    else if (paramAttr?.Required == true)
                    {
                        return CreateErrorResult($"Required parameter '{paramName}' is missing");
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
                        // 使用类型默认值
                        paramValues[i] = param.ParameterType.IsValueType
                            ? Activator.CreateInstance(param.ParameterType)
                            : null;
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
