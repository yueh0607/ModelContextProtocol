# Unity MCP 服务器实现计划

## 一、对比分析

### 现有实现（McpServer 文件夹）
- ✅ 完整的工作实现
- ✅ 使用自定义的 JSON-RPC 实现
- ✅ 包含工具注册和调用机制
- ✅ 支持多种传输方式

### 目标实现（ModelContextProtocol.Core）
- 需要适配 Unity（.NET Standard 2.0）
- 需要与原始 SDK 的接口兼容
- 需要更简洁的实现
- 使用 Newtonsoft.Json

---

## 二、核心设计决策

### 1. 为什么保留 McpServer 为抽象类？

```csharp
public abstract class McpServer
{
    public abstract Task RunAsync(CancellationToken cancellationToken = default);
}
```

**原因**：
- 不同传输方式（WebSocket/stdio/HTTP）需要不同的实现
- 抽象类提供统一接口，具体实现由传输层决定
- 便于测试和扩展

### 2. 我们的实现策略

**目标**：创建一个具体的 McpServer 实现，适配 Unity

```
Core/Server/McpServer.cs (抽象基类)
    ↓
Core/Server/TransportBasedMcpServer.cs (具体实现)
    ↓
可能需要不同的传输层实现：
- StdioMcpServer (stdio 传输，适合 Unity)
- WebSocketMcpServer (WebSocket 传输)
- HttpMcpServer (HTTP 传输)
```

---

## 三、实现步骤

### 步骤 1：创建传输层接口

我们需要定义抽象的传输层接口，让 McpServer 不依赖具体传输：

```csharp
// IMcpTransport.cs
public interface IMcpTransport
{
    Task<string> ReadMessageAsync(CancellationToken cancellationToken);
    Task WriteMessageAsync(string message, CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    void Stop();
}
```

### 步骤 2：实现具体的传输层

```csharp
// StdioTransport.cs - 用于 Unity CLI/编辑器
public class StdioTransport : IMcpTransport
{
    public async Task<string> ReadMessageAsync(CancellationToken ct)
    {
        // 从 stdin 读取
    }
    
    public async Task WriteMessageAsync(string message, CancellationToken ct)
    {
        // 写入 stdout
    }
}

// UnityWebSocketTransport.cs - 用于 Unity 编辑器/运行时
public class UnityWebSocketTransport : IMcpTransport
{
    // 使用 Unity 的 WebSocket 实现
}
```

### 步骤 3：实现 McpServer

```csharp
// TransportBasedMcpServer.cs
public class TransportBasedMcpServer : McpServer
{
    private readonly IMcpTransport _transport;
    private readonly IList<McpServerTool> _tools;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    
    public override async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cts.Token))
        {
            await _transport.StartAsync(cts.Token);
            
            while (!cts.Token.IsCancellationRequested)
            {
                var message = await _transport.ReadMessageAsync(cts.Token);
                _ = Task.Run(() => ProcessMessageAsync(message, cts.Token));
            }
        }
    }
    
    private async Task ProcessMessageAsync(string messageJson, CancellationToken ct)
    {
        var message = JsonConvert.DeserializeObject<JsonRpcMessage>(messageJson);
        
        // 路由到对应的处理器
        if (message is JsonRpcRequest request)
        {
            await HandleRequestAsync(request, ct);
        }
        else if (message is JsonRpcNotification notification)
        {
            await HandleNotificationAsync(notification, ct);
        }
    }
}
```

---

## 四、关键实现细节

### 1. 消息路由机制

```csharp
private async Task HandleRequestAsync(JsonRpcRequest request, CancellationToken ct)
{
    switch (request.Method)
    {
        case "initialize":
            await HandleInitializeAsync(request, ct);
            break;
        case "tools/list":
            await HandleToolsListAsync(request, ct);
            break;
        case "tools/call":
            await HandleToolCallAsync(request, ct);
            break;
        default:
            await SendErrorAsync(request.Id, -32601, "Method not found");
            break;
    }
}
```

### 2. 工具注册与查找

```csharp
public void AddTool(McpServerTool tool)
{
    _tools.Add(tool);
}

private McpServerTool FindTool(string name)
{
    return _tools.FirstOrDefault(t => t.ProtocolTool.Name == name);
}
```

### 3. 参数绑定

核心代码在 `AIFunctionMcpServerTool.cs` 中：

```csharp
// 第 270-316 行展示了参数绑定逻辑
// 关键点：
// 1. 检查特殊类型（CancellationToken, IServiceProvider, JObject）
// 2. 从 JObject args 中提取参数值
// 3. 使用反射调用方法
// 4. 处理异步返回值
// 5. 转换返回值为 CallToolResult
```

---

## 五、Unity 特定适配

### 1. 异步处理

Unity 的 .NET Standard 2.0 完全支持 async/await，所以我们可以直接使用：

```csharp
public async override Task RunAsync(CancellationToken cancellationToken)
{
    // Unity 完全支持
}
```

### 2. CancellationToken 支持

Unity 2019+ 都支持 CancellationToken：

```csharp
using System.Threading;

public async Task ProcessAsync(CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();
    // ...
}
```

### 3. JSON 序列化

使用 Newtonsoft.Json（Unity 2021.2+ 内置支持）：

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// 序列化
var json = JsonConvert.SerializeObject(obj);

// 反序列化
var obj = JsonConvert.DeserializeObject<T>(json);

// 动态读取
JObject jobj = JObject.Parse(json);
string value = jobj["key"].ToString();
```

---

## 六、性能和内存考虑

### 1. ValueTask vs Task

```csharp
// 使用 ValueTask 减少分配
public ValueTask<CallToolResult> InvokeAsync(...)
{
    // 如果同步完成，避免 Task 分配
}
```

### 2. 对象池（可选）

对于高频调用的对象，可以考虑对象池：

```csharp
// Unity 有内置的 ObjectPool
using UnityEngine.Pool;

var pool = new ObjectPool<JObject>(
    createFunc: () => new JObject(),
    actionOnRelease: obj => { obj.Clear(); }
);
```

---

## 七、测试策略

### 1. 单元测试

测试工具注册、调用、参数绑定：

```csharp
[Test]
public void TestToolRegistration()
{
    var server = new TransportBasedMcpServer(mockTransport);
    var tool = McpServerTool.Create((string msg) => msg);
    server.AddTool(tool);
    
    Assert.AreEqual(1, server.GetToolCount());
}
```

### 2. 集成测试

测试完整的 JSON-RPC 流程：

```csharp
[Test]
public async Task TestToolCallFlow()
{
    // 1. 发送 initialize
    // 2. 发送 tools/call
    // 3. 验证响应
}
```

---

## 八、实现优先级

1. ✅ **已完成**：协议定义（Protocol层）
2. ✅ **已完成**：AIFunctionMcpServerTool 实现
3. ⏳ **进行中**：McpServer 具体实现
4. ⏳ **待实现**：传输层实现
5. ⏳ **待实现**：初始化握手
6. ⏳ **待实现**：工具调用流程
7. ⏳ **待实现**：资源管理
8. ⏳ **待实现**：示例代码

---

## 九、下一步

让我们开始实现！

