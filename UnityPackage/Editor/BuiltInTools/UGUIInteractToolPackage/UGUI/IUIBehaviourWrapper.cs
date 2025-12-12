using System.Collections.Generic;
using UnityEngine;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// UI 组件包装器接口
    /// 定义了 AI 与 UGUI 组件交互的标准协议
    /// </summary>
    public interface IUIBehaviourWrapper
    {
        /// <summary>
        /// 获取该组件支持的交互类型列表
        /// </summary>
        /// <returns>支持的交互类型数组</returns>
        InteractiveType[] GetInteractiveSchema();

        /// <summary>
        /// 获取组件的元数据信息
        /// </summary>
        /// <param name="target">目标组件</param>
        /// <param name="includeScreenshot">是否包含截图</param>
        /// <returns>元数据字典</returns>
        Dictionary<string, object> GetMetadata(Object target, bool includeScreenshot = false);

        /// <summary>
        /// 执行交互操作
        /// </summary>
        /// <param name="target">目标组件</param>
        /// <param name="type">交互类型</param>
        /// <param name="data">交互数据</param>
        /// <returns>交互结果</returns>
        object Interact(Object target, InteractiveType type, object data);
    }

    /// <summary>
    /// UI 组件包装器基类
    /// 提供通用的基础实现
    /// </summary>
    public abstract class UIBehaviourWrapper : IUIBehaviourWrapper
    {
        /// <summary>
        /// 执行交互操作
        /// </summary>
        public abstract object Interact(Object target, InteractiveType type, object data);

        /// <summary>
        /// 获取组件的元数据信息，默认实现返回空字典
        /// </summary>
        public virtual Dictionary<string, object> GetMetadata(Object target, bool includeScreenshot = false)
        {
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// 获取该组件支持的交互类型列表
        /// </summary>
        public abstract InteractiveType[] GetInteractiveSchema();
    }

    /// <summary>
    /// 泛型 UI 组件包装器基类
    /// </summary>
    /// <typeparam name="T">UI 组件类型</typeparam>
    public abstract class UIBehaviourWrapper<T> : UIBehaviourWrapper where T : Component
    {
    }
}

