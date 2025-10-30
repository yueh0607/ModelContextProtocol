# MCP Server Console - HTTP 调试程序

基于 Model Context Protocol (MCP) 实现的 HTTP 通信控制台调试程序。

## 功能特性

- ✅ 支持 HTTP 和 STDIO 两种传输方式
- ✅ 完整的 MCP 协议支持（初始化、工具列表、工具调用）
- ✅ 内置多个示例工具用于测试
- ✅ 详细的日志输出用于调试
- ✅ 独立的测试客户端

## 项目结构

```
McpServerConsole/
├── Program.cs          # MCP Server 主程序
├── TestClient.cs       # HTTP 测试客户端
├── README.md           # 本文档
└── McpServerConsole.csproj
```

## 快速开始

### 1. 启动 MCP Server

**使用 HTTP 传输（默认）：**

```powershell
cd McpServerConsole
dotnet run
```

服务器将在 `http://localhost:8767` 上监听。

**指定端口：**

```powershell
dotnet run -- --port 9000
```

**使用 STDIO 传输：**

```powershell
dotnet run -- --transport stdio
```

### 2. 使用测试客户端

在另一个 PowerShell 窗口中运行测试客户端：

**运行所有自动测试：**

```powershell
cd ..\McpTestClient
dotnet run
```

**交互式模式：**

```powershell
cd ..\McpTestClient
dotnet run -- --interactive
```

**使用 PowerShell 脚本快速测试：**

```powershell
# 从项目根目录
.\simple-test.ps1
```

## 内置工具

服务器提供以下测试工具：

### 1. echo
回显传入的消息

**参数:**
- `message` (string): 要回显的消息

**示例:**
```json
{
  "name": "echo",
  "arguments": {
    "message": "Hello World"
  }
}
```

### 2. calculator
执行基本数学运算

**参数:**
- `a` (number): 第一个数字
- `b` (number): 第二个数字
- `operation` (string): 运算类型 (add/subtract/multiply/divide)

**示例:**
```json
{
  "name": "calculator",
  "arguments": {
    "a": 10,
    "b": 5,
    "operation": "add"
  }
}
```

### 3. get_system_info
获取系统信息

**参数:** 无

**返回:** 系统的详细信息（操作系统、运行时版本等）

### 4. delay
测试异步操作的延迟工具

**参数:**
- `milliseconds` (number): 延迟的毫秒数

## 手动测试

你也可以使用 `curl` 或其他 HTTP 客户端手动测试：

### 初始化连接

```bash
curl -X POST http://localhost:8767 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {
        "name": "curl",
        "version": "1.0.0"
      }
    }
  }'
```

### 获取工具列表

```bash
curl -X POST http://localhost:8767 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list"
  }'
```

### 调用工具

```bash
curl -X POST http://localhost:8767 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "echo",
      "arguments": {
        "message": "Hello from curl!"
      }
    }
  }'
```

## 开发和调试

### 添加新工具

在 `Program.cs` 的 `RegisterTools` 方法中添加新工具：

```csharp
toolCollection.Add(SimpleMcpServerTool.Create(
    name: "my_tool",
    description: "我的自定义工具",
    handler: async (args, ct) =>
    {
        // 实现你的工具逻辑
        string param = args["param"]?.ToString();
        
        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = $"结果: {param}" }
            }
        };
    }
));
```

### 日志输出

服务器会在控制台输出详细的日志信息：

- `[启动]` - 启动相关信息
- `[HTTP]` - HTTP 传输层日志
- `[工具调用]` - 工具调用日志
- `[错误]` - 错误信息
- `[关闭]` - 关闭相关信息

## 常见问题

### 端口已被占用

如果看到 "Address already in use" 错误，请使用 `--port` 参数指定其他端口：

```bash
dotnet run -- --port 9000
```

### 测试客户端连接失败

确保：
1. MCP Server 正在运行
2. 端口号匹配（默认 8767）
3. 防火墙没有阻止连接

### GET 请求返回信息

访问 `http://localhost:8767` 会返回服务器信息：

```
MCP Server HTTP Transport

Use POST request with JSON-RPC message in body.
```

## 技术架构

- **传输层**: `HttpTransport` - 基于 HTTP POST 的 JSON-RPC 传输
- **服务器**: `TransportBasedMcpServer` - MCP 协议服务器实现
- **工具系统**: `SimpleMcpServerTool` - 简化的工具创建和调用
- **协议**: JSON-RPC 2.0 over HTTP

## 相关文档

- [Model Context Protocol](https://modelcontextprotocol.io/)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)

## 许可证

MIT License

