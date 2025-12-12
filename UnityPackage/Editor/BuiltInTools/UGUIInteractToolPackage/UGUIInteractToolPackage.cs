using UnityAIStudio.McpServer.Tools.Attributes;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// UGUI 交互工具包
    /// 提供 Unity 运行时 UI 的感知与交互能力
    /// 核心工具: See(查看) → Click/Input/Scroll(交互) → See(验证)
    /// </summary>
    [McpToolClass(Category = "UGUI Interaction", Description = "Tools for interacting with Unity UGUI at runtime. Enables AI to see, click, input and scroll UI elements.")]
    public partial class UGUIInteractToolPackage
    {
    }
}

