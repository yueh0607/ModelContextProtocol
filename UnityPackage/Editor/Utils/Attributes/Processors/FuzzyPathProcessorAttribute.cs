using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 模糊路径处理器
    /// 自动将模糊路径转换为完整的资源路径
    /// 支持相对路径、绝对路径、文件名查找等多种格式
    /// </summary>
    public class FuzzyPathProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 是否在找不到资源时保持原值
        /// </summary>
        public bool KeepOriginalIfNotFound { get; set; } = true;

        /// <summary>
        /// 文件扩展名（可选，如 ".prefab", ".cs" 等）
        /// 用于文件名搜索时的类型过滤
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// 资源类型过滤器（可选，如 "t:Prefab", "t:Script" 等）
        /// 用于 AssetDatabase 搜索时的类型过滤
        /// </summary>
        public string AssetType { get; set; }

        /// <summary>
        /// 是否返回包含 Assets/ 前缀的完整路径
        /// 默认为 true，返回 "Assets/..." 格式
        /// 如果为 false，返回相对于 Assets 的路径
        /// </summary>
        public bool IncludeAssetsPrefix { get; set; } = true;

        /// <summary>
        /// 处理模糊路径参数
        /// </summary>
        /// <param name="value">原始路径值</param>
        /// <param name="parameterType">参数类型</param>
        /// <returns>处理后的资源路径，如果找不到且 KeepOriginalIfNotFound=true 则返回 null（保持原值）</returns>
        public override object Process(object value, Type parameterType)
        {
            // 只处理字符串类型
            if (value == null || parameterType != typeof(string))
            {
                return value;
            }

            string inputPath = value.ToString();
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                return value;
            }

            try
            {
                // 规范化路径
                string normalizedPath = NormalizeAssetPath(inputPath, FileExtension, AssetType);

                // 如果找到了路径
                if (!string.IsNullOrEmpty(normalizedPath))
                {
                    // 根据配置决定是否添加 Assets/ 前缀
                    if (IncludeAssetsPrefix)
                    {
                        return BuildUnityAssetPath(normalizedPath);
                    }
                    else
                    {
                        return normalizedPath;
                    }
                }

                // 没找到时，根据配置决定是否保持原值
                return KeepOriginalIfNotFound ? value : value;
            }
            catch
            {
                // 处理失败，返回原值
                return value;
            }
        }

        /// <summary>
        /// 规范化资源路径，支持多种输入格式并自动转换为相对于Assets的路径
        /// </summary>
        private static string NormalizeAssetPath(string inputPath, string fileExtension = null, string assetType = null)
        {
            if (string.IsNullOrEmpty(inputPath))
                return null;

            // 统一路径分隔符为正斜杠
            string normalizedPath = inputPath.Replace('\\', '/');

            // 移除开头和结尾的空白字符
            normalizedPath = normalizedPath.Trim();

            // 如果路径以 "Assets/" 开头，移除前缀
            if (normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath.Substring(7);
            }
            // 如果路径以 "Assets" 开头（没有斜杠），移除前缀并确保格式正确
            else if (normalizedPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath.Substring(6);
                if (normalizedPath.StartsWith("/"))
                    normalizedPath = normalizedPath.Substring(1);
            }
            // 如果是绝对路径且包含Assets目录，提取Assets之后的部分
            else if (normalizedPath.Contains("/Assets/"))
            {
                int assetsIndex = normalizedPath.LastIndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
                normalizedPath = normalizedPath.Substring(assetsIndex + 8); // +8 for "/Assets/"
            }
            else if (normalizedPath.Contains("\\Assets\\"))
            {
                int assetsIndex = normalizedPath.LastIndexOf("\\Assets\\", StringComparison.OrdinalIgnoreCase);
                normalizedPath = normalizedPath.Substring(assetsIndex + 8); // +8 for "\Assets\"
                normalizedPath = normalizedPath.Replace('\\', '/');
            }
            // 如果包含Application.dataPath，移除它
            else if (!string.IsNullOrEmpty(Application.dataPath))
            {
                string dataPath = Application.dataPath.Replace('\\', '/');
                if (normalizedPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedPath = normalizedPath.Substring(dataPath.Length);
                    if (normalizedPath.StartsWith("/"))
                        normalizedPath = normalizedPath.Substring(1);
                }
            }
            // 如果看起来只是一个文件名（不包含路径分隔符），尝试搜索
            else if (!normalizedPath.Contains("/") && !normalizedPath.Contains("\\"))
            {
                var searchResults = SearchAssetByFileName(normalizedPath, fileExtension, assetType);
                if (searchResults.Count == 1)
                {
                    // 找到唯一匹配
                    normalizedPath = searchResults[0];
                }
                else if (searchResults.Count > 1)
                {
                    // 找到多个匹配，返回null
                    return null;
                }
                else
                {
                    // 没找到匹配的文件
                    return null;
                }
            }

            // 移除开头的斜杠（如果有的话）
            if (normalizedPath.StartsWith("/"))
                normalizedPath = normalizedPath.Substring(1);

            return normalizedPath;
        }

        /// <summary>
        /// 搜索指定文件名的资源文件
        /// </summary>
        private static List<string> SearchAssetByFileName(string fileName, string fileExtension = null, string assetType = null)
        {
            var results = new List<string>();

            if (string.IsNullOrEmpty(fileName))
                return results;

            // 处理文件扩展名
            string searchFileName = fileName;
            if (!string.IsNullOrEmpty(fileExtension))
            {
                // 确保扩展名以点开头
                if (!fileExtension.StartsWith("."))
                    fileExtension = "." + fileExtension;

                // 如果文件名没有指定扩展名，添加它
                if (!searchFileName.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    searchFileName += fileExtension;
                }
            }

            // 构建搜索查询
            string searchQuery = Path.GetFileNameWithoutExtension(searchFileName);
            if (!string.IsNullOrEmpty(assetType))
            {
                searchQuery += " " + assetType;
            }

            // 使用AssetDatabase查找所有匹配的文件
            string[] guids = AssetDatabase.FindAssets(searchQuery);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetFileName = Path.GetFileName(assetPath);

                // 精确匹配文件名（忽略大小写）
                bool isMatch = false;
                if (!string.IsNullOrEmpty(fileExtension))
                {
                    // 如果指定了扩展名，必须精确匹配
                    isMatch = string.Equals(assetFileName, searchFileName, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    // 如果没有指定扩展名，只匹配文件名部分
                    string assetFileNameWithoutExt = Path.GetFileNameWithoutExtension(assetFileName);
                    string searchFileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    isMatch = string.Equals(assetFileNameWithoutExt, searchFileNameWithoutExt, StringComparison.OrdinalIgnoreCase);
                }

                if (isMatch)
                {
                    // 移除Assets/前缀，返回相对路径
                    string relativePath = assetPath.StartsWith("Assets/") ? assetPath.Substring(7) : assetPath;
                    results.Add(relativePath);
                }
            }

            return results;
        }

        /// <summary>
        /// 构建Unity资源路径（用于AssetDatabase API）
        /// </summary>
        private static string BuildUnityAssetPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return "Assets/";

            // 确保使用正斜杠
            relativePath = relativePath.Replace('\\', '/');

            // 移除开头的斜杠（如果有）
            relativePath = relativePath.TrimStart('/');

            return "Assets/" + relativePath;
        }
    }
}
