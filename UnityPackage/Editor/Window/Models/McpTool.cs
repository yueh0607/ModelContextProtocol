using System;

namespace UnityAIStudio.McpServer.Models
{
    /// <summary>
    /// MCP Tool information
    /// </summary>
    [Serializable]
    public class McpTool
    {
        public string name;
        public string description;
        public bool enabled;

        public McpTool(string name, string description)
        {
            this.name = name;
            this.description = description;
            enabled = true;
        }

        public override string ToString()
        {
            return $"{name} - {description}";
        }
    }
}
