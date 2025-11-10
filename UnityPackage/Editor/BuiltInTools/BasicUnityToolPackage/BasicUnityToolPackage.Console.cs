using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// BasicUnityToolPackage - Console工具部分
    /// 提供Unity控制台日志的读取和清除功能
    /// </summary>
    public partial class BasicUnityToolPackage
    {
        #region MCP Tools

        /// <summary>
        /// 获取Unity控制台日志
        /// </summary>
        [McpTool(
            Description = "Get Unity Console logs with filtering options",
            Category = "Console"
        )]
        public async Task<CallToolResult> GetConsoleLogs(
            [McpParameter("Log types to retrieve: 'log', 'warning', 'error', 'all' (comma-separated, default: 'all')")]
            string types = "all",
            [McpParameter("Number of log entries to retrieve (default: 20)")]
            int count = 20,
            [McpParameter("Filter text - only logs containing this text will be returned")]
            string filterText = null,
            [McpParameter("Include stack traces in the output (default: false)")]
            bool includeStacktrace = false,
            CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    if (!IsConsoleReflectionInitialized())
                    {
                        return McpUtils.Error(
                            "Console tool failed to initialize due to reflection errors. Cannot access console logs.");
                    }

                    var typeList = types.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().ToLower())
                        .ToList();

                    if (typeList.Contains("all"))
                    {
                        typeList = new List<string> { "log", "warning", "error" };
                    }

                    return GetConsoleEntriesReadable(typeList, count, filterText, includeStacktrace);
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"Failed to get console logs: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 清除Unity控制台
        /// </summary>
        [McpTool(
            Description = "Clear all logs from Unity Console",
            Category = "Console"
        )]
        public async Task<CallToolResult> ClearConsole(CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    if (!IsConsoleReflectionInitialized())
                    {
                        return McpUtils.Error(
                            "Console tool failed to initialize due to reflection errors. Cannot clear console.");
                    }

                    return ClearConsoleInternal();
                }
                catch (Exception ex)
                {
                    return McpUtils.Error($"Failed to clear console: {ex.Message}");
                }
            });
        }

        #endregion

        #region Reflection Members

        private static MethodInfo _startGettingEntriesMethod;
        private static MethodInfo _endGettingEntriesMethod;
        private static MethodInfo _clearMethod;
        private static MethodInfo _getCountMethod;
        private static MethodInfo _getEntryMethod;
        private static FieldInfo _modeField;
        private static FieldInfo _messageField;
        private static FieldInfo _fileField;
        private static FieldInfo _lineField;
        private static FieldInfo _instanceIdField;

        #endregion

        #region Initialization

        /// <summary>
        /// 静态构造函数 - 初始化控制台反射成员
        /// </summary>
        static BasicUnityToolPackage()
        {
            InitializeConsoleReflection();
        }

        /// <summary>
        /// 初始化Console反射成员
        /// </summary>
        private static void InitializeConsoleReflection()
        {
            try
            {
                Type logEntriesType = typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType == null)
                    throw new Exception("Could not find internal type UnityEditor.LogEntries");

                BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                _startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", staticFlags);
                if (_startGettingEntriesMethod == null)
                    throw new Exception("Failed to reflect LogEntries.StartGettingEntries");

                _endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", staticFlags);
                if (_endGettingEntriesMethod == null)
                    throw new Exception("Failed to reflect LogEntries.EndGettingEntries");

                _clearMethod = logEntriesType.GetMethod("Clear", staticFlags);
                if (_clearMethod == null)
                    throw new Exception("Failed to reflect LogEntries.Clear");

                _getCountMethod = logEntriesType.GetMethod("GetCount", staticFlags);
                if (_getCountMethod == null)
                    throw new Exception("Failed to reflect LogEntries.GetCount");

                _getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", staticFlags);
                if (_getEntryMethod == null)
                    throw new Exception("Failed to reflect LogEntries.GetEntryInternal");

                Type logEntryType = typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntry");
                if (logEntryType == null)
                    throw new Exception("Could not find internal type UnityEditor.LogEntry");

                _modeField = logEntryType.GetField("mode", instanceFlags);
                if (_modeField == null)
                    throw new Exception("Failed to reflect LogEntry.mode");

                _messageField = logEntryType.GetField("message", instanceFlags);
                if (_messageField == null)
                    throw new Exception("Failed to reflect LogEntry.message");

                _fileField = logEntryType.GetField("file", instanceFlags);
                if (_fileField == null)
                    throw new Exception("Failed to reflect LogEntry.file");

                _lineField = logEntryType.GetField("line", instanceFlags);
                if (_lineField == null)
                    throw new Exception("Failed to reflect LogEntry.line");

                _instanceIdField = logEntryType.GetField("instanceID", instanceFlags);
                if (_instanceIdField == null)
                    throw new Exception("Failed to reflect LogEntry.instanceID");
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[BasicUnityToolPackage] Console Reflection Initialization Failed: " +
                    $"Could not setup reflection for LogEntries/LogEntry. " +
                    $"Console reading/clearing will not work. Error: {e.Message}");

                _startGettingEntriesMethod = null;
                _endGettingEntriesMethod = null;
                _clearMethod = null;
                _getCountMethod = null;
                _getEntryMethod = null;
                _modeField = null;
                _messageField = null;
                _fileField = null;
                _lineField = null;
                _instanceIdField = null;
            }
        }

        /// <summary>
        /// 检查Console反射是否成功初始化
        /// </summary>
        private static bool IsConsoleReflectionInitialized()
        {
            return _startGettingEntriesMethod != null
                && _endGettingEntriesMethod != null
                && _clearMethod != null
                && _getCountMethod != null
                && _getEntryMethod != null
                && _modeField != null
                && _messageField != null
                && _fileField != null
                && _lineField != null
                && _instanceIdField != null;
        }

        #endregion

        #region Implementation

        // LogEntry.mode 的位标志定义
        private const int ModeBitError = 1 << 0;
        private const int ModeBitAssert = 1 << 1;
        private const int ModeBitWarning = 1 << 2;
        private const int ModeBitLog = 1 << 3;
        private const int ModeBitException = 1 << 4;
        private const int ModeBitScriptingError = 1 << 9;
        private const int ModeBitScriptingWarning = 1 << 10;
        private const int ModeBitScriptingLog = 1 << 11;
        private const int ModeBitScriptingException = 1 << 18;
        private const int ModeBitScriptingAssertion = 1 << 22;

        /// <summary>
        /// 清除控制台
        /// </summary>
        private static CallToolResult ClearConsoleInternal()
        {
            try
            {
                _clearMethod.Invoke(null, null);
                return McpUtils.Success("Console cleared successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BasicUnityToolPackage] Failed to clear console: {e}");
                return McpUtils.Error($"Failed to clear console: {e.Message}");
            }
        }

        /// <summary>
        /// 获取格式化的控制台日志条目
        /// </summary>
        private static CallToolResult GetConsoleEntriesReadable(
            List<string> types,
            int count,
            string filterText,
            bool includeStacktrace)
        {
            List<string> lines = new List<string>();
            int retrievedCount = 0;
            int matchedCount = 0; // 符合条件的日志总数

            try
            {
                _startGettingEntriesMethod.Invoke(null, null);
                int totalEntries = (int)_getCountMethod.Invoke(null, null);
                Type logEntryType = typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntry");
                if (logEntryType == null)
                    throw new Exception("Could not find internal type UnityEditor.LogEntry during GetConsoleEntries.");

                object logEntryInstance = Activator.CreateInstance(logEntryType);

                for (int i = totalEntries - 1; i >= 0; i--)
                {
                    _getEntryMethod.Invoke(null, new object[] { i, logEntryInstance });
                    int mode = (int)_modeField.GetValue(logEntryInstance);
                    string message = (string)_messageField.GetValue(logEntryInstance);
                    string file = (string)_fileField.GetValue(logEntryInstance);
                    int line = (int)_lineField.GetValue(logEntryInstance);

                    if (string.IsNullOrEmpty(message))
                        continue;

                    LogType currentType = GetLogTypeFromMode(mode);
                    string typeStr = currentType.ToString().ToLowerInvariant();

                    if (types.Contains("error"))
                    {
                        if (typeStr == "assert" || typeStr == "exception")
                            typeStr = "error";
                    }

                    if (!types.Contains(typeStr))
                        continue;

                    if (!string.IsNullOrEmpty(filterText) &&
                        message.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    // 符合条件的日志计数
                    matchedCount++;

                    // 只在未达到显示上限时添加到结果
                    if (retrievedCount < count)
                    {
                        string stackTrace = includeStacktrace ? ExtractStackTrace(message) : null;
                        string messageOnly = (includeStacktrace && !string.IsNullOrEmpty(stackTrace))
                            ? message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)[0]
                            : message;

                        string lineText = $"[{typeStr}] {file}:{line} {messageOnly}";
                        if (includeStacktrace && !string.IsNullOrEmpty(stackTrace))
                        {
                            lineText += "\n" + stackTrace;
                        }

                        lines.Add(lineText);
                        retrievedCount++;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BasicUnityToolPackage] Error while retrieving log entries: {e}");
                try { _endGettingEntriesMethod.Invoke(null, null); } catch { }
                return McpUtils.Error($"Error retrieving log entries: {e.Message}");
            }
            finally
            {
                try
                {
                    _endGettingEntriesMethod.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BasicUnityToolPackage] Failed to call EndGettingEntries: {e}");
                }
            }

            if (matchedCount == 0)
            {
                return McpUtils.Success("No logs found matching the specified criteria.");
            }

            string result = string.Join("\n\n", lines);
            string summary = matchedCount > retrievedCount
                ? $"Showing {retrievedCount} of {matchedCount} matching log entries:"
                : $"Showing all {matchedCount} matching log entries:";

            return McpUtils.Success($"{summary}\n\n{result}");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// 从mode位标志获取LogType
        /// </summary>
        private static LogType GetLogTypeFromMode(int mode)
        {
            if ((mode & (ModeBitError | ModeBitScriptingError | ModeBitException | ModeBitScriptingException)) != 0)
            {
                return LogType.Error;
            }
            else if ((mode & (ModeBitAssert | ModeBitScriptingAssertion)) != 0)
            {
                return LogType.Assert;
            }
            else if ((mode & (ModeBitWarning | ModeBitScriptingWarning)) != 0)
            {
                return LogType.Warning;
            }
            else
            {
                return LogType.Log;
            }
        }

        /// <summary>
        /// 从完整日志消息中提取堆栈跟踪部分
        /// </summary>
        private static string ExtractStackTrace(string fullMessage)
        {
            if (string.IsNullOrEmpty(fullMessage))
                return null;

            string[] lines = fullMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length <= 1)
                return null;

            int stackStartIndex = -1;

            for (int i = 1; i < lines.Length; ++i)
            {
                string trimmedLine = lines[i].TrimStart();

                if (trimmedLine.StartsWith("at ") ||
                    trimmedLine.StartsWith("UnityEngine.") ||
                    trimmedLine.StartsWith("UnityEditor.") ||
                    trimmedLine.Contains("(at ") ||
                    (trimmedLine.Length > 0 && char.IsUpper(trimmedLine[0]) && trimmedLine.Contains('.')))
                {
                    stackStartIndex = i;
                    break;
                }
            }

            if (stackStartIndex > 0)
            {
                return string.Join("\n", lines.Skip(stackStartIndex));
            }

            return null;
        }

        #endregion
    }
}
