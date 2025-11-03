using System;
using System.Collections.Concurrent;
using UnityEditor;

namespace UnityAIStudio.McpServer.Tools
{
    /// <summary>
    /// Unity主线程调度器 - 用于从后台线程安全调用Unity API
    /// </summary>
    public static class UnityMainThreadScheduler
    {
        private static readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private static bool isInitialized = false;

        /// <summary>
        /// 初始化调度器（注册EditorApplication.update回调）
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;

            EditorApplication.update += Update;
            isInitialized = true;
        }

        /// <summary>
        /// 清理调度器
        /// </summary>
        public static void Cleanup()
        {
            if (!isInitialized) return;

            EditorApplication.update -= Update;
            isInitialized = false;

            // 清空队列
            while (mainThreadActions.TryDequeue(out _)) { }
        }

        /// <summary>
        /// 在主线程执行Action（异步，不等待结果）
        /// </summary>
        public static void Execute(Action action)
        {
            if (action == null) return;
            mainThreadActions.Enqueue(action);
        }

        /// <summary>
        /// 在主线程执行Func并等待结果
        /// </summary>
        public static System.Threading.Tasks.Task<T> ExecuteAsync<T>(Func<T> func)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<T>();

            Execute(() =>
            {
                try
                {
                    T result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// 在主线程执行Action并等待完成
        /// </summary>
        public static System.Threading.Tasks.Task ExecuteAsync(Action action)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            Execute(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        private static void Update()
        {
            // 每帧处理所有待执行的Action
            while (mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[UnityMainThreadScheduler] Error executing action: {ex}");
                }
            }
        }
    }
}
