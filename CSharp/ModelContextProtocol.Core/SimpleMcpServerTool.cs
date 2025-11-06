using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// 简化的MCP工具基类，不依赖Microsoft.Extensions.AI
    /// 适用于 .NET Standard 2.0 + Newtonsoft.Json
    /// </summary>
    public abstract class SimpleMcpServerTool : IMcpServerPrimitive
    {
        public abstract Tool ProtocolTool { get; }
        public abstract IReadOnlyList<object> Metadata { get; }

        public static SimpleMcpServerTool Create(
            string name,
            string description,
            Func<JToken, CancellationToken, Task<CallToolResult>> handler)
        {
            return new DelegateTool(name, description, handler);
        }

        public static SimpleMcpServerTool Create<TTarget>(
            string name,
            string description,
            TTarget target,
            Func<TTarget, JToken, CancellationToken, Task<CallToolResult>> method)
        {
            return new MethodTool<TTarget>(name, description, target, method);
        }

        public abstract Task<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken);

        string IMcpServerPrimitive.Id => ProtocolTool.Name;


        private class DelegateTool : SimpleMcpServerTool
        {
            private readonly Tool _tool;
            private readonly Func<JToken, CancellationToken, Task<CallToolResult>> _handler;

            public DelegateTool(string name, string description, Func<JToken, CancellationToken, Task<CallToolResult>> handler)
            {
                _tool = new Tool
                {
                    Name = name,
                    Description = description
                };
                _handler = handler;
            }

            public override Tool ProtocolTool => _tool;
            public override IReadOnlyList<object> Metadata { get; } = Array.Empty<object>();

            public override async Task<CallToolResult> InvokeAsync(
                RequestContext<CallToolRequestParams> request,
                CancellationToken cancellationToken)
            {
                // Arguments 允许为对象或数组
                var args = request.Params?.Arguments ?? new JObject();

                // 调用实际的处理方法
                return await _handler(args, cancellationToken);
            }
        }

        /// <summary>
        /// 方法包装实现
        /// </summary>
        private class MethodTool<TTarget> : SimpleMcpServerTool
        {
            private readonly Tool _tool;
            private readonly TTarget _target;
            private readonly Func<TTarget, JToken, CancellationToken, Task<CallToolResult>> _method;

            public MethodTool(string name, string description, TTarget target, Func<TTarget, JToken, CancellationToken, Task<CallToolResult>> method)
            {
                _tool = new Tool
                {
                    Name = name,
                    Description = description
                };
                _target = target;
                _method = method;
            }

            public override Tool ProtocolTool => _tool;
            public override IReadOnlyList<object> Metadata { get; } = Array.Empty<object>();

            public override async Task<CallToolResult> InvokeAsync(
                RequestContext<CallToolRequestParams> request,
                CancellationToken cancellationToken)
            {
                // Arguments 允许为对象或数组
                var args = request.Params?.Arguments ?? new JObject();

                // 调用实际的方法
                return await _method(_target, args, cancellationToken);
            }
        }
    }
}

// ============================================================
// 使用示例
// ============================================================

namespace ModelContextProtocol.Examples
{
    using ModelContextProtocol.Server;

    /// <summary>
    /// 示例：如何创建和使用简单工具
    /// </summary>
    public class ExampleUsage
    {
        // 示例1: 使用委托创建简单工具
        public static SimpleMcpServerTool CreateEchoTool()
        {
            return SimpleMcpServerTool.Create(
                name: "echo",
                description: "回显输入的消息",
                handler: async (args, ct) =>
                {
                    string message = "Hello World";

                    if (args is JObject o)
                    {
                        message = o["message"]?.ToString() ?? message;
                    }
                    else if (args is JArray a && a.Count > 0)
                    {
                        message = a[0]?.ToString() ?? message;
                    }

                    string result = $"Echo: {message}";

                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = result }
                        }
                    };
                }
            );
        }

        // 示例2: 使用类型安全的方法
        public class WeatherService
        {
            public async Task<CallToolResult> GetWeather(JToken args, CancellationToken ct)
            {
                string city = null;
                if (args is JObject o)
                {
                    city = o["city"]?.ToString();
                }
                else if (args is JArray a && a.Count > 0)
                {
                    city = a[0]?.ToString();
                }

                if (string.IsNullOrEmpty(city))
                {
                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = "错误：需要提供城市名称" }
                        },
                        IsError = true
                    };
                }

                // 模拟天气数据
                var weather = $"今天{city}的天气是晴天，25度";

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = weather }
                    }
                };
            }
        }

        public static SimpleMcpServerTool CreateWeatherTool(WeatherService service)
        {
            return SimpleMcpServerTool.Create(
                name: "get_weather",
                description: "获取指定城市的天气信息",
                target: service,
                method: (s, args, ct) => s.GetWeather(args, ct)
            );
        }

        // 示例3: 更复杂的工具
        public static SimpleMcpServerTool CreateCalculatorTool()
        {
            return SimpleMcpServerTool.Create(
                name: "calculate",
                description: "执行数学计算",
                handler: async (args, ct) =>
                {
                    double aVal = 0;
                    double bVal = 0;
                    string operation = null;

                    if (args is JObject o)
                    {
                        aVal = o["a"]?.ToObject<double>() ?? 0;
                        bVal = o["b"]?.ToObject<double>() ?? 0;
                        operation = o["operation"]?.ToString();
                    }
                    else if (args is JArray arr)
                    {
                        if (arr.Count > 0) aVal = arr[0]?.ToObject<double>() ?? 0;
                        if (arr.Count > 1) bVal = arr[1]?.ToObject<double>() ?? 0;
                        if (arr.Count > 2) operation = arr[2]?.ToString();
                    }

                    double result = 0;
                    switch (operation?.ToLower())
                    {
                        case "add":
                            result = aVal + bVal;
                            break;
                        case "subtract":
                            result = aVal - bVal;
                            break;
                        case "multiply":
                            result = aVal * bVal;
                            break;
                        case "divide":
                            result = bVal != 0 ? aVal / bVal : 0;
                            break;
                        default:
                            return new CallToolResult
                            {
                                Content = new List<ContentBlock>
                                {
                                    new TextContentBlock { Text = "错误：不支持的操作" }
                                },
                                IsError = true
                            };
                    }

                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"结果: {result}" }
                        }
                    };
                }
            );
        }
    }
}

// ============================================================
// 集成到McpServer的示例
// ============================================================

namespace ModelContextProtocol.Examples
{
    /// <summary>
    /// 如何将简化工具集成到McpServer
    /// </summary>
    public class ServerSetupExample
    {
        public static void SetupServer()
        {
            // 1. 创建工具集合
            var tools = new List<SimpleMcpServerTool>
            {
                ExampleUsage.CreateEchoTool(),
                ExampleUsage.CreateCalculatorTool()
            };

            // 2. 如果需要，可以转换类型
            // 注意：您可能需要让 SimpleMcpServerTool 也实现 IMcpServerPrimitive
            // 或者创建一个适配器

            // 3. 创建服务器选项（假设您已经实现了 McpServerOptions）
            // var options = new McpServerOptions
            // {
            //     ToolCollection = tools
            // };
        }
    }
}

