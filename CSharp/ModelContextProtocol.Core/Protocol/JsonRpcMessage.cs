using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示模型上下文协议 (MCP) 中使用的任何 JSON-RPC 消息。
    /// </summary>
    /// <remarks>
    /// 此接口是 JSON-RPC 2.0 协议中所有消息类型的基础，
    /// MCP 使用这些消息，包括请求、响应、通知和错误。JSON-RPC 是一种无状态的、
    /// 轻量级的远程过程调用 (RPC) 协议，使用 JSON 作为其数据格式。
    /// </remarks>
    public abstract class JsonRpcMessage
    {
        /// <summary>
        /// 禁止外部继承，仅供派生类使用。
        /// </summary>
        protected JsonRpcMessage()
        {
        }

        /// <summary>
        /// 获取使用的 JSON-RPC 协议版本。
        /// </summary>
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /*
         * TODO: 原生库中存在 public JsonRpcMessageContext? Context { get; set; } 字段
         * 用于在运行时携带“与协议报文无关”的上下文信息
         * 不是 JSON-RPC/MCP 线上协议的一部分，而是给传输层、会话、管道过滤器或日志追踪用的便捷挂点
         */




    }
    
    /// <summary>
    /// 为 <see cref="JsonRpcMessage"/> 提供多态序列化/反序列化的转换器。
    /// </summary>
    /// <remarks>
    /// 判定规则（遵循 JSON-RPC 2.0）：
    /// - 同时包含 "method" 与 "id"：请求（<see cref="JsonRpcRequest"/>）
    /// - 仅包含 "method"：通知（<see cref="JsonRpcNotification"/>）
    /// - 包含 "id" 且有 "result"：成功响应（<see cref="JsonRpcResponse"/>）
    /// - 包含 "id" 且有 "error"：错误响应（<see cref="JsonRpcError"/>）
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class JsonRpcMessageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(JsonRpcMessage).IsAssignableFrom(objectType);
        }
        
        
    
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
    
            // 检查版本
            var version = (string)obj["jsonrpc"];
            if (version != "2.0")
                throw new JsonSerializationException("Invalid or missing jsonrpc version.");
            
            bool hasId = obj["id"] != null;
            bool hasMethod = obj["method"] != null;
            bool hasError = obj["error"] != null;
            bool hasResult = obj["result"] != null;
            
            if (hasId && !hasMethod)
            {
                if (hasError)
                {
                    var inst = new JsonRpcError();
                    using (var r = obj.CreateReader())
                    {
                        serializer.Populate(r, inst);
                    }
                    return inst;
                }
                if (hasResult)
                {
                    var inst = new JsonRpcResponse();
                    using (var r = obj.CreateReader())
                    {
                        serializer.Populate(r, inst);
                    }
                    return inst;
                }
    
                throw new JsonSerializationException("Response must have either result or error.");
            }
            if (hasMethod && !hasId)
            {
                var inst = new JsonRpcNotification();
                using (var r = obj.CreateReader())
                {
                    serializer.Populate(r, inst);
                }
                return inst;
            }
    
            if (hasMethod && hasId)
            {
                var inst = new JsonRpcRequest();
                using (var r = obj.CreateReader())
                {
                    serializer.Populate(r, inst);
                }
                return inst;
            }
            
            throw new JsonSerializationException("Invalid JSON-RPC message format.");
            
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
                throw new JsonSerializationException("Cannot serialize null JSON-RPC message.");

            // 构造不包含本转换器的安全序列化器，保留其它转换器（RequestId、ContentBlock 等）
            var safeSerializer = new JsonSerializer
            {
                NullValueHandling = serializer.NullValueHandling,
                ReferenceLoopHandling = serializer.ReferenceLoopHandling
            };
            foreach (var c in serializer.Converters)
            {
                if (!(c is JsonRpcMessageConverter))
                    safeSerializer.Converters.Add(c);
            }

            // 通过 JToken 中转，避免再次触发本转换器
            var token = JToken.FromObject(value, safeSerializer);
            token.WriteTo(writer);
        }
        
    }
}
