using UnityEngine;
using UnityMCP.Examples;

namespace UnityMCP
{
    /// <summary>
    /// 示例：如何设置Unity MCP服务器
    /// 将此脚本添加到场景中的GameObject，它会自动注册示例工具和资源
    /// </summary>
    public class UnityMcpServerExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("UnityMcpServer组件引用，如果为空则自动查找")]
        [SerializeField] private UnityMcpServer mcpServer;

        private void Awake()
        {
            // 如果没有指定，尝试在同一GameObject上查找
            if (mcpServer == null)
            {
                mcpServer = GetComponent<UnityMcpServer>();
            }

            // 如果还是没有，尝试在场景中查找
            if (mcpServer == null)
            {
                mcpServer = FindObjectOfType<UnityMcpServer>();
            }

            if (mcpServer == null)
            {
                Debug.LogError("UnityMcpServer component not found! Please add it to the scene.");
                return;
            }

            RegisterToolsAndResources();
        }

        private void RegisterToolsAndResources()
        {
            // 注册示例工具
            mcpServer.RegisterTool(new UnityLogTool());
            Debug.Log("Registered tool: unity_log");

            mcpServer.RegisterTool(new UnityGameObjectTool());
            Debug.Log("Registered tool: unity_find_gameobject");

            mcpServer.RegisterTool(new UnitySceneInfoTool());
            Debug.Log("Registered tool: unity_scene_info");

            // 注册示例资源
            mcpServer.RegisterResource(new UnitySceneHierarchyResource());
            Debug.Log("Registered resource: unity://scene/hierarchy");

            Debug.Log("All example tools and resources registered successfully!");
        }

        private void Update()
        {
            // 更新主线程调度器
            UnityMainThreadDispatcher.Update();
        }
    }
}
