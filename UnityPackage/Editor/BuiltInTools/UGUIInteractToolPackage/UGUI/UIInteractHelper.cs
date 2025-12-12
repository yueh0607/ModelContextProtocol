using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// UGUI 交互辅助类
    /// 管理所有 UI 组件的包装器注册和交互分发
    /// </summary>
    public static class UIInteractHelper
    {
        /// <summary>
        /// 已注册的 UI 组件包装器字典
        /// </summary>
        private static readonly Dictionary<Type, IUIBehaviourWrapper> Wrappers = new Dictionary<Type, IUIBehaviourWrapper>()
        {
            { typeof(Button), new ButtonWrapper() },
            { typeof(InputField), new InputFieldWrapper() },
            { typeof(Toggle), new ToggleWrapper() },
            { typeof(ScrollRect), new ScrollRectWrapper() },
            { typeof(Slider), new SliderWrapper() },
            { typeof(Dropdown), new DropdownWrapper() },
        };

        /// <summary>
        /// 可交互对象的描述结构
        /// </summary>
        public class InteractableSchema
        {
            /// <summary>组件类型名称</summary>
            public string TargetType;
            /// <summary>目标对象名称</summary>
            public string TargetName;
            /// <summary>目标对象ID（InstanceID）</summary>
            public string TargetId;
            /// <summary>支持的交互类型列表</summary>
            public string[] SupportedInteractions;
            /// <summary>组件描述</summary>
            public string Description;
            /// <summary>组件元数据</summary>
            public Dictionary<string, object> Metadata;

            public InteractableSchema()
            {
                Metadata = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 获取指定类型的包装器
        /// </summary>
        /// <param name="type">UI组件类型</param>
        /// <returns>包装器实例</returns>
        public static IUIBehaviourWrapper GetWrapper(Type type)
        {
            if (Wrappers.TryGetValue(type, out var wrapper))
            {
                return wrapper;
            }

            // 尝试查找父类型的包装器
            foreach (var kvp in Wrappers)
            {
                if (kvp.Key.IsAssignableFrom(type))
                {
                    Debug.Log($"[UIInteractHelper] 使用父类型 {kvp.Key.Name} 的包装器处理 {type.Name}");
                    return kvp.Value;
                }
            }

            Debug.LogError($"[UIInteractHelper] 找不到类型的包装器: {type}");
            return null;
        }

        /// <summary>
        /// 执行 UI 交互
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="type">交互类型</param>
        /// <param name="data">交互数据</param>
        /// <returns>交互结果</returns>
        public static object Interact(UnityEngine.Object target, InteractiveType type, object data)
        {
            try
            {
                var wrapper = GetWrapper(target.GetType());
                if (wrapper == null)
                {
                    return new { success = false, error = $"不支持的UI类型: {target.GetType().Name}" };
                }

                // 检查是否支持此交互类型
                var supportedTypes = wrapper.GetInteractiveSchema();
                bool isSupported = supportedTypes.Contains(type);

                if (!isSupported)
                {
                    return new { success = false, error = $"对象 {target.name} 不支持交互类型: {type}" };
                }

                return wrapper.Interact(target, type, data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIInteractHelper] 交互执行失败: {ex.Message}\n{ex.StackTrace}");
                return new { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// 获取所有已注册的 UI 类型
        /// </summary>
        /// <returns>UI 类型列表</returns>
        public static Type[] GetSupportedUITypes()
        {
            return Wrappers.Keys.ToArray();
        }

        /// <summary>
        /// 注册自定义的 UI 组件包装器
        /// </summary>
        /// <param name="componentType">UI 组件类型</param>
        /// <param name="wrapper">包装器实例</param>
        public static void RegisterWrapper(Type componentType, IUIBehaviourWrapper wrapper)
        {
            Wrappers[componentType] = wrapper;
            Debug.Log($"[UIInteractHelper] 注册包装器: {componentType.Name}");
        }
    }
}

