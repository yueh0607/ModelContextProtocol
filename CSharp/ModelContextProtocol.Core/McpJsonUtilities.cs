using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol
{

    public static partial class McpJsonUtilities
    {

        public static JsonSerializerSettings DefaultSettings { get; } = CreateDefaultSettings();


        public static readonly JsonSerializer DefaultSerializer = JsonSerializer.Create(DefaultSettings);


        public static readonly JObject DefaultMcpToolSchema = new JObject { ["type"] = "object" };


        private static JsonSerializerSettings CreateDefaultSettings()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };
            settings.Converters.Add(new StringEnumConverter());

            settings.Converters.Add(new Protocol.JsonRpcMessageConverter());

            return settings;
        }

        public static bool IsValidMcpToolSchema(JToken token)
        {
            if (token == null) return false;
            if (token.Type != JTokenType.Object) return false;

            var obj = (JObject)token;
            var typeProp = obj["type"];
            return typeProp != null
                   && typeProp.Type == JTokenType.String
                   && string.Equals(typeProp.Value<string>(), "object", StringComparison.Ordinal);
        }


        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, DefaultSettings);
        }

        public static T Deserialize<T>(string json)
        {
            T res = default;
            try
            {
                res = JsonConvert.DeserializeObject<T>(json, DefaultSettings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return res;
        }
    }
}