using System;

namespace McpServerLib.Mcp.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class McpToolAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; set; }

        public McpToolAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = string.Empty;
        }
    }
}