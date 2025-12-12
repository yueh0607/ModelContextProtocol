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

        /// <summary>
        /// 创建带图片的成功结果（图文混排）
        /// </summary>
        /// <param name="message">文本消息</param>
        /// <param name="base64ImageData">Base64编码的图片数据</param>
        /// <param name="imageFormat">图片格式（png, jpg等）</param>
        public static CallToolResult SuccessWithImage(string message, string base64ImageData, string imageFormat = "png")
        {
            var content = new List<ContentBlock>
            {
                new TextContentBlock { Text = message }
            };

            if (!string.IsNullOrEmpty(base64ImageData))
            {
                // 转换格式到MIME类型
                string mimeType = imageFormat.ToLower() switch
                {
                    "png" => "image/png",
                    "jpg" or "jpeg" => "image/jpeg",
                    "gif" => "image/gif",
                    "webp" => "image/webp",
                    _ => "image/png"
                };

                content.Add(new ImageContentBlock
                {
                    Data = base64ImageData,
                    MimeType = mimeType
                });
            }

            return new CallToolResult
            {
                Content = content,
                IsError = false
            };
        }

        /// <summary>
        /// 创建仅包含图片的成功结果
        /// </summary>
        /// <param name="base64ImageData">Base64编码的图片数据</param>
        /// <param name="imageFormat">图片格式（png, jpg等）</param>
        public static CallToolResult SuccessImage(string base64ImageData, string imageFormat = "png")
        {
            string mimeType = imageFormat.ToLower() switch
            {
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                "webp" => "image/webp",
                _ => "image/png"
            };

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new ImageContentBlock
                    {
                        Data = base64ImageData,
                        MimeType = mimeType
                    }
                },
                IsError = false
            };
        }
    }
}
