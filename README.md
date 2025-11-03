# ModelContextProtocolCSharp
基于.NET Standard 2.0 的MCP Server实现，用于嵌入unity和各种允许CSharp开发插件的程序内。


## 开始
打开MCP工具设置，点击Start Server，开启MCP服务器
<img width="326" height="265" alt="局部截取_20251103_202751" src="https://github.com/user-attachments/assets/b808bc0f-e647-41ec-9ca1-7d38e4e1d042" />
<img width="1467" height="1118" alt="Unity exe_20251103_202823" src="https://github.com/user-attachments/assets/53f46fd5-a67f-4f8c-b2b7-4acf72d0b543" />

于Cursor设置中修改MCP设置
```json
{
  "mcpServers": {
    "unity-mcp": {
      "transport": "http",
      "url": "http://localhost:8080/"
    }
  }
}
```
<img width="1051" height="430" alt="image" src="https://github.com/user-attachments/assets/de4c4d40-ad00-486c-bb38-960f56247799" />

添加你的工具：

完全通用的工具：Packages/com.yourcompany.modelcontextprotocol/Editor/BuiltInTools/McpServer.BuiltInTools.asmdef  (跟随此开源项目)
项目特化的工具：McpServer.ProjectTools.asmdef(可放在任意位置)


## Contributors
<a href="https://github.com/yueh0607/ModelContextProtocol/graphs/contributors"> <img src="https://contrib.rocks/image?repo=yueh0607/ModelContextProtocol" /> </a>
