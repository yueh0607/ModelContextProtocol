using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityEditor;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// 执行Unity编辑器菜单项
    /// </summary>
    [McpToolClass(Category = "Unity", Description = "Execute Unity Editor menu items")]
    public class ExecuteMenuItem
    {
        // 黑名单 - 防止执行破坏性菜单项
        private static readonly HashSet<string> _menuPathBlacklist = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "File/Quit",
            "File/Exit"
        };

        /// <summary>
        /// 执行Unity编辑器菜单项
        /// </summary>
        /// <param name="menuPath">菜单路径，例如 "File/Save Project"</param>
        [McpTool(
            Description = "Execute a Unity Editor menu item by its path (e.g., 'File/Save Project')",
            Category = "Unity"
        )]
        public async Task<CallToolResult> ExecuteMenu(
            [McpParameter("The menu item path to execute")]
            string menuPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(menuPath))
            {
                return McpUtils.Error("Required parameter 'menuPath' is missing or empty.");
            }

            if (_menuPathBlacklist.Contains(menuPath))
            {
                return McpUtils.Error($"Execution of menu item '{menuPath}' is blocked for safety reasons.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    bool executed = EditorApplication.ExecuteMenuItem(menuPath);
                    if (!executed)
                    {
                        return McpUtils.Error(
                            $"Failed to execute menu item '{menuPath}'. " +
                            "It might be invalid, disabled, or context-dependent.");
                    }

                    return McpUtils.Success(
                        $"Successfully executed menu item: '{menuPath}'. " +
                        "Check Unity console for any confirmation messages or errors.");
                }
                catch (Exception e)
                {
                    return McpUtils.Error($"Error executing menu item '{menuPath}': {e.Message}");
                }
            });
        }
    }
}
