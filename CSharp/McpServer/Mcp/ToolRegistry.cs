using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using McpServerLib.Mcp.Attributes;
using McpServerLib.Mcp.Models;
using McpServerLib.Utils;

namespace McpServerLib.Mcp
{
    public class ToolRegistry
    {
        private readonly Dictionary<string, ToolMethodInfo> _tools = new Dictionary<string, ToolMethodInfo>();
        private readonly List<object> _toolInstances = new List<object>();

        public void RegisterToolClass<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            McpLogger.Debug("ToolRegistry: 注册工具类实例 {0}", typeof(T).Name);
            _toolInstances.Add(instance);
            DiscoverTools(instance);
            McpLogger.Debug("ToolRegistry: 工具发现完成，当前总工具数: {0}", _tools.Count);
        }

        public void RegisterToolClass(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var instance = Activator.CreateInstance(type);
            RegisterToolClass(instance);
        }

        public void RegisterToolClass<T>() where T : class, new()
        {
            var instance = new T();
            RegisterToolClass(instance);
        }

        private void DiscoverTools(object instance)
        {
            var type = instance.GetType();
            McpLogger.Debug("ToolRegistry: 开始发现工具，类型: {0}", type.Name);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var toolAttribute = method.GetCustomAttribute<McpToolAttribute>();
                if (toolAttribute == null)
                    continue;

                McpLogger.Debug("ToolRegistry: 发现工具方法 {0}.{1} -> 工具名称: {2}", type.Name, method.Name, toolAttribute.Name);

                var toolInfo = new ToolMethodInfo
                {
                    Instance = instance,
                    Method = method,
                    ToolAttribute = toolAttribute,
                    Tool = CreateToolFromMethod(method, toolAttribute)
                };

                _tools[toolAttribute.Name] = toolInfo;
                McpLogger.Debug("ToolRegistry: 工具已注册: {0}", toolAttribute.Name);
            }
        }

        private Tool CreateToolFromMethod(MethodInfo method, McpToolAttribute toolAttribute)
        {
            var tool = new Tool
            {
                Name = toolAttribute.Name,
                Description = toolAttribute.Description,
                InputSchema = CreateInputSchemaFromMethod(method)
            };

            return tool;
        }

        private ToolInputSchema CreateInputSchemaFromMethod(MethodInfo method)
        {
            var schema = new ToolInputSchema();
            var parameters = method.GetParameters();

            // Skip CancellationToken parameters
            var relevantParams = parameters.Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();

            foreach (var param in relevantParams)
            {
                var paramAttr = param.GetCustomAttribute<McpParameterAttribute>();
                var paramName = param.Name; // Always use actual parameter name
                var paramType = GetJsonType(param.ParameterType);

                var paramSchema = new Dictionary<string, object>
                {
                    ["type"] = paramType
                };

                if (paramAttr != null && !string.IsNullOrEmpty(paramAttr.Description))
                {
                    paramSchema["description"] = paramAttr.Description;
                }

                schema.Properties[paramName] = paramSchema;

                // Add to required if parameter is required
                if (paramAttr?.Required != false && !param.HasDefaultValue)
                {
                    schema.Required.Add(paramName);
                }
            }

            return schema;
        }

        private string GetJsonType(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                return "integer";
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
                return "array";

            return "object";
        }

        public List<Tool> GetAllTools()
        {
            var tools = _tools.Values.Select(t => t.Tool).ToList();
            McpLogger.Debug("GetAllTools 返回 {0} 个工具: {1}", tools.Count, string.Join(", ", tools.Select(t => t.Name)));
            return tools;
        }

        public async Task<CallToolResponse> ExecuteToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
        {
            return await ExecuteToolInternal(toolName, arguments, cancellationToken, forceSync: false);
        }

        // Unity-compatible synchronous execution method
        public CallToolResponse ExecuteToolSync(string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
        {
            return ExecuteToolInternal(toolName, arguments, cancellationToken, forceSync: true).GetAwaiter().GetResult();
        }

        private async Task<CallToolResponse> ExecuteToolInternal(string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken, bool forceSync)
        {
            if (!_tools.TryGetValue(toolName, out var toolInfo))
            {
                return new CallToolResponse
                {
                    Content = new List<Content>
                    {
                        new Content { Type = "text", Text = $"Tool '{toolName}' not found" }
                    },
                    IsError = true
                };
            }

            try
            {
                var parameters = PrepareMethodParameters(toolInfo.Method, arguments, cancellationToken);
                var result = toolInfo.Method.Invoke(toolInfo.Instance, parameters);

                // Handle async methods
                if (result is Task task)
                {
                    if (forceSync)
                    {
                        // For Unity compatibility, execute synchronously
                        return ExecuteTaskSync(task);
                    }
                    else
                    {
                        // Normal async execution
                        return await ExecuteTaskAsync(task);
                    }
                }

                // Handle synchronous methods
                return ConvertResultToResponse(result);
            }
            catch (Exception ex)
            {
                return new CallToolResponse
                {
                    Content = new List<Content>
                    {
                        new Content { Type = "text", Text = $"Error executing tool: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }

        private object[] PrepareMethodParameters(MethodInfo method, Dictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            var parameters = method.GetParameters();
            var paramValues = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (param.ParameterType == typeof(CancellationToken))
                {
                    paramValues[i] = cancellationToken;
                    continue;
                }

                var paramAttr = param.GetCustomAttribute<McpParameterAttribute>();
                var paramName = param.Name; // Always use actual parameter name

                if (arguments != null && arguments.TryGetValue(paramName, out var value))
                {
                    paramValues[i] = ConvertParameter(value, param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    paramValues[i] = param.DefaultValue;
                }
                else
                {
                    paramValues[i] = GetDefaultValue(param.ParameterType);
                }
            }

            return paramValues;
        }

        private object ConvertParameter(object value, Type targetType)
        {
            if (value == null || targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        private async Task<CallToolResponse> ExecuteTaskAsync(Task task)
        {
            await task;

            // Get result from Task<T>
            if (task.GetType().IsGenericType)
            {
                var property = task.GetType().GetProperty("Result");
                var result = property?.GetValue(task);
                return ConvertResultToResponse(result);
            }
            else
            {
                // Task without result
                return ConvertResultToResponse(null);
            }
        }

        private CallToolResponse ExecuteTaskSync(Task task)
        {
            // For Unity compatibility - execute task synchronously
            task.GetAwaiter().GetResult();

            // Get result from Task<T>
            if (task.GetType().IsGenericType)
            {
                var property = task.GetType().GetProperty("Result");
                var result = property?.GetValue(task);
                return ConvertResultToResponse(result);
            }
            else
            {
                // Task without result
                return ConvertResultToResponse(null);
            }
        }

        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private CallToolResponse ConvertResultToResponse(object result)
        {
            if (result == null)
            {
                return new CallToolResponse
                {
                    Content = new List<Content>
                    {
                        new Content { Type = "text", Text = "Tool executed successfully" }
                    }
                };
            }

            if (result is CallToolResponse response)
            {
                return response;
            }

            if (result is string text)
            {
                return new CallToolResponse
                {
                    Content = new List<Content>
                    {
                        new Content { Type = "text", Text = text }
                    }
                };
            }

            return new CallToolResponse
            {
                Content = new List<Content>
                {
                    new Content { Type = "text", Text = result.ToString() }
                }
            };
        }
    }

    internal class ToolMethodInfo
    {
        public object Instance { get; set; }
        public MethodInfo Method { get; set; }
        public McpToolAttribute ToolAttribute { get; set; }
        public Tool Tool { get; set; }
    }
}