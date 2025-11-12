using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools.Attributes;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// BasicUnityToolPackage - Asset Ping 部分
    /// </summary>
    public partial class BasicUnityToolPackage
    {
        /// <summary>
        /// Ping 并选中一个资产
        /// </summary>
        [McpTool(
            Description = "Ping and select an asset in the Project window. This highlights the asset and makes it the active selection.",
            Category = "Basic Unity Tools"
        )]
        public async Task<CallToolResult> PingAsset(
            [McpParameter("Path to the asset file (supports fuzzy path matching)")]
            [FuzzyPathProcessor(KeepOriginalIfNotFound = false)]
            string assetPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return McpUtils.Error("Required parameter 'assetPath' is missing or empty.");
            }

            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                try
                {
                    // 加载资产
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                    if (asset == null)
                    {
                        return McpUtils.Error($"Asset not found at path: {assetPath}\n" +
                            $"Please check the path is correct.");
                    }

                    // Ping 并选中资产
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;

                    return McpUtils.Success($"Asset pinged and selected:\n" +
                        $"Path: {assetPath}\n" +
                        $"Type: {asset.GetType().Name}\n" +
                        $"Name: {asset.name}");
                }
                catch (System.Exception ex)
                {
                    return McpUtils.Error($"Failed to ping asset: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }
    }
}
