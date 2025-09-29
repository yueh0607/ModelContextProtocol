using System;
using JsonRpc.Models;

namespace JsonRpc.Utilities
{
    public static class JsonRpcValidator
    {
        public static bool IsValidRequest(JsonRpcRequest request, out string error)
        {
            error = null;

            if (request == null)
            {
                error = "Request cannot be null";
                return false;
            }

            if (request.JsonRpc != "2.0")
            {
                error = "JsonRpc version must be '2.0'";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.Method))
            {
                error = "Method name cannot be null or empty";
                return false;
            }

            if (request.Method.StartsWith("rpc.") && !IsAllowedRpcMethod(request.Method))
            {
                error = $"Method name '{request.Method}' is reserved";
                return false;
            }

            return true;
        }

        public static bool IsValidResponse(JsonRpcResponse response, out string error)
        {
            error = null;

            if (response == null)
            {
                error = "Response cannot be null";
                return false;
            }

            if (response.JsonRpc != "2.0")
            {
                error = "JsonRpc version must be '2.0'";
                return false;
            }

            if (response.Result != null && response.Error != null)
            {
                error = "Response cannot have both result and error";
                return false;
            }

            if (response.Result == null && response.Error == null)
            {
                error = "Response must have either result or error";
                return false;
            }

            if (response.Error != null && !IsValidError(response.Error, out var errorMessage))
            {
                error = $"Invalid error object: {errorMessage}";
                return false;
            }

            return true;
        }

        public static bool IsValidError(JsonRpcError error, out string errorMessage)
        {
            errorMessage = null;

            if (error == null)
            {
                errorMessage = "Error cannot be null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(error.Message))
            {
                errorMessage = "Error message cannot be null or empty";
                return false;
            }

            return true;
        }

        public static bool IsServerError(int errorCode)
        {
            return errorCode >= JsonRpcErrorCodes.ServerErrorEnd && errorCode <= JsonRpcErrorCodes.ServerErrorStart;
        }

        public static bool IsStandardError(int errorCode)
        {
            return errorCode == JsonRpcErrorCodes.ParseError ||
                   errorCode == JsonRpcErrorCodes.InvalidRequest ||
                   errorCode == JsonRpcErrorCodes.MethodNotFound ||
                   errorCode == JsonRpcErrorCodes.InvalidParams ||
                   errorCode == JsonRpcErrorCodes.InternalError;
        }

        private static bool IsAllowedRpcMethod(string method)
        {
            // Only some rpc. methods might be allowed in specific contexts
            // For now, we'll be strict and not allow any rpc. methods
            return false;
        }
    }
}