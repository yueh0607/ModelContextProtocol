using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ModelContextProtocol.Protocol;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 模糊路径处理器
    /// 自动将模糊路径转换为完整的资源路径
    /// 支持相对路径、绝对路径、文件名查找等多种格式
    ///
    /// 注意：此处理器使用 AssetDatabase API，必须在 Unity 主线程中执行
    /// 参数处理在工具方法调用前执行，如果不在主线程会自动调度到主线程（同步阻塞）
    /// </summary>
    public class FuzzyPathProcessorAttribute : McpParameterProcessorAttribute
    {
        /// <summary>
        /// 是否在找不到资源时保持原值
        /// true: 找不到时返回原值
        /// false: 找不到时返回错误
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

        // 缓存 AssetPathExists 方法（Unity 2023+），如果不存在则为 null
        private static readonly MethodInfo _assetPathExistsMethod = typeof(AssetDatabase).GetMethod(
            "AssetPathExists",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string) },
            null
        );

        /// <summary>
        /// 处理模糊路径参数
        /// </summary>
        /// <param name="value">原始路径值</param>
        /// <param name="parameterType">参数类型</param>
        /// <returns>处理后的资源路径，如果找不到且 KeepOriginalIfNotFound=true 则返回原值，否则返回错误</returns>
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
                // 确保在主线程执行（AssetDatabase API 要求）
                string normalizedPath = EnsureMainThread(() =>
                {
                    // 规范化路径并查找资源
                    return NormalizeAndFindAssetPath(inputPath, FileExtension, AssetType);
                });

                // 如果找到了路径
                if (!string.IsNullOrEmpty(normalizedPath))
                {
                    // 根据配置决定是否添加 Assets/ 前缀
                    string resultPath = IncludeAssetsPrefix
                        ? BuildUnityAssetPath(normalizedPath)
                        : normalizedPath;

                    return resultPath;
                }

                // 没找到时，根据配置决定是否保持原值或返回错误
                return KeepOriginalIfNotFound
                    ? value
                    : McpUtils.Error($"Asset not found: {inputPath}");
            }
            catch (ArgumentException ex)
            {
                // 参数验证失败（如多个匹配），返回错误
                return McpUtils.Error(ex.Message);
            }
            catch (Exception ex)
            {
                // 其他处理失败，记录详细错误
                Debug.LogWarning($"[FuzzyPathProcessor] Error processing path '{inputPath}': {ex.Message}");
                return KeepOriginalIfNotFound
                    ? value
                    : McpUtils.Error($"Failed to process path '{inputPath}': {ex.Message}");
            }
        }

        /// <summary>
        /// 确保在主线程执行
        /// </summary>
        private static T EnsureMainThread<T>(Func<T> func)
        {
            // 检查是否在主线程
            if (IsMainThread())
            {
                return func();
            }
            else
            {
                // 不在主线程，使用调度器并同步等待
                // 注意：这会阻塞当前线程，但由于参数处理必须是同步的，这是唯一选择
                return UnityMainThreadScheduler.ExecuteAsync(func).Result;
            }
        }

        /// <summary>
        /// 检查当前是否在主线程
        /// </summary>
        private static bool IsMainThread()
        {
            // Unity 编辑器中，主线程 ID 通常是固定的
            // 但更可靠的方法是尝试访问只能在主线程访问的 API
            try
            {
                // Application.isPlaying 只能在主线程访问
                var _ = Application.isPlaying;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查资源路径是否存在（兼容不同 Unity 版本）
        /// </summary>
        private static bool AssetPathExists(string assetPath)
        {
            // 方法1: 使用 AssetPathExists API（Unity 2023+）
            if (_assetPathExistsMethod != null)
            {
                try
                {
                    return (bool)_assetPathExistsMethod.Invoke(null, new object[] { assetPath });
                }
                catch
                {
                    // 反射调用失败，使用备用方法
                }
            }

            // 方法2: 使用 AssetDatabase.LoadAssetAtPath（兼容所有版本）
            // 这个方法会加载资源，性能稍差，但兼容性好
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null;
        }

        /// <summary>
        /// 规范化路径并查找资源，返回相对于Assets的路径（不含Assets/前缀）
        /// 支持的输入格式：
        /// 1. 纯文件名: "MyPrefab" 或 "MyPrefab.prefab"
        /// 2. 相对路径: "Prefabs/MyPrefab.prefab"
        /// 3. 带Assets前缀: "Assets/Prefabs/MyPrefab.prefab"
        /// 4. 绝对路径: "G:/Unity/MyProject/Assets/Prefabs/MyPrefab.prefab"
        /// 5. 部分路径: "Subdir/MyPrefab.prefab"（会搜索匹配的完整路径）
        /// </summary>
        private static string NormalizeAndFindAssetPath(string inputPath, string fileExtension = null, string assetType = null)
        {
            if (string.IsNullOrEmpty(inputPath))
                return null;

            // 统一路径分隔符为正斜杠
            string normalizedPath = inputPath.Replace('\\', '/').Trim();

            // 步骤1: 提取相对于Assets的路径部分
            string relativePath = ExtractRelativePath(normalizedPath);

            // 步骤2: 根据路径类型进行处理
            if (string.IsNullOrEmpty(relativePath))
            {
                // Case 1: 纯文件名 - 进行全局搜索
                return SearchByFileName(normalizedPath, fileExtension, assetType);
            }
            else
            {
                // Case 2-7: 包含路径信息 - 验证或匹配
                return VerifyOrMatchPath(relativePath, fileExtension, assetType);
            }
        }

        /// <summary>
        /// 从输入路径中提取相对于Assets的路径部分
        /// 返回null表示输入是纯文件名（不包含路径分隔符）
        /// </summary>
        private static string ExtractRelativePath(string normalizedPath)
        {
            // Case 3: 如果路径以 "Assets/" 开头，移除前缀
            if (normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath.Substring(7);
            }

            // 如果路径以 "Assets" 开头（没有斜杠），处理边界情况
            if (normalizedPath.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return ""; // Assets根目录
            }
            else if (normalizedPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase) && normalizedPath.Length > 6)
            {
                // 处理 "AssetsFolder" 这种情况 - 不是Assets目录
                char nextChar = normalizedPath[6];
                if (nextChar == '/')
                {
                    return normalizedPath.Substring(7);
                }
                // 否则不是Assets前缀，继续处理
            }

            // Case 4: 如果是绝对路径且包含 "/Assets/" 目录，提取Assets之后的部分
            if (normalizedPath.Contains("/Assets/"))
            {
                int assetsIndex = normalizedPath.LastIndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
                return normalizedPath.Substring(assetsIndex + 8); // +8 for "/Assets/"
            }

            // Case 5: 如果包含Application.dataPath，移除它
            if (!string.IsNullOrEmpty(Application.dataPath))
            {
                string dataPath = Application.dataPath.Replace('\\', '/');
                if (normalizedPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                {
                    string remaining = normalizedPath.Substring(dataPath.Length);
                    return remaining.TrimStart('/');
                }
            }

            // Case 2, 6, 7: 如果包含路径分隔符，说明是相对路径或部分路径
            if (normalizedPath.Contains("/"))
            {
                return normalizedPath.TrimStart('/');
            }

            // Case 1: 纯文件名
            return null;
        }

        /// <summary>
        /// 通过文件名搜索资源（Case 1: 纯文件名）
        /// </summary>
        private static string SearchByFileName(string fileName, string fileExtension = null, string assetType = null)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            // 处理文件扩展名
            string searchFileName = fileName;
            if (!string.IsNullOrEmpty(fileExtension))
            {
                // 确保扩展名以点开头
                string ext = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;

                // 如果文件名没有扩展名，添加它
                if (!searchFileName.Contains("."))
                {
                    searchFileName += ext;
                }
                // 如果有扩展名但不匹配，不添加
            }

            // 构建搜索查询
            string searchQuery = Path.GetFileNameWithoutExtension(searchFileName);
            if (!string.IsNullOrEmpty(assetType))
            {
                searchQuery += " " + assetType;
            }

            // 使用AssetDatabase查找所有匹配的文件
            string[] guids = AssetDatabase.FindAssets(searchQuery);
            var matchingPaths = new List<string>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetFileName = Path.GetFileName(assetPath);

                // 精确匹配文件名（忽略大小写）
                bool isMatch = false;
                if (!string.IsNullOrEmpty(fileExtension))
                {
                    // 如果指定了扩展名，必须精确匹配文件名（包括扩展名）
                    isMatch = string.Equals(assetFileName, searchFileName, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    // 如果没有指定扩展名，只匹配文件名部分（不含扩展名）
                    string assetFileNameWithoutExt = Path.GetFileNameWithoutExtension(assetFileName);
                    string searchFileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    isMatch = string.Equals(assetFileNameWithoutExt, searchFileNameWithoutExt, StringComparison.OrdinalIgnoreCase);
                }

                if (isMatch)
                {
                    // 移除Assets/前缀，返回相对路径
                    string relativePath = assetPath.StartsWith("Assets/") ? assetPath.Substring(7) : assetPath;
                    matchingPaths.Add(relativePath);
                }
            }

            if (matchingPaths.Count == 1)
            {
                return matchingPaths[0];
            }
            else if (matchingPaths.Count > 1)
            {
                string matches = string.Join("\n  - Assets/", matchingPaths);
                throw new ArgumentException(
                    $"Multiple assets found matching '{fileName}':\n  - Assets/{matches}\n\nPlease specify a more precise path.");
            }

            // 未找到匹配
            return null;
        }

        /// <summary>
        /// 验证或匹配路径（Case 2-7: 包含路径的输入）
        /// </summary>
        private static string VerifyOrMatchPath(string relativePath, string fileExtension = null, string assetType = null)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            // 移除开头的斜杠
            relativePath = relativePath.TrimStart('/');

            // 策略1: 直接验证完整路径是否存在（Case 2, 3）
            string fullAssetPath = "Assets/" + relativePath;
            if (AssetPathExists(fullAssetPath))
            {
                // 如果指定了文件扩展名，验证是否匹配
                if (!string.IsNullOrEmpty(fileExtension))
                {
                    string ext = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;
                    if (fullAssetPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        return relativePath;
                    }
                    // 不匹配，继续尝试其他策略
                }
                else
                {
                    return relativePath;
                }
            }

            // 策略2: 如果路径不包含扩展名，尝试添加扩展名（Case 6）
            if (!string.IsNullOrEmpty(fileExtension) && !relativePath.Contains("."))
            {
                string ext = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;
                string pathWithExt = relativePath + ext;
                fullAssetPath = "Assets/" + pathWithExt;
                if (AssetPathExists(fullAssetPath))
                {
                    return pathWithExt;
                }
            }

            // 策略3: 部分路径匹配 - 搜索以该路径结尾的所有资源（Case 7）
            string fileName = Path.GetFileName(relativePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            // 使用 AssetDatabase 搜索
            string searchQuery = fileNameWithoutExt;
            if (!string.IsNullOrEmpty(assetType))
            {
                searchQuery += " " + assetType;
            }

            string[] guids = AssetDatabase.FindAssets(searchQuery);
            var matchingPaths = new List<string>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 检查是否以部分路径结尾（忽略大小写）
                if (assetPath.EndsWith(relativePath, StringComparison.OrdinalIgnoreCase) ||
                    assetPath.EndsWith("/" + relativePath, StringComparison.OrdinalIgnoreCase))
                {
                    // 如果指定了扩展名，验证是否匹配
                    if (!string.IsNullOrEmpty(fileExtension))
                    {
                        string ext = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;
                        if (assetPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingPaths.Add(assetPath.Substring(7)); // 移除 "Assets/" 前缀
                        }
                    }
                    else
                    {
                        matchingPaths.Add(assetPath.Substring(7));
                    }
                }
            }

            if (matchingPaths.Count == 1)
            {
                return matchingPaths[0];
            }
            else if (matchingPaths.Count > 1)
            {
                string matches = string.Join("\n  - Assets/", matchingPaths);
                throw new ArgumentException(
                    $"Multiple assets found matching partial path '{relativePath}':\n  - Assets/{matches}\n\nPlease specify a more precise path.");
            }

            // 未找到匹配
            return null;
        }

        /// <summary>
        /// 构建Unity资源路径（用于AssetDatabase API）
        /// </summary>
        private static string BuildUnityAssetPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException("Relative path cannot be null or empty");
            }

            // 确保使用正斜杠
            relativePath = relativePath.Replace('\\', '/');

            // 移除开头的斜杠（如果有）
            relativePath = relativePath.TrimStart('/');

            // 再次验证处理后的路径不为空
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException("Invalid relative path after normalization");
            }

            return "Assets/" + relativePath;
        }
    }
}
