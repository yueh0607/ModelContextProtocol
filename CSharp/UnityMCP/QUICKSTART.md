# Unity MCP Server - 快速开始

30秒内让Unity MCP服务器运行起来！

## 📦 第一步：安装依赖

1. 打开Unity项目
2. 打开 **Window → Package Manager**
3. 点击 **+ → Add package by name...**
4. 输入：`com.unity.nuget.newtonsoft-json`
5. 点击 **Add**

## 📂 第二步：导入代码

将 `UnityMCP` 文件夹复制到你的Unity项目的 `Assets/Scripts/` 目录下。

## 🚀 第三步：启动服务器（编辑器模式）

### 方法A：使用窗口（推荐）⭐

1. 打开 **Window → MCP Server** 或按快捷键：
   - Windows/Linux: **Ctrl+Shift+M**
   - macOS: **Cmd+Shift+M**

2. 在弹出的窗口中点击 **▶ Start Server**

3. 完成！你应该看到：
   ```
   Status: ● Running
   Endpoint: http://localhost:3000
   ```

### 方法B：使用菜单

`Tools → MCP Server → Open Server Window`

## ✅ 第四步：验证运行

### 测试连接

打开命令行，运行：

**Windows (PowerShell):**
```powershell
Invoke-WebRequest -Uri http://localhost:3000 -Method POST -ContentType "application/json" -Body '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
```

**macOS/Linux:**
```bash
curl -X POST http://localhost:3000 \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
```

应该返回包含工具列表的JSON响应。

### 查看窗口

在MCP Server窗口中你可以看到：
- ✅ **3个已注册的工具**:
  - `unity_log` - 写入Unity日志
  - `unity_find_gameobject` - 查找GameObject
  - `unity_scene_info` - 获取场景信息

- ✅ **1个已注册的资源**:
  - `unity://scene/hierarchy` - 场景层级结构

- ✅ **实时日志输出**

## 🎯 编辑器模式 vs 运行时模式

### 编辑器模式（刚才使用的）⭐

**优势：**
- ✅ **无需Play模式** - 直接在编辑器运行
- ✅ **持续运行** - 不受场景切换影响
- ✅ **可视化控制** - 直观的控制面板
- ✅ **自动启动** - 勾选后每次打开Unity自动运行

**适用场景：**
- 日常开发时使用AI辅助
- 让AI帮你编辑场景、查找对象
- 调试和测试工具

### 运行时模式（可选）

如果你需要在游戏运行时使用MCP：

1. 使用菜单：`Tools → MCP Server → Create Runtime Server in Scene`
2. 或手动创建：
   - 创建空GameObject "MCP Server"
   - 添加 `UnityMcpServer` 组件
   - 添加 `UnityMcpServerExample` 组件
3. 点击Play

**适用场景：**
- 游戏运行时调试
- 运行时AI集成
- 测试游戏内工具

## 🔌 连接Claude Desktop

1. 找到Claude Desktop配置文件：
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

2. 编辑文件，添加：
```json
{
  "mcpServers": {
    "unity": {
      "url": "http://localhost:3000"
    }
  }
}
```

3. 重启Claude Desktop

4. 确保Unity编辑器中MCP Server窗口显示 "● Running"

5. 在Claude中询问："列出所有可用的Unity工具"

## 🛠 自定义工具（进阶）

### 创建自己的工具

1. 创建新的C#脚本：

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityMCP;
using Newtonsoft.Json.Linq;

public class MyCustomTool : McpTool
{
    public override string Name => "my_tool";
    public override string Description => "我的自定义工具";

    public override JObject GetInputSchema()
    {
        return new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["message"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "输入消息"
                }
            },
            ["required"] = new JArray { "message" }
        };
    }

    public override Task<CallToolResult> ExecuteAsync(Dictionary<string, object> arguments)
    {
        var message = arguments["message"].ToString();
        UnityEngine.Debug.Log($"收到消息: {message}");
        return Task.FromResult(CreateTextResult($"处理完成: {message}"));
    }
}
```

2. 注册工具：

在编辑器中注册工具需要创建一个Editor脚本：

```csharp
using UnityEditor;
using UnityMCP.Editor;

[InitializeOnLoad]
public static class MyToolsRegistration
{
    static MyToolsRegistration()
    {
        EditorApplication.delayCall += () =>
        {
            var window = EditorWindow.GetWindow<McpServerEditorWindow>();
            if (window != null)
            {
                // 注册你的工具
                // 注意：需要修改EditorMcpServerManager使其可以从外部注册工具
            }
        };
    }
}
```

或者在运行时模式下，编辑 `UnityMcpServerExample.cs`：

```csharp
private void RegisterToolsAndResources()
{
    // 现有工具...
    mcpServer.RegisterTool(new UnityLogTool());

    // 添加你的工具
    mcpServer.RegisterTool(new MyCustomTool());
}
```

## 💡 常用功能

### 自动启动

在MCP Server窗口中：
1. 勾选 **Auto Start**
2. 下次打开Unity时服务器会自动启动

### 查看日志

- 在MCP Server窗口底部查看 **Server Logs**
- 勾选 **Auto-scroll** 自动滚动到最新日志
- 取消勾选 **Verbose Logging** 减少输出

### 更改端口

1. 停止服务器（如果正在运行）
2. 在 **Server Configuration** 中修改 **Port**
3. 重新启动服务器

### 快捷操作

- **Ctrl+Shift+M** (Cmd+Shift+M) - 打开MCP Server窗口
- **Tools → MCP Server → Check Dependencies** - 检查依赖
- **Tools → MCP Server → About** - 查看版本信息

## ❓ 常见问题

### Q: 窗口找不到？
**A:** 使用菜单 `Window → MCP Server` 打开

### Q: 端口被占用？
**A:** 在窗口中修改Port为其他端口号（如3001）

### Q: 工具调用失败？
**A:** 检查Console是否有错误信息，确保Newtonsoft.Json已正确安装

### Q: Claude连接不上？
**A:**
1. 确保MCP Server窗口显示"● Running"
2. 检查端口号是否与配置文件一致
3. 重启Claude Desktop

### Q: 想在运行时使用怎么办？
**A:** 使用 `Tools → MCP Server → Create Runtime Server in Scene` 创建运行时服务器

## 📚 下一步

- 查看 [README.md](./README.md) 了解完整功能
- 学习创建自定义工具和资源
- 探索更多Unity API集成

完成！现在你可以让AI助手控制你的Unity编辑器了 🎉
