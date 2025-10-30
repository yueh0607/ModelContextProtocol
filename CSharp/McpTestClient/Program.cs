using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace McpTestClient
{
    /// <summary>
    /// MCP HTTP 客户端测试工具
    /// 用于测试 MCP Server 的各种功能
    /// </summary>
    class Program
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private int _requestId = 1;

        public Program(string baseUrl = "http://localhost:8767")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 发送 JSON-RPC 请求
        /// </summary>
        private async Task<string> SendJsonRpcRequest(string method, object parameters = null)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = _requestId++,
                method = method,
                @params = parameters
            };

            string requestJson = JsonConvert.SerializeObject(request, Formatting.Indented);
            Console.WriteLine("========================================");
            Console.WriteLine($"发送请求 -> {method}");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(requestJson);
            Console.WriteLine();

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_baseUrl, content);
            
            string responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine("收到响应:");
            Console.WriteLine("----------------------------------------");
            
            try
            {
                var jobj = JObject.Parse(responseJson);
                Console.WriteLine(jobj.ToString(Formatting.Indented));
            }
            catch
            {
                Console.WriteLine(responseJson);
            }
            
            Console.WriteLine("========================================");
            Console.WriteLine();

            return responseJson;
        }

        /// <summary>
        /// 测试初始化
        /// </summary>
        public async Task TestInitialize()
        {
            Console.WriteLine("【测试 1】初始化连接");
            await SendJsonRpcRequest("initialize", new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "Test Client",
                    version = "1.0.0"
                }
            });
        }

        /// <summary>
        /// 测试工具列表
        /// </summary>
        public async Task TestListTools()
        {
            Console.WriteLine("【测试 2】获取工具列表");
            await SendJsonRpcRequest("tools/list");
        }

        /// <summary>
        /// 测试 Echo 工具
        /// </summary>
        public async Task TestEchoTool()
        {
            Console.WriteLine("【测试 3】调用 Echo 工具");
            await SendJsonRpcRequest("tools/call", new
            {
                name = "echo",
                arguments = new
                {
                    message = "Hello from test client!"
                }
            });
        }

        /// <summary>
        /// 测试计算器工具
        /// </summary>
        public async Task TestCalculatorTool()
        {
            Console.WriteLine("【测试 4】调用计算器工具 - 加法");
            await SendJsonRpcRequest("tools/call", new
            {
                name = "calculator",
                arguments = new
                {
                    a = 10,
                    b = 5,
                    operation = "add"
                }
            });

            Console.WriteLine("【测试 5】调用计算器工具 - 乘法");
            await SendJsonRpcRequest("tools/call", new
            {
                name = "calculator",
                arguments = new
                {
                    a = 7,
                    b = 8,
                    operation = "multiply"
                }
            });
        }

        /// <summary>
        /// 测试系统信息工具
        /// </summary>
        public async Task TestSystemInfoTool()
        {
            Console.WriteLine("【测试 6】调用系统信息工具");
            await SendJsonRpcRequest("tools/call", new
            {
                name = "get_system_info",
                arguments = new { }
            });
        }

        /// <summary>
        /// 测试延迟工具
        /// </summary>
        public async Task TestDelayTool()
        {
            Console.WriteLine("【测试 7】调用延迟工具");
            await SendJsonRpcRequest("tools/call", new
            {
                name = "delay",
                arguments = new
                {
                    milliseconds = 2000
                }
            });
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task RunAllTests()
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   MCP Server HTTP 客户端测试工具      ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                await TestInitialize();
                await Task.Delay(500);

                await TestListTools();
                await Task.Delay(500);

                await TestEchoTool();
                await Task.Delay(500);

                await TestCalculatorTool();
                await Task.Delay(500);

                await TestSystemInfoTool();
                await Task.Delay(500);

                await TestDelayTool();

                Console.WriteLine();
                Console.WriteLine("✓ 所有测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"✗ 测试失败: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 交互式测试模式
        /// </summary>
        public async Task RunInteractiveMode()
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   MCP Server HTTP 交互式测试工具      ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("可用命令:");
            Console.WriteLine("  init         - 初始化连接");
            Console.WriteLine("  list         - 获取工具列表");
            Console.WriteLine("  echo         - 测试 Echo 工具");
            Console.WriteLine("  calc         - 测试计算器工具");
            Console.WriteLine("  sysinfo      - 测试系统信息工具");
            Console.WriteLine("  delay        - 测试延迟工具");
            Console.WriteLine("  all          - 运行所有测试");
            Console.WriteLine("  quit/exit    - 退出");
            Console.WriteLine();

            while (true)
            {
                Console.Write("> ");
                string command = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrEmpty(command))
                    continue;

                try
                {
                    switch (command)
                    {
                        case "quit":
                        case "exit":
                            return;
                        case "init":
                            await TestInitialize();
                            break;
                        case "list":
                            await TestListTools();
                            break;
                        case "echo":
                            await TestEchoTool();
                            break;
                        case "calc":
                            await TestCalculatorTool();
                            break;
                        case "sysinfo":
                            await TestSystemInfoTool();
                            break;
                        case "delay":
                            await TestDelayTool();
                            break;
                        case "all":
                            await RunAllTests();
                            return;
                        default:
                            Console.WriteLine($"未知命令: {command}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误: {ex.Message}");
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// 主入口 - 运行测试客户端
        /// </summary>
        static async Task Main(string[] args)
        {
            string baseUrl = "http://localhost:8767";
            bool interactive = false;

            // 解析命令行参数
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--url" && i + 1 < args.Length)
                {
                    baseUrl = args[i + 1];
                }
                else if (args[i] == "--interactive" || args[i] == "-i")
                {
                    interactive = true;
                }
            }

            var client = new Program(baseUrl);

            if (interactive)
            {
                await client.RunInteractiveMode();
            }
            else
            {
                await client.RunAllTests();
            }
        }
    }
}

