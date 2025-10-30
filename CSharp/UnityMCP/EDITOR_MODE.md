# Unity MCP Server - 编辑器模式详解

本文档详细介绍Unity MCP Server的编辑器模式功能。

## 概述

编辑器模式允许MCP服务器在Unity编辑器中独立运行，**无需进入Play模式**。这是Unity MCP Server的核心特性，使得AI助手可以在日常开发过程中持续辅助你的工作。

## 核心优势

### 🚀 无需Play模式
- 直接在编辑器环境运行
- 不影响游戏开发流程
- 随时可用，无需额外准备

### 🔄 持续运行
- 不受场景切换影响
- 不受Play/Stop影响
- 编辑器打开期间始终可用

### 🎨 可视化控制
- 直观的GUI控制面板
- 实时状态监控
- 日志输出和管理

### ⚙️ 自动化支持
- 支持自动启动
- EditorPrefs持久化配置
- 快捷键操作

## 打开编辑器窗口

### 方法一：菜单栏
`Window → MCP Server`

### 方法二：快捷键
- **Windows/Linux**: `Ctrl + Shift + M`
- **macOS**: `Cmd + Shift + M`

### 方法三：Tools菜单
`Tools → MCP Server → Open Server Window`

## 窗口界面说明

### 1. 标题区域
显示服务器名称和描述

### 2. Server Configuration（服务器配置）
- **Port**: 服务器监听端口（默认3000）
  - 只能在服务器停止时修改
  - 修改后需要重启服务器生效

- **Server Name**: 服务器名称
  - 会在MCP客户端显示
  - 建议使用描述性名称

- **Auto Start**: 自动启动
  - 勾选后，每次打开Unity编辑器时自动启动服务器
  - 适合日常开发使用

- **Verbose Logging**: 详细日志
  - 启用后会在Unity Console输出所有日志
  - 调试时建议开启，正常使用可关闭

### 3. 控制按钮
- **▶ Start Server**: 启动服务器
  - 服务器停止时显示（绿色按钮）
  - 点击后立即启动，几秒内完成

- **⏹ Stop Server**: 停止服务器
  - 服务器运行时显示（红色按钮）
  - 安全停止，不会丢失数据

- **Clear Logs**: 清空日志
  - 清除窗口中的所有日志记录
  - 不影响服务器运行状态

### 4. Server Status（服务器状态）
- **Status**: 运行状态指示
  - `● Running` (绿色) - 服务器正在运行
  - `○ Stopped` (灰色) - 服务器已停止

- **Endpoint**: 服务器端点
  - 显示完整的URL地址
  - 可以直接复制用于MCP客户端配置
  - 提供"Copy"按钮快速复制到剪贴板

### 5. Registered Tools（已注册工具）
- 可折叠列表，显示所有已注册的工具
- 每个工具显示：
  - 🔧 图标 + 工具名称
  - 工具描述
- 数量显示在标题上

**默认工具：**
- `unity_log` - 写入Unity控制台日志
- `unity_find_gameobject` - 查找GameObject
- `unity_scene_info` - 获取场景信息

### 6. Registered Resources（已注册资源）
- 可折叠列表，显示所有已注册的资源
- 每个资源显示：
  - 📦 图标 + 资源名称
  - URI地址
  - 资源描述
- 数量显示在标题上

**默认资源：**
- `unity://scene/hierarchy` - 场景层级结构

### 7. Server Logs（服务器日志）
- 可折叠的日志输出区域
- **Auto-scroll**: 勾选后自动滚动到最新日志
- 支持颜色编码：
  - 🟢 绿色 - 成功操作（Started, Registered等）
  - 🔴 红色 - 错误信息（ERROR）
  - ⚪ 默认 - 普通日志
- 显示时间戳：`[HH:mm:ss]`
- 最多保留200条日志（自动清理旧日志）

## 使用流程

### 首次使用

1. **安装依赖**
   ```
   Window → Package Manager → + → Add package by name
   输入: com.unity.nuget.newtonsoft-json
   ```

2. **打开窗口**
   ```
   Window → MCP Server (或 Ctrl+Shift+M)
   ```

3. **配置服务器**
   - 检查端口号（默认3000）
   - 设置服务器名称
   - 决定是否启用自动启动

4. **启动服务器**
   ```
   点击 ▶ Start Server
   ```

5. **验证运行**
   - 检查Status显示"● Running"
   - 查看Endpoint地址
   - 确认工具和资源已注册

### 日常使用

#### 情况A：启用了Auto Start
1. 打开Unity编辑器
2. 服务器自动启动
3. 查看窗口确认状态即可开始使用

#### 情况B：未启用Auto Start
1. 打开Unity编辑器
2. 按 `Ctrl+Shift+M` 打开窗口
3. 点击 `▶ Start Server`
4. 开始使用

### 停止服务器

正常情况下不需要手动停止服务器，但如果需要：

1. 打开MCP Server窗口
2. 点击 `⏹ Stop Server`
3. 或直接关闭Unity编辑器（会自动停止）

## 配置持久化

编辑器模式使用 `EditorPrefs` 保存配置，以下设置会被持久化：

- ✅ 端口号 (Port)
- ✅ 自动启动 (Auto Start)
- ✅ 服务器名称 (Server Name)
- ✅ 详细日志 (Verbose Logging)

这意味着你的配置会在Unity重启后保持不变。

## 与MCP客户端连接

### Claude Desktop配置

1. **找到配置文件**
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

2. **添加Unity服务器**
   ```json
   {
     "mcpServers": {
       "unity": {
         "url": "http://localhost:3000"
       }
     }
   }
   ```

3. **重启Claude Desktop**

4. **验证连接**
   - 确保Unity编辑器中服务器状态为"Running"
   - 在Claude中询问："列出所有Unity工具"
   - 应该能看到3个默认工具

### 自定义MCP客户端

发送HTTP POST请求到窗口中显示的Endpoint：

```bash
curl -X POST http://localhost:3000 \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list",
    "params": {}
  }'
```

## 技术实现细节

### 架构组件

1. **McpServerEditorWindow**: 编辑器窗口UI
2. **EditorMcpServerManager**: 服务器管理器
3. **EditorApplication.update**: 主线程更新钩子
4. **UnityMainThreadDispatcher**: 线程安全的主线程调度

### 线程模型

```
[HTTP请求] → [后台线程]
    ↓
[EditorMcpServerManager]
    ↓
[McpServer.HandleRequestAsync()]
    ↓
[Tool.ExecuteAsync()]
    ↓ (需要Unity API)
[UnityMainThreadDispatcher]
    ↓
[EditorApplication.update] → [Unity主线程]
    ↓
[Unity API调用]
```

### 与运行时模式的区别

| 特性 | 编辑器模式 | 运行时模式 |
|------|-----------|-----------|
| 基类 | EditorWindow | MonoBehaviour |
| 生命周期 | 编辑器会话 | Play会话 |
| 主线程更新 | EditorApplication.update | MonoBehaviour.Update |
| 配置持久化 | EditorPrefs | SerializedField |
| UI | 自定义EditorWindow | Inspector |
| 场景依赖 | 无 | 需要GameObject |

## 常见问题

### Q: 窗口关闭后服务器还在运行吗？
**A:** 不在。窗口关闭时会自动停止服务器。

### Q: 可以同时运行编辑器模式和运行时模式吗？
**A:** 不建议。会导致端口冲突。建议使用不同的端口，或只运行一个。

### Q: Auto Start不生效？
**A:**
1. 确保勾选了Auto Start
2. 检查是否有其他程序占用端口
3. 查看Console是否有错误信息

### Q: 如何查看详细的启动日志？
**A:**
1. 勾选"Verbose Logging"
2. 查看Unity Console
3. 或在窗口中展开"Server Logs"

### Q: 服务器启动失败怎么办？
**A:**
1. 检查端口是否被占用（尝试更换端口）
2. 确认Newtonsoft.Json已安装（Tools → MCP Server → Check Dependencies）
3. 查看Console中的错误信息
4. 尝试重启Unity编辑器

### Q: 可以在编辑器模式注册自定义工具吗？
**A:** 可以！修改 `McpServerEditorWindow.cs` 中的 `RegisterDefaultTools()` 方法：

```csharp
private void RegisterDefaultTools()
{
    try
    {
        // 默认工具
        _serverManager.RegisterTool(new Examples.UnityLogTool());
        _serverManager.RegisterTool(new Examples.UnitySceneInfoTool());
        _serverManager.RegisterTool(new Examples.UnityGameObjectTool());

        // 添加你的自定义工具
        _serverManager.RegisterTool(new MyCustomTool());

        // 默认资源
        _serverManager.RegisterResource(new Examples.UnitySceneHierarchyResource());
    }
    catch (Exception ex)
    {
        Debug.LogError($"Failed to register default tools: {ex.Message}");
    }
}
```

## 性能建议

1. **日常开发**: 启用Auto Start，关闭Verbose Logging
2. **调试工具**: 启用Verbose Logging，查看详细日志
3. **长时间运行**: 定期点击"Clear Logs"清理日志，避免内存占用
4. **多项目切换**: 不同项目使用不同端口，避免冲突

## 小技巧

1. **快速访问**: 将窗口停靠在Unity界面的固定位置
2. **监控状态**: 保持窗口可见，随时查看服务器状态
3. **快捷键**: 记住 `Ctrl+Shift+M`，快速打开窗口
4. **复制端点**: 使用"Copy"按钮快速复制Endpoint地址
5. **自动滚动**: 勾选"Auto-scroll"，始终看到最新日志

## 更多资源

- [README.md](./README.md) - 完整文档
- [QUICKSTART.md](./QUICKSTART.md) - 快速开始指南
- `Tools → MCP Server → Documentation` - 快速访问文档
- `Tools → MCP Server → About` - 版本信息

编辑器模式是Unity MCP Server的推荐使用方式，让AI助手成为你日常开发的得力助手！
