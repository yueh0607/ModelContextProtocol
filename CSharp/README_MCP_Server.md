# MCP Server HTTP 控制台调试程序

> 基于 Model Context Protocol (MCP) 的完整 HTTP Server 实现，用于调试和测试 MCP 协议。

## 🚀 快速开始（30秒）

### Windows 用户

1. **启动服务器** - 双击 `启动服务器.bat`
2. **运行测试** - 双击 `测试客户端.bat`

### 命令行用户

```bash
# 终端 1: 启动服务器
cd McpServerConsole
dotnet run

# 终端 2: 运行测试
cd McpTestClient
dotnet run
```

## 📁 项目结构

```
CSharp/
├── ModelContextProtocol.Core/      # MCP 核心库
│   └── Server/Transport/
│       └── HttpTransport.cs        # HTTP 传输实现 ✨
├── McpServerConsole/               # HTTP 控制台服务器 ✨
├── McpTestClient/                  # 测试客户端 ✨
├── 启动服务器.bat                  # Windows 启动脚本
├── 测试客户端.bat                  # Windows 测试脚本
└── 使用指南.md                     # 完整文档
```

## ✨ 主要功能

- ✅ **完整的 MCP 协议支持** - JSON-RPC 2.0 + MCP 2024-11-05
- ✅ **HTTP 传输层** - 基于 HTTP POST 的消息传输
- ✅ **4 个内置工具** - echo, calculator, get_system_info, delay
- ✅ **测试客户端** - 自动化测试 + 交互式测试
- ✅ **详细日志** - 完整的请求/响应日志
- ✅ **生产就绪** - 线程安全、错误处理、优雅关闭

## 🧪 快速测试

### 使用 PowerShell

```powershell
# 测试服务器
Invoke-WebRequest -Uri "http://localhost:8767" -Method GET

# 获取工具列表
$body = @{ jsonrpc = "2.0"; id = 1; method = "tools/list" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8767" -Method POST -Body $body -ContentType "application/json"

# 调用 echo 工具
$body = @{
    jsonrpc = "2.0"
    id = 2
    method = "tools/call"
    params = @{
        name = "echo"
        arguments = @{ message = "Hello MCP!" }
    }
} | ConvertTo-Json -Depth 10
Invoke-RestMethod -Uri "http://localhost:8767" -Method POST -Body $body -ContentType "application/json"
```

### 使用 curl

```bash
# 获取工具列表
curl -X POST http://localhost:8767 \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# 调用工具
curl -X POST http://localhost:8767 \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"echo","arguments":{"message":"Hello"}}}'
```

## 🛠️ 内置工具

| 工具 | 说明 | 参数 |
|------|------|------|
| **echo** | 回显消息 | message (string) |
| **calculator** | 数学计算 | a (number), b (number), operation (string) |
| **get_system_info** | 系统信息 | 无 |
| **delay** | 异步延迟 | milliseconds (number) |

## 📖 文档

- 📘 [使用指南.md](使用指南.md) - 完整的使用文档
- 📗 [运行说明.txt](运行说明.txt) - 快速运行指南
- 📕 [项目完成总结.md](项目完成总结.md) - 项目详情
- 📙 [McpServerConsole/README.md](McpServerConsole/README.md) - 服务器文档

## 🎯 使用场景

1. **MCP 协议调试** - 测试 MCP 客户端实现
2. **工具开发测试** - 快速验证工具功能
3. **集成测试** - 自动化测试脚本
4. **演示和教学** - MCP 协议示例

## 🔧 命令行选项

### MCP Server

```bash
dotnet run -- [选项]

选项:
  --port <端口>        指定 HTTP 端口（默认: 8767）
  --transport <类型>   传输类型：http 或 stdio（默认: http）
```

### 测试客户端

```bash
dotnet run -- [选项]

选项:
  --url <地址>         服务器地址（默认: http://localhost:8767）
  --interactive, -i    交互式模式
```

## 💻 技术栈

- **语言**: C# / .NET 8.0
- **协议**: JSON-RPC 2.0, MCP 2024-11-05
- **JSON**: Newtonsoft.Json 13.0.2
- **网络**: System.Net.Sockets (HTTP)

## 📊 架构

```
┌─────────────┐         HTTP POST          ┌─────────────┐
│   Client    │ ──────────────────────────> │  Transport  │
│             │ <────────────────────────── │   (HTTP)    │
└─────────────┘       JSON-RPC 2.0          └─────────────┘
                                                    │
                                                    ▼
                                            ┌─────────────┐
                                            │  MCP Server │
                                            │   + Tools   │
                                            └─────────────┘
```

## 🚨 故障排除

### 问题：端口已被占用

```powershell
# 停止占用端口的进程
Get-Process -Name "dotnet" | Stop-Process -Force

# 或使用其他端口
dotnet run -- --port 9000
```

### 问题：无法连接到服务器

1. 确认服务器正在运行
2. 检查端口号是否正确
3. 检查防火墙设置

## 📝 添加自定义工具

```csharp
toolCollection.Add(SimpleMcpServerTool.Create(
    name: "my_tool",
    description: "我的自定义工具",
    handler: async (args, ct) =>
    {
        // 获取参数
        string param = args["param"]?.ToString();
        
        // 执行逻辑
        string result = $"Result: {param}";
        
        // 返回结果
        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = result }
            }
        };
    }
));
```

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License

---

## 🎉 开始使用

立即运行：
```bash
# 启动服务器
cd McpServerConsole && dotnet run

# 在另一个终端运行测试
cd McpTestClient && dotnet run
```

或双击：
1. `启动服务器.bat`
2. `测试客户端.bat`

**Enjoy! 🚀**

