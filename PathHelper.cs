using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityMcpBridge.Editor.Helpers
{
    /// <summary>
    /// GameObject路径匹配和查找的辅助工具类
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// 搜索指定文件名的资源文件
        /// </summary>
        /// <param name="fileName">文件名（可以包含或不包含扩展名）</param>
        /// <param name="fileExtension">文件扩展名（可选，如".prefab", ".cs", ".txt"等）</param>
        /// <param name="assetType">资源类型过滤器（可选，如"t:Prefab", "t:Script", "t:Texture2D"等）</param>
        /// <returns>找到的资源文件路径列表（相对于Assets）</returns>
        public static List<string> SearchAssetByFileName(string fileName, string fileExtension = null, string assetType = null)
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
        /// 搜索指定文件名的prefab文件（保持向后兼容）
        /// </summary>
        /// <param name="fileName">文件名（可以包含或不包含.prefab扩展名）</param>
        /// <returns>找到的prefab文件路径列表（相对于Assets）</returns>
        public static List<string> SearchPrefabByFileName(string fileName)
        {
            return SearchAssetByFileName(fileName, ".prefab", "t:Prefab");
        }

        /// <summary>
        /// 搜索指定文件名的脚本文件
        /// </summary>
        /// <param name="fileName">文件名（可以包含或不包含.cs扩展名）</param>
        /// <returns>找到的脚本文件路径列表（相对于Assets）</returns>
        public static List<string> SearchScriptByFileName(string fileName)
        {
            return SearchAssetByFileName(fileName, ".cs", "t:Script");
        }

        /// <summary>
        /// 搜索指定文件名的材质文件
        /// </summary>
        /// <param name="fileName">文件名（可以包含或不包含.mat扩展名）</param>
        /// <returns>找到的材质文件路径列表（相对于Assets）</returns>
        public static List<string> SearchMaterialByFileName(string fileName)
        {
            return SearchAssetByFileName(fileName, ".mat", "t:Material");
        }

        /// <summary>
        /// 搜索指定文件名的贴图文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>找到的贴图文件路径列表（相对于Assets）</returns>
        public static List<string> SearchTextureByFileName(string fileName)
        {
            return SearchAssetByFileName(fileName, null, "t:Texture2D");
        }

        /// <summary>
        /// 搜索指定文件名的音频文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>找到的音频文件路径列表（相对于Assets）</returns>
        public static List<string> SearchAudioByFileName(string fileName)
        {
            return SearchAssetByFileName(fileName, null, "t:AudioClip");
        }

        /// <summary>
        /// 搜索指定文件名的场景文件
        /// </summary>
        /// <param name="fileName">文件名（可以包含或不包含.unity扩展名）</param>
        /// <returns>找到的场景文件路径列表（相对于Assets）</returns>
        public static List<string> SearchSceneByFileName(string fileName)
        {
            return SearchAssetByFileName(fileName, ".unity", "t:Scene");
        }

        /// <summary>
        /// 规范化资源路径，支持多种输入格式并自动转换为相对于Assets的路径
        /// </summary>
        /// <param name="inputPath">输入路径</param>
        /// <param name="fileExtension">文件扩展名（可选，用于文件名搜索）</param>
        /// <param name="assetType">资源类型过滤器（可选，用于文件名搜索）</param>
        /// <param name="logPrefix">日志前缀（可选，用于调试信息）</param>
        /// <returns>相对于Assets的路径，如果路径无效返回null</returns>
        public static string NormalizeAssetPath(string inputPath, string fileExtension = null, string assetType = null, string logPrefix = "PathHelper")
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
                    // 找到唯一匹配，使用它
                    normalizedPath = searchResults[0];
                    string assetTypeDesc = !string.IsNullOrEmpty(assetType) ? assetType.Replace("t:", "") : "asset";
                    Debug.Log($"[{logPrefix}] Found {assetTypeDesc} by filename: '{inputPath}' -> '{normalizedPath}'");
                }
                else if (searchResults.Count > 1)
                {
                    // 找到多个匹配，返回null并让调用者处理
                    string assetTypeDesc = !string.IsNullOrEmpty(assetType) ? assetType.Replace("t:", "") + "s" : "assets";
                    Debug.LogWarning($"[{logPrefix}] Multiple {assetTypeDesc} found with filename '{inputPath}': {string.Join(", ", searchResults.Select(p => "Assets/" + p))}");
                    return null;
                }
                else
                {
                    // 没找到匹配的文件
                    string assetTypeDesc = !string.IsNullOrEmpty(assetType) ? assetType.Replace("t:", "") : "asset";
                    Debug.LogWarning($"[{logPrefix}] No {assetTypeDesc} found with filename '{inputPath}'");
                    return null;
                }
            }
            
            // 移除开头的斜杠（如果有的话）
            if (normalizedPath.StartsWith("/"))
                normalizedPath = normalizedPath.Substring(1);
            
            return normalizedPath;
        }

        /// <summary>
        /// 规范化Prefab路径（保持向后兼容）
        /// </summary>
        /// <param name="inputPath">输入路径</param>
        /// <returns>相对于Assets的路径，如果路径无效返回null</returns>
        public static string NormalizePrefabPath(string inputPath)
        {
            return NormalizeAssetPath(inputPath, ".prefab", "t:Prefab", "PathHelper");
        }

        /// <summary>
        /// 规范化脚本文件路径
        /// </summary>
        /// <param name="inputPath">输入路径</param>
        /// <returns>相对于Assets的路径，如果路径无效返回null</returns>
        public static string NormalizeScriptPath(string inputPath)
        {
            return NormalizeAssetPath(inputPath, ".cs", "t:Script", "PathHelper");
        }

        /// <summary>
        /// 规范化材质路径
        /// </summary>
        /// <param name="inputPath">输入路径</param>
        /// <returns>相对于Assets的路径，如果路径无效返回null</returns>
        public static string NormalizeMaterialPath(string inputPath)
        {
            return NormalizeAssetPath(inputPath, ".mat", "t:Material", "PathHelper");
        }

        /// <summary>
        /// 规范化贴图路径
        /// </summary>
        /// <param name="inputPath">输入路径</param>
        /// <returns>相对于Assets的路径，如果路径无效返回null</returns>
        public static string NormalizeTexturePath(string inputPath)
        {
            return NormalizeAssetPath(inputPath, null, "t:Texture2D", "PathHelper");
        }

        /// <summary>
        /// 规范化音频文件路径
        /// </summary>
        /// <param name="inputPath">输入路径</param>
        /// <returns>相对于Assets的路径，如果路径无效返回null</returns>
        public static string NormalizeAudioPath(string inputPath)
        {
            return NormalizeAssetPath(inputPath, null, "t:AudioClip", "PathHelper");
        }

        /// <summary>
        /// 规范化场景文件路径
        /// </summary>
        /// <param name="inputPath">输入路径</param>
        /// <returns>相对于Assets的路径，如果路径无效返回null</returns>
        public static string NormalizeScenePath(string inputPath)
        {
            return NormalizeAssetPath(inputPath, ".unity", "t:Scene", "PathHelper");
        }

        /// <summary>
        /// 构建Unity资源路径（用于AssetDatabase API）
        /// </summary>
        /// <param name="relativePath">相对于Assets的路径</param>
        /// <returns>Unity资源路径（格式: Assets/...，使用正斜杠）</returns>
        public static string BuildUnityAssetPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return "Assets/";

            // 确保使用正斜杠
            relativePath = relativePath.Replace('\\', '/');
            
            // 移除开头的斜杠（如果有）
            relativePath = relativePath.TrimStart('/');
            
            return "Assets/" + relativePath;
        }

        /// <summary>
        /// 构建文件系统路径（用于File.Exists等文件系统API）
        /// </summary>
        /// <param name="relativePath">相对于Assets的路径</param>
        /// <returns>完整的文件系统路径</returns>
        public static string BuildFileSystemPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return Application.dataPath;

            // 使用Path.Combine确保正确的路径分隔符
            return Path.Combine(Application.dataPath, relativePath);
        }

        /// <summary>
        /// 路径跳跃匹配 - 在GameObject层级中查找最佳匹配路径
        /// </summary>
        /// <param name="root">根GameObject</param>
        /// <param name="searchPath">要搜索的路径</param>
        /// <returns>最匹配的GameObject，如果没有找到则返回null</returns>
        public static GameObject FindBestMatchPath(GameObject root, string searchPath)
        {
            // 空路径直接返回根对象
            if (string.IsNullOrWhiteSpace(searchPath)) return root;
            
            // 规范化路径格式
            string cleanPath = searchPath
                .Trim()
                .TrimStart('/')
                .TrimEnd('/')
                .ToLowerInvariant();
            
            // 获取所有可能的候选节点
            List<GameObject> candidates = new List<GameObject>();
            CollectAllDescendants(root, candidates);
            
            // 路径匹配度评分系统
            var scoredCandidates = candidates.Select(candidate => {
                string candidatePath = GetGameObjectPath(candidate);
                return new {
                    gameObject = candidate,
                    score = CalculatePathMatchScore(cleanPath, candidatePath)
                };
            });
            
            // 获取最佳匹配
            var bestMatch = scoredCandidates
                .OrderByDescending(x => x.score)
                .ThenBy(x => GetDepth(x.gameObject))
                .FirstOrDefault();
            
            return bestMatch?.score > 0 ? bestMatch.gameObject : null;
        }

        /// <summary>
        /// 路径匹配度评分算法
        /// </summary>
        /// <param name="searchPath">搜索路径</param>
        /// <param name="candidatePath">候选路径</param>
        /// <returns>匹配度分数(0-1.2)</returns>
        private static float CalculatePathMatchScore(string searchPath, string candidatePath)
        {
            // 路径格式处理
            string[] searchParts = searchPath.Split('/');
            string[] candidateParts = candidatePath.Split('/');

            int searchIndex = 0;
            int matchCount = 0;
            bool fullSequence = true;

            // 顺序匹配核心算法
            for (int i = 0; i < candidateParts.Length && searchIndex < searchParts.Length; i++)
            {
                if (candidateParts[i] == searchParts[searchIndex])
                {
                    matchCount++;
                    searchIndex++;
                }
                else if (searchIndex > 0)
                {
                    // 匹配中断意味着不是完整序列
                    fullSequence = false;
                }
            }

            // 计算匹配度分数（含权重）
            float sequenceScore = (float)matchCount / searchParts.Length;
            float positionScore = (float)searchIndex / searchParts.Length;
            
            // 组合评分：顺序权重70% + 位置权重30% + 完整序列奖励
            return 0.7f * sequenceScore + 
                0.3f * positionScore + 
                (fullSequence ? 0.2f : 0);
        }

        /// <summary>
        /// 收集所有子孙节点
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="collection">收集结果的列表</param>
        private static void CollectAllDescendants(GameObject parent, List<GameObject> collection)
        {
            collection.Add(parent);
            foreach (Transform child in parent.transform)
            {
                CollectAllDescendants(child.gameObject, collection);
            }
        }

        /// <summary>
        /// 获取GameObject的完整路径
        /// </summary>
        /// <param name="go">目标GameObject</param>
        /// <returns>从根到目标的完整路径字符串</returns>
        public static string GetGameObjectPath(GameObject go)
        {
            List<string> pathParts = new List<string>();
            Transform current = go.transform;
            
            while (current != null)
            {
                pathParts.Add(current.name.ToLowerInvariant());
                current = current.parent;
            }
            
            pathParts.Reverse();
            return string.Join("/", pathParts);
        }

        /// <summary>
        /// 获取GameObject在层级中的深度
        /// </summary>
        /// <param name="go">目标GameObject</param>
        /// <returns>深度值，根节点为0</returns>
        private static int GetDepth(GameObject go)
        {
            int depth = 0;
            Transform current = go.transform;
            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }
            return depth;
        }
    }
} 