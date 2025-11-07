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
        public string category;
        public bool enabled;

        public McpTool(string name, string description, string category = "General")
        {
            this.name = name;
            this.description = description;
            this.category = category;
            enabled = true;
        }

        public override string ToString()
        {
            return $"{name} ({category}) - {description}";
        }
    }
}
