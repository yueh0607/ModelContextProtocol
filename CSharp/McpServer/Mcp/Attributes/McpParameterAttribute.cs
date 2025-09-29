using System;

namespace McpServerLib.Mcp.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class McpParameterAttribute : Attribute
    {
        public string Description { get; set; }
        public bool Required { get; set; }

        public McpParameterAttribute(string description = "")
        {
            Description = description;
            Required = true;
        }
    }
}