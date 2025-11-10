using System;
using System.Collections.Generic;

namespace UnityAIStudio.McpServer.Editor.Window.Models
{
    /// <summary>
    /// Represents a tool package (group of tools from a single class) for the MCP Server
    /// </summary>
    [Serializable]
    public class McpToolPackage
    {
        /// <summary>
        /// The fully qualified name of the tool class (e.g., "MyNamespace.MyToolClass")
        /// </summary>
        public string className;

        /// <summary>
        /// Display name of the tool package (defaults to class name without namespace)
        /// </summary>
        public string displayName;

        /// <summary>
        /// Category of the tool package
        /// </summary>
        public string category;

        /// <summary>
        /// Description of the tool package
        /// </summary>
        public string description;

        /// <summary>
        /// Whether this tool package is enabled
        /// </summary>
        public bool enabled;

        /// <summary>
        /// Number of tools in this package
        /// </summary>
        public int toolCount;

        /// <summary>
        /// List of tool names in this package (for display purposes)
        /// </summary>
        public List<string> toolNames;

        public McpToolPackage(string className, string displayName, string category = "General", string description = "", int toolCount = 0)
        {
            this.className = className;
            this.displayName = displayName;
            this.category = category;
            this.description = description;
            this.enabled = true; // Default to enabled
            this.toolCount = toolCount;
            this.toolNames = new List<string>();
        }

        /// <summary>
        /// Gets a unique key for EditorPrefs storage
        /// </summary>
        public string GetPrefsKey()
        {
            return $"McpServer_ToolPackage_{className}_Enabled";
        }
    }
}
