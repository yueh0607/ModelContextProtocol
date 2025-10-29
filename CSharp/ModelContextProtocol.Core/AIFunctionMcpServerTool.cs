using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Server
{
    /// <summary>
    /// 简化版的 McpServerTool 实现，不依赖 Microsoft.Extensions.AI
    /// 使用委托直接实现工具逻辑
    /// </summary>
    internal sealed class AIFunctionMcpServerTool : McpServerTool
    {
        private readonly Tool _tool;
        private readonly Func<JObject, CancellationToken, Task<CallToolResult>> _handler;
        private readonly object _target; // 用于持有方法的目标对象（如果需要）
        private readonly MethodInfo _methodInfo; // 存储方法信息

        /// <summary>
        /// 从委托创建工具
        /// </summary>
        public static AIFunctionMcpServerTool Create(Delegate method, McpServerToolCreateOptions options = null)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            return Create(method.Method, method.Target, options);
        }

        /// <summary>
        /// 从 MethodInfo 创建工具
        /// </summary>
        public static AIFunctionMcpServerTool Create(MethodInfo method, object target, McpServerToolCreateOptions options = null)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            options = options ?? new McpServerToolCreateOptions();

            // 从属性或参数中获取名称和描述
            var toolAttr = method.GetCustomAttributes(false).OfType<McpServerToolAttribute>().FirstOrDefault();
            string name = options.Name ?? toolAttr?.Name ?? method.Name;
            string description = options.Description ?? method.GetCustomAttributes(false).OfType<System.ComponentModel.DescriptionAttribute>().FirstOrDefault()?.Description;

            // 创建工具对象
            var tool = new Tool
            {
                Name = name,
                Description = description ?? "",
                InputSchema = CreateInputSchema(method, options), // 从方法签名生成Schema
            };

            // 如果指定了图标
            if (options != null && options.Icons != null)
            {
                tool.Icons = new List<Icon>(options.Icons);
            }

            // 如果指定了注释
            if (options != null)
            {
                if (options.Title != null || options.Destructive || options.Idempotent || options.OpenWorld || options.ReadOnly)
                {
                    tool.Annotations = new ToolAnnotations
                    {
                        Title = options.Title,
                        IdempotentHint = options.Idempotent,
                        DestructiveHint = options.Destructive,
                        OpenWorldHint = options.OpenWorld,
                        ReadOnlyHint = options.ReadOnly,
                    };
                }
            }

            // 创建处理器：将方法转换为委托处理器
            Func<JObject, CancellationToken, Task<CallToolResult>> handler = CreateHandler(method, target, options);

            return new AIFunctionMcpServerTool(tool, handler, target, method);
        }

        /// <summary>
        /// 从 MethodInfo + createTargetFunc 创建工具
        /// </summary>
        public static AIFunctionMcpServerTool Create(
            MethodInfo method,
            Func<RequestContext<CallToolRequestParams>, object> createTargetFunc,
            McpServerToolCreateOptions options = null)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (createTargetFunc == null)
                throw new ArgumentNullException(nameof(createTargetFunc));

            options = options ?? new McpServerToolCreateOptions();

            var toolAttr = method.GetCustomAttributes(false).OfType<McpServerToolAttribute>().FirstOrDefault();
            string name = options.Name ?? toolAttr?.Name ?? method.Name;
            var descAttr = method.GetCustomAttributes(false).OfType<System.ComponentModel.DescriptionAttribute>().FirstOrDefault();
            string description = options.Description ?? descAttr?.Description;

            var tool = new Tool
            {
                Name = name,
                Description = description ?? "",
                InputSchema = CreateInputSchema(method, options),
            };

            if (options?.Icons != null)
                tool.Icons = new List<Icon>(options.Icons);

            if (options != null && (options.Title != null || options.Destructive || options.Idempotent || options.OpenWorld || options.ReadOnly))
            {
                tool.Annotations = new ToolAnnotations
                {
                    Title = options.Title,
                    IdempotentHint = options.Idempotent,
                    DestructiveHint = options.Destructive,
                    OpenWorldHint = options.OpenWorld,
                    ReadOnlyHint = options.ReadOnly,
                };
            }

            // 创建特殊的处理器，每次调用时创建新目标
            Func<JObject, CancellationToken, Task<CallToolResult>> handler = async (args, ct) =>
            {
                // 创建一个模拟的 request 来调用 createTargetFunc
                var mockRequest = new RequestContext<CallToolRequestParams>
                {
                    Params = new CallToolRequestParams { Arguments = args.ToObject<Dictionary<string, JToken>>() }
                };

                object target = createTargetFunc(mockRequest);
                return await InvokeMethodAsync(method, target, args, ct, options);
            };

            return new AIFunctionMcpServerTool(tool, handler, null, method);
        }

        /// <summary>
        /// 从 AIFunction 创建（支持旧接口，但在简化版中实际上不使用 AIFunction）
        /// </summary>
        [Obsolete("简化版本不支持 AIFunction 参数，请使用委托版本")]
        public static AIFunctionMcpServerTool Create(object function, McpServerToolCreateOptions options = null)
        {
            // 简化版本：直接抛出异常
            throw new NotSupportedException(
                "简化版本不支持 AIFunction。" + 
                "请使用以下方式创建工具：" + 
                "1. McpServerTool.Create(Delegate method, McpServerToolCreateOptions options)" +
                "2. McpServerTool.Create(MethodInfo method, object target, McpServerToolCreateOptions options)");
        }

        private AIFunctionMcpServerTool(Tool tool, Func<JObject, CancellationToken, Task<CallToolResult>> handler, object target, MethodInfo methodInfo)
        {
            _tool = tool;
            _tool.McpServerTool = this;
            _handler = handler;
            _target = target;
            _methodInfo = methodInfo;

            // 初始化元数据
            Metadata = CreateMetadata(_methodInfo);
        }

        public override Tool ProtocolTool => _tool;
        public override IReadOnlyList<object> Metadata { get; }

        public override async ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // 将请求参数转换为 JObject
            JObject args = new JObject();
            if (request.Params?.Arguments != null)
            {
                foreach (var kvp in request.Params.Arguments)
                {
                    args[kvp.Key] = JToken.FromObject(kvp.Value);
                }
            }

            // 调用处理器
            return await _handler(args, cancellationToken);
        }

        /// <summary>
        /// 从方法签名生成 JSON Schema
        /// </summary>
        private static JToken CreateInputSchema(MethodInfo method, McpServerToolCreateOptions options)
        {
            // 简化的Schema生成：仅包含基本的 object 类型
            // 完整实现需要分析方法参数并生成详细的 Schema
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject()
            };

            var properties = (JObject)schema["properties"];
            var required = new List<string>();

            foreach (var param in method.GetParameters())
            {
                // 跳过特殊参数
                if (typeof(CancellationToken).IsAssignableFrom(param.ParameterType) ||
                    typeof(IServiceProvider).IsAssignableFrom(param.ParameterType) ||
                    typeof(McpServer).IsAssignableFrom(param.ParameterType))
                {
                    continue;
                }

                string paramName = param.Name;
                string paramType = param.ParameterType.Name.ToLowerInvariant();

                // 根据.NET类型推断JSON Schema类型
                string jsonType = paramType switch
                {
                    "string" or "guid" => "string",
                    "int32" or "int" or "int64" or "long" or "decimal" or "double" or "float" => "number",
                    "bool" or "boolean" => "boolean",
                    _ => "string" // 默认
                };

                properties[paramName] = new JObject
                {
                    ["type"] = jsonType,
                    ["description"] = param.GetCustomAttributes(false).OfType<System.ComponentModel.DescriptionAttribute>().FirstOrDefault()?.Description
                };

                if (!param.HasDefaultValue)
                {
                    required.Add(paramName);
                }
            }

            if (required.Count > 0)
            {
                schema["required"] = JArray.FromObject(required);
            }

            return schema;
        }

        /// <summary>
        /// 创建方法处理器
        /// </summary>
        private static Func<JObject, CancellationToken, Task<CallToolResult>> CreateHandler(
            MethodInfo method,
            object target,
            McpServerToolCreateOptions options)
        {
            return async (args, ct) =>
            {
                // 这里应该实现参数绑定逻辑
                // 简化版：返回一个错误提示用户手动实现
                return await InvokeMethodAsync(method, target, args, ct, options);
            };
        }

        /// <summary>
        /// 实际调用方法
        /// </summary>
        private static async Task<CallToolResult> InvokeMethodAsync(
            MethodInfo method,
            object target,
            JObject args,
            CancellationToken ct,
            McpServerToolCreateOptions options)
        {
            try
            {
                // 准备参数数组
                var parameters = method.GetParameters();
                object[] paramValues = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    
                    // 特殊参数处理
                    if (typeof(CancellationToken).IsAssignableFrom(param.ParameterType))
                    {
                        paramValues[i] = ct;
                    }
                    else if (typeof(IServiceProvider).IsAssignableFrom(param.ParameterType))
                    {
                        paramValues[i] = options?.Services;
                    }
                    else if (param.ParameterType == typeof(JObject))
                    {
                        paramValues[i] = args;
                    }
                    else
                    {
                        // 从 args 中获取参数值
                        if (args[param.Name] != null)
                        {
                            paramValues[i] = args[param.Name].ToObject(param.ParameterType);
                        }
                        else if (param.HasDefaultValue)
                        {
                            paramValues[i] = param.DefaultValue;
                        }
                        else
                        {
                            throw new ArgumentException($"缺少必需参数: {param.Name}");
                        }
                    }
                }

                // 调用方法
                object result = method.Invoke(target, paramValues);

                // 处理异步返回值
                if (result is Task taskResult)
                {
                    await taskResult;
                    
                    if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        result = ((dynamic)taskResult).Result;
                    }
                    else
                    {
                        result = null;
                    }
                }

                // 将返回值转换为 CallToolResult
                return ConvertToCallToolResult(result);
            }
            catch (Exception ex)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"工具执行错误: {ex.Message}" }
                    }
                };
            }
        }

        /// <summary>
        /// 将方法返回值转换为 CallToolResult
        /// </summary>
        private static CallToolResult ConvertToCallToolResult(object result)
        {
            if (result == null)
            {
                return new CallToolResult { Content = new List<ContentBlock>() };
            }

            // 如果已经是 CallToolResult
            if (result is CallToolResult ctr)
            {
                return ctr;
            }

            // 如果是字符串
            if (result is string str)
            {
                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = str }
                    }
                };
            }

            // 如果是 ContentBlock 列表
            if (result is IList<ContentBlock> blocks)
            {
                return new CallToolResult { Content = blocks };
            }

            // 默认：序列化为 JSON
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock
                    {
                        Text = Newtonsoft.Json.JsonConvert.SerializeObject(result)
                    }
                }
            };
        }

        /// <summary>
        /// 从方法创建元数据
        /// </summary>
        private static IReadOnlyList<object> CreateMetadata(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return Array.Empty<object>();

            var metadata = new List<object>();

            // 添加类级别的属性
            var classAttrs = methodInfo.DeclaringType?.GetCustomAttributes(false).Cast<object>() ?? Enumerable.Empty<object>();
            metadata.AddRange(classAttrs);

            // 添加方法级别的属性
            var methodAttrs = methodInfo.GetCustomAttributes(false);
            metadata.AddRange(methodAttrs);

            return metadata;
        }
    }
}


