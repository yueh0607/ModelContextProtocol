# Unity MCP Server

一个为Unity量身定制的Model Context Protocol (MCP)服务器实现，使AI助手（如Claude）能够直接与你的Unity项目交互。

## 概述

Unity MCP Server是一个基于HTTP的MCP服务器，专为Unity环境设计：
- ✅ **完全兼容** .NET Standard 2.0 和 C# 7.3
- ✅ **零外部依赖** 仅需Newtonsoft.Json（Unity官方支持）
- ✅ **线程安全** 自动处理Unity主线程调度
- ✅ **协议兼容** 可与任何标准MCP客户端对接（如Claude Desktop）
- ✅ **易于扩展** 简单的抽象基类，轻松创建自定义工具和资源
- ✅ **双模式运行** 支持编辑器模式和运行时模式

## 🎯 两种运行模式

### 编辑器模式（推荐）⭐
- **无需进入Play模式**，直接在Unity编辑器中运行
- 打开 `Window → MCP Server` 查看可视化控制面板
- 服务器独立运行，不受场景切换影响
- 适合日常开发和AI辅助编辑

### 运行时模式
- 在Play模式下运行，作为游戏的一部分
- 使用MonoBehaviour组件：`UnityMcpServer`
- 适合运行时调试和游戏内AI集成

## 为什么不用官方C# SDK？

官方MCP C# SDK使用的技术栈在Unity中无法运行：
- `System.Text.Json` 需要 .NET Standard 2.1（Unity 2019.x只支持2.0）
- `ASP.NET Core` 无法在Unity中运行
- 大量现代.NET依赖不兼容Unity环境

本项目是**协议兼容**的独立实现，使用Newtonsoft.Json（Unity原生支持），可与任何标准MCP客户端无缝对接。

## 系统要求

- **Unity版本**: 2019.x 或更高
- **API兼容级别**: .NET Standard 2.0 或 .NET 4.x
- **依赖**: Newtonsoft.Json for Unity
  - 通过 Unity Package Manager 安装：`com.unity.nuget.newtonsoft-json`

## 安装步骤

### 1. 安装Newtonsoft.Json

在Unity中打开 **Window > Package Manager**:

1. 点击左上角的 **+** 按钮
2. 选择 **Add package by name...**
3. 输入：`com.unity.nuget.newtonsoft-json`
4. 点击 **Add**

### 2. 导入Unity MCP Server

将以下文件复制到你的Unity项目的 `Assets/Scripts/UnityMCP/` 目录：

```
UnityMCP/
├── McpTypes.cs                  # MCP协议类型定义
├── McpServer.cs                 # MCP服务器核心和抽象基类
├── McpHttpServer.cs             # HTTP服务器实现
├── UnityMcpServer.cs            # Unity MonoBehaviour包装器（运行时）
├── Editor/
│   ├── McpServerEditorWindow.cs    # 编辑器窗口
│   ├── EditorMcpServerManager.cs   # 编辑器服务器管理器
│   ├── UnityMcpServerEditor.cs     # 自定义Inspector
│   └── McpServerMenuItems.cs       # 菜单项和快捷操作
└── Examples/
    ├── UnityMcpExamples.cs         # 示例工具和资源
    └── UnityMcpServerExample.cs    # 示例启动脚本
```

### 3. 验证安装

安装完成后，你应该能看到：
- 菜单栏出现 **Tools → MCP Server**
- 可以通过 **Window → MCP Server** 打开控制面板

使用 `Tools → MCP Server → Check Dependencies` 验证Newtonsoft.Json是否正确安装。

## 快速开始

### 方式一：编辑器模式（推荐）⭐

1. 打开 **Window → MCP Server** 或按 **Ctrl+Shift+M** (Windows) / **Cmd+Shift+M** (macOS)
2. 在打开的窗口中点击 **▶ Start Server**
3. 服务器启动成功！可以在窗口中看到：
   - 服务器状态和端点
   - 已注册的工具和资源列表
   - 实时日志输出

**优势：**
- 无需进入Play模式
- 服务器持续运行，不受场景切换影响
- 可视化控制面板，操作直观
- 支持自动启动（在窗口中勾选 Auto Start）

### 方式二：运行时模式

1. 在场景中创建GameObject：右键 → **Create Empty**
2. 重命名为 "MCP Server"
3. 添加组件：`UnityMcpServer` 和 `UnityMcpServerExample`
4. 点击Play按钮
5. 查看Console日志确认服务器启动

**或使用菜单快速创建：**
`Tools → MCP Server → Create Runtime Server in Scene`

## 使用指南

### 编辑器窗口功能

打开 `Window → MCP Server`，你将看到：

- **Server Configuration**: 配置端口、服务器名称、自动启动等
- **Server Status**: 实时显示服务器状态和端点
- **Registered Tools**: 查看所有已注册的工具
- **Registered Resources**: 查看所有已注册的资源
- **Server Logs**: 实时日志输出，支持自动滚动

### 创建自定义工具

继承 `McpTool` 抽象类：

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class MyCustomTool : McpTool
{
    public override string Name => "my_custom_tool";
    public override string Description => "这是我的自定义工具";

    public override JObject GetInputSchema()
    {
        return new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["parameter1"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "第一个参数"
                }
            },
            ["required"] = new JArray { "parameter1" }
        };
    }

    public override Task<CallToolResult> ExecuteAsync(Dictionary<string, object> arguments)
    {
        var param1 = arguments["parameter1"].ToString();

        // 如果需要访问Unity API，使用主线程调度器
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log($"执行工具，参数: {param1}");
        });

        return Task.FromResult(CreateTextResult($"处理完成: {param1}"));
    }
}
```

### 创建自定义资源

继承 `McpResource` 抽象类：

```csharp
using System.Threading.Tasks;
using UnityEngine;

public class MyCustomResource : McpResource
{
    public override string Uri => "unity://my/resource";
    public override string Name => "我的资源";
    public override string Description => "自定义资源描述";
    public override string MimeType => "text/plain";

    public override async Task<ResourceContents> ReadAsync()
    {
        var taskCompletionSource = new TaskCompletionSource<ResourceContents>();

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            // 读取Unity数据
            var data = "资源内容";

            taskCompletionSource.SetResult(new ResourceContents
            {
                Uri = Uri,
                MimeType = MimeType,
                Text = data
            });
        });

        return await taskCompletionSource.Task;
    }
}
```

### 注册工具和资源

在你的MonoBehaviour脚本中：

```csharp
public class MyMcpSetup : MonoBehaviour
{
    private UnityMcpServer mcpServer;

    private void Awake()
    {
        mcpServer = GetComponent<UnityMcpServer>();

        // 注册工具
        mcpServer.RegisterTool(new MyCustomTool());

        // 注册资源
        mcpServer.RegisterResource(new MyCustomResource());
    }

    private void Update()
    {
        // 重要：必须调用以执行主线程操作
        UnityMainThreadDispatcher.Update();
    }
}
```

## 内置示例工具

### 1. unity_log
写入Unity控制台日志
```json
{
  "name": "unity_log",
  "arguments": {
    "message": "Hello from MCP!",
    "level": "info"  // "info" | "warning" | "error"
  }
}
```

### 2. unity_find_gameobject
在场景中查找GameObject
```json
{
  "name": "unity_find_gameobject",
  "arguments": {
    "searchType": "name",  // "name" | "tag"
    "searchValue": "Player"
  }
}
```

### 3. unity_scene_info
获取当前场景信息
```json
{
  "name": "unity_scene_info",
  "arguments": {}
}
```

## 连接MCP客户端

### 使用Claude Desktop

1. 打开Claude Desktop配置文件：
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

2. 添加Unity服务器配置：
```json
{
  "mcpServers": {
    "unity": {
      "url": "http://localhost:3000"
    }
  }
}
```

3. 重启Claude Desktop并运行Unity项目

### 使用自定义客户端

发送HTTP POST请求到 `http://localhost:3000`，body为JSON-RPC 2.0格式：

```bash
# 初始化
curl -X POST http://localhost:3000 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {"name": "test-client", "version": "1.0"}
    }
  }'

# 列出工具
curl -X POST http://localhost:3000 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'

# 调用工具
curl -X POST http://localhost:3000 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "unity_log",
      "arguments": {"message": "Hello!", "level": "info"}
    }
  }'
```

## 架构说明

### 核心组件

1. **McpTypes.cs**
   - MCP协议的所有类型定义
   - 使用Newtonsoft.Json序列化特性

2. **McpServer.cs**
   - JSON-RPC 2.0请求处理
   - 工具/资源/Prompt注册表
   - `McpTool`、`McpResource`、`McpPrompt` 抽象基类

3. **McpHttpServer.cs**
   - 基于TcpListener的轻量级HTTP服务器
   - 无外部依赖
   - 后台线程处理HTTP请求

4. **UnityMcpServer.cs**
   - Unity MonoBehaviour包装器
   - Inspector配置界面
   - 服务器生命周期管理

5. **UnityMainThreadDispatcher**
   - 线程安全的主线程调度器
   - 用于从后台线程访问Unity API

### 线程模型

```
[HTTP请求]
    ↓ (后台线程)
[McpHttpServer]
    ↓
[McpServer.HandleRequestAsync()]
    ↓
[Tool.ExecuteAsync()]
    ↓ (需要Unity API时)
[UnityMainThreadDispatcher.Enqueue()]
    ↓
[Unity主线程] → [Unity API调用]
```

## 常见问题

### Q: 服务器启动失败，提示端口被占用
**A:** 修改 `UnityMcpServer` 组件的端口号，或关闭占用该端口的其他程序。

### Q: 工具执行时Unity崩溃
**A:** 确保在 `Update()` 中调用 `UnityMainThreadDispatcher.Update()`，所有Unity API调用必须通过主线程调度器。

### Q: 客户端连接超时
**A:** 检查防火墙设置，确保允许localhost上的端口通信。Windows可能需要手动添加防火墙规则。

### Q: JSON序列化错误
**A:** 确保已正确安装 Newtonsoft.Json for Unity (`com.unity.nuget.newtonsoft-json`)。

### Q: 能在WebGL平台使用吗？
**A:** 不能。WebGL不支持多线程和Socket编程，无法运行HTTP服务器。此实现仅适用于Standalone和Editor平台。

## 协议兼容性

本实现遵循 **MCP协议版本 2024-11-05**，支持：
- ✅ Tools (工具)
- ✅ Resources (资源)
- ✅ Prompts (提示)
- ✅ JSON-RPC 2.0
- ❌ Sampling (需要客户端支持)
- ❌ Roots (不适用于Unity场景)

## 性能建议

- **工具执行时间**：尽量保持在100ms以内，避免阻塞MCP客户端
- **资源大小**：建议单个资源内容小于1MB
- **并发请求**：HTTP服务器支持并发，但Unity API调用会在主线程串行执行
- **日志输出**：生产环境可关闭详细日志以提升性能

## 许可证

MIT License - 自由使用、修改和分发

## 贡献

欢迎提交Issue和Pull Request！

## 参考资料

- [MCP官方文档](https://modelcontextprotocol.io)
- [MCP协议规范](https://github.com/modelcontextprotocol/specification)
- [Newtonsoft.Json文档](https://www.newtonsoft.com/json/help/html/Introduction.htm)
