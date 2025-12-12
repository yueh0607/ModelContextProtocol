namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// UGUI 交互类型枚举
    /// 定义 UI 组件支持的交互方式
    /// </summary>
    public enum InteractiveType
    {
        /// <summary>无交互</summary>
        None,
        /// <summary>点击交互</summary>
        Click,
        /// <summary>拖拽交互</summary>
        Drag,
        /// <summary>放下交互</summary>
        Drop,
        /// <summary>滚动交互</summary>
        Scroll,
        /// <summary>缩放交互</summary>
        Zoom,
        /// <summary>旋转交互</summary>
        Rotate,
        /// <summary>缩放交互</summary>
        Scale,
        /// <summary>输入交互</summary>
        Input,
        /// <summary>悬停交互</summary>
        Hover
    }
}

