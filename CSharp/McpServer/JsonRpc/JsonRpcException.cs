using System;
using JsonRpc.Models;

namespace JsonRpc
{
    public class JsonRpcException : Exception
    {
        public JsonRpcError Error { get; }

        public JsonRpcException(JsonRpcError error) : base(error.Message)
        {
            Error = error;
        }

        public JsonRpcException(int code, string message, object data = null) : base(message)
        {
            Error = new JsonRpcError
            {
                Code = code,
                Message = message,
                Data = data
            };
        }

        public static JsonRpcException ParseError(string message = "Parse error", object data = null)
        {
            return new JsonRpcException(JsonRpcErrorCodes.ParseError, message, data);
        }

        public static JsonRpcException InvalidRequest(string message = "Invalid Request", object data = null)
        {
            return new JsonRpcException(JsonRpcErrorCodes.InvalidRequest, message, data);
        }

        public static JsonRpcException MethodNotFound(string methodName = null)
        {
            var message = methodName != null ? $"Method '{methodName}' not found" : "Method not found";
            return new JsonRpcException(JsonRpcErrorCodes.MethodNotFound, message);
        }

        public static JsonRpcException InvalidParams(string message = "Invalid params", object data = null)
        {
            return new JsonRpcException(JsonRpcErrorCodes.InvalidParams, message, data);
        }

        public static JsonRpcException InternalError(string message = "Internal error", object data = null)
        {
            return new JsonRpcException(JsonRpcErrorCodes.InternalError, message, data);
        }

        public static JsonRpcException ServerError(int code, string message, object data = null)
        {
            if (code < JsonRpcErrorCodes.ServerErrorEnd || code > JsonRpcErrorCodes.ServerErrorStart)
            {
                throw new ArgumentException($"Server error code must be between {JsonRpcErrorCodes.ServerErrorEnd} and {JsonRpcErrorCodes.ServerErrorStart}");
            }

            return new JsonRpcException(code, message, data);
        }
    }
}