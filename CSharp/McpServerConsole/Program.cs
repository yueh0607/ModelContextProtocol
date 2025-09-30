using System;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Transport;
using McpServerLib.Mcp;
using McpServerLib.Mcp.Tools;
using McpServerLib.Utils;

namespace McpServerConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Error.WriteLine("正在启动 MCP 服务器...");
            McpLogger.Info("MCP 服务器控制台应用程序启动");

            try
            {
                // 创建传输层
                var options = new StreamableHttpTransportOptions();
                McpLogger.Debug("使用的传输选项: ServerUrl={0}", options.ServerUrl);

                using (IJsonRpcTransport transport = TransportFactory.CreateTransport(TransportType.StreamableHttp, options))
                {
                    McpLogger.Debug("传输层已创建");

                    // 创建 MCP 服务器
                    McpServer server = new McpServer(transport);
                    McpLogger.Debug("开始注册 BasicTools 工具类");
                    server.RegisterToolClass<BasicTools>();
                    McpLogger.Debug("MCP 服务器已创建并注册工具类");

                    // 测试工具注册
                    var toolCount = server.GetToolCount();
                    McpLogger.Debug("工具注册测试完成，工具总数: {0}", toolCount);

                    // 设置服务器信息
                    server.SetServerInfo("mcp-server-csharp", "1.0.0", "C# MCP Server Console");
                    McpLogger.Debug("服务器信息已设置");

                    // 处理错误事件
                    server.ErrorOccurred += (sender, ex) =>
                    {
                        Console.Error.WriteLine($"服务器错误: {ex.Message}");
                        Console.Error.WriteLine(ex.StackTrace);
                        McpLogger.Error("MCP 服务器错误", ex);
                    };

                    Console.Error.WriteLine("MCP 服务器已启动，正在监听连接...");
                    Console.Error.WriteLine("使用 Ctrl+C 停止服务器");
                    McpLogger.Info("MCP 服务器配置完成，开始监听");

                    // 设置取消令牌
                    using (var cts = new CancellationTokenSource())
                    {
                        Console.CancelKeyPress += (sender, e) =>
                        {
                            e.Cancel = true;
                            cts.Cancel();
                            Console.Error.WriteLine("正在关闭服务器...");
                            McpLogger.Info("收到关闭信号，正在停止服务器");
                        };

                        // 等待取消信号（传输层已在JsonRpcServer构造函数中启动）
                        McpLogger.Debug("等待取消信号...");
                        try
                        {
                            await Task.Delay(Timeout.Infinite, cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            McpLogger.Debug("收到取消信号");
                        }
                    }
                }

                Console.Error.WriteLine("MCP 服务器已停止");
                McpLogger.Info("MCP 服务器已完全停止");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"致命错误: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
