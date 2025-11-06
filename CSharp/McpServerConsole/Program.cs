using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Server.Transport;

namespace McpServerConsole
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("     MCP Server HTTP 控制台调试程序");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 解析命令行参数
            int port = 8767;
            string transportType = "http";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--port" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out port);
                }
                else if (args[i] == "--transport" && i + 1 < args.Length)
                {
                    transportType = args[i + 1].ToLower();
                }
            }

            try
            {
                // 创建传输层
                IMcpTransport transport;

                if (transportType == "stdio")
                {
                    Console.WriteLine("[启动] 使用 STDIO 传输");
                    transport = new StdioTransport();
                }
                else
                {
                    Console.WriteLine($"[启动] 使用 HTTP 传输，端口: {port}");
                    transport = new HttpTransport(port, "/", msg => Console.WriteLine($"[HTTP] {msg}"));
                }

                // 创建服务器选项
                var options = new McpServerOptions
                {
                    ServerInfo = new Implementation
                    {
                        Name = "MCP Server Console",
                        Version = "1.0.0"
                    },
                    Capabilities = new ServerCapabilities
                    {
                        Tools = new ToolsCapability { ListChanged = false }
                    }
                };

                // 注册示例工具
                RegisterTools(options.ToolCollection);

                // 创建服务器
                var server = new TransportBasedMcpServer(transport, options);

                Console.WriteLine($"[启动] 已注册 {options.ToolCollection.Count} 个工具");
                Console.WriteLine();
                Console.WriteLine("可用工具:");
                foreach (var tool in options.ToolCollection)
                {
                    Console.WriteLine($"  - {tool.ProtocolTool.Name}: {tool.ProtocolTool.Description}");
                }
                Console.WriteLine();
                Console.WriteLine("[就绪] MCP 服务器已启动，等待连接...");
                Console.WriteLine("按 Ctrl+C 停止服务器");
                Console.WriteLine();

                // 设置取消令牌
                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        cts.Cancel();
                        Console.WriteLine();
                        Console.WriteLine("[关闭] 正在停止服务器...");
                    };

                    // 运行服务器
                    try
                    {
                        await server.RunAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消
                    }
                }

                Console.WriteLine("[关闭] MCP 服务器已停止");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"[错误] 致命错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// 注册示例工具
        /// </summary>
        private static void RegisterTools(IList<SimpleMcpServerTool> toolCollection)
        {
            // 1. Echo 工具 - 回显消息
            toolCollection.Add(SimpleMcpServerTool.Create(
                name: "echo",
                description: "回显传入的消息。参数: message (string)",
                handler: async (args, ct) =>
                {
                    string message = args["message"]?.ToString() ?? "Hello World";
                    Console.WriteLine($"[工具调用] echo: {message}");

                    var result = new CallToolResult();
                    result.Content.Add(new TextContentBlock
                    {
                        Text = $"Echo: {message}"
                    });
                    return result;
                }
            ));

            // 2. 计算器工具 - 基本数学运算
            toolCollection.Add(SimpleMcpServerTool.Create(
                name: "calculator",
                description: "执行数学计算。参数: a (number), b (number), operation (add/subtract/multiply/divide)",
                handler: async (args, ct) =>
                {
                    try
                    {
                        double a = args["a"]?.ToObject<double>() ?? 0;
                        double b = args["b"]?.ToObject<double>() ?? 0;
                        string operation = args["operation"]?.ToString()?.ToLower() ?? "add";

                        Console.WriteLine($"[工具调用] calculator: {a} {operation} {b}");

                        double result;
                        string resultText;

                        switch (operation)
                        {
                            case "add":
                                result = a + b;
                                resultText = $"{a} + {b} = {result}";
                                break;
                            case "subtract":
                                result = a - b;
                                resultText = $"{a} - {b} = {result}";
                                break;
                            case "multiply":
                                result = a * b;
                                resultText = $"{a} × {b} = {result}";
                                break;
                            case "divide":
                                if (b == 0)
                                {
                                    return new CallToolResult
                                    {
                                        Content = new List<ContentBlock>
                                        {
                                            new TextContentBlock { Text = "错误：除数不能为0" }
                                        },
                                        IsError = true
                                    };
                                }
                                result = a / b;
                                resultText = $"{a} ÷ {b} = {result}";
                                break;
                            default:
                                return new CallToolResult
                                {
                                    Content = new List<ContentBlock>
                                    {
                                        new TextContentBlock { Text = $"错误：不支持的操作 '{operation}'" }
                                    },
                                    IsError = true
                                };
                        }

                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = resultText }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = $"错误：{ex.Message}" }
                            },
                            IsError = true
                        };
                    }
                }
            ));

            // 3. 系统信息工具
            toolCollection.Add(SimpleMcpServerTool.Create(
                name: "get_system_info",
                description: "获取系统信息",
                handler: async (args, ct) =>
                {
                    Console.WriteLine($"[工具调用] get_system_info");

                    var info = new
                    {
                        os = Environment.OSVersion.ToString(),
                        runtime = Environment.Version.ToString(),
                        machineName = Environment.MachineName,
                        processorCount = Environment.ProcessorCount,
                        currentDirectory = Environment.CurrentDirectory,
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    var json = ModelContextProtocol.Json.JsonConvert.SerializeObject(info, ModelContextProtocol.Json.Formatting.Indented);

                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = json }
                        }
                    };
                }
            ));

            // 4. 延迟工具 - 测试异步
            toolCollection.Add(SimpleMcpServerTool.Create(
                name: "delay",
                description: "延迟指定毫秒数。参数: milliseconds (number)",
                handler: async (args, ct) =>
                {
                    int ms = args["milliseconds"]?.ToObject<int>() ?? 1000;
                    Console.WriteLine($"[工具调用] delay: {ms}ms");

                    await Task.Delay(ms, ct);

                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"延迟了 {ms} 毫秒" }
                        }
                    };
                }
            ));
        }
    }
}
