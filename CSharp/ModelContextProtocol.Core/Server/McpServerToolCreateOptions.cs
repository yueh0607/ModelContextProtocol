using System;
using System.Collections.Generic;
using System.ComponentModel;
using MapleModelContextProtocol.Protocol;
using Newtonsoft.Json;

namespace MapleModelContextProtocol.Server
{

    public sealed class McpServerToolCreateOptions
    {
        public IServiceProvider Services { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Title { get; set; }

        public bool Destructive { get; set; }

        public bool Idempotent { get; set; }

        public bool OpenWorld { get; set; }

        public bool ReadOnly { get; set; }

        public bool UseStructuredContent { get; set; }

        public JsonSerializerSettings SerializerOptions { get; set; }

        public IReadOnlyList<object> Metadata { get; set; }

        public IList<Icon> Icons { get; set; }

        internal McpServerToolCreateOptions Clone() =>
            new McpServerToolCreateOptions
            {
                Services = Services,
                Name = Name,
                Description = Description,
                Title = Title,
                Destructive = Destructive,
                Idempotent = Idempotent,
                OpenWorld = OpenWorld,
                ReadOnly = ReadOnly,
                UseStructuredContent = UseStructuredContent,
                SerializerOptions = SerializerOptions,
                // SchemaCreateOptions = SchemaCreateOptions,
                Metadata = Metadata,
                Icons = Icons,
            };
    }
}