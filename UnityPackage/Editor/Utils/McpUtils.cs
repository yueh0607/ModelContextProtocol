using System.Collections.Generic;
using ModelContextProtocol.Protocol;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// MCP 工具辅助方法 - 提供常用的工具返回值创建方法
    /// </summary>
    public static class McpUtils
    {
        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static CallToolResult Success(string message)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = message }
                },
                IsError = false
            };
        }

        /// <summary>
        /// 创建错误结果
        /// </summary>
        public static CallToolResult Error(string message)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Error: {message}" }
                },
                IsError = true
            };
        }

        /// <summary>
        /// 创建多条内容的结果
        /// </summary>
        public static CallToolResult Result(bool isError, params string[] messages)
        {
            var content = new List<ContentBlock>();
            foreach (var msg in messages)
            {
                content.Add(new TextContentBlock { Text = msg });
            }

            return new CallToolResult
            {
                Content = content,
                IsError = isError
            };
        }
    }
}
