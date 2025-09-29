using System;

namespace McpServerLib.Mcp.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class McpToolClassAttribute : Attribute
    {
        public string Category { get; set; }
        public string Description { get; set; }

        public McpToolClassAttribute()
        {
            Category = "General";
            Description = string.Empty;
        }
    }
}