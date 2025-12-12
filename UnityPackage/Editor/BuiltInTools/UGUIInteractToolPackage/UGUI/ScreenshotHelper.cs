using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityAIStudio.McpServer.Tools.UGUI
{
    /// <summary>
    /// 截图助手 - 支持可靠的Game View截图
    /// </summary>
    public static class ScreenshotHelper
    {
        private static bool _isCapturing = false;
        private static CoroutineRunner _coroutineRunner;

        static ScreenshotHelper()
        {
            EditorApplication.quitting += CleanupCoroutineRunner;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void CleanupCoroutineRunner()
        {
            if (_coroutineRunner != null)
            {
                _coroutineRunner.SafeDestroy();
                _coroutineRunner = null;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                CleanupCoroutineRunner();
            }
        }

        private static void EnsureCoroutineRunner()
        {
            if (_coroutineRunner == null)
            {
                var go = new GameObject("ScreenshotCoroutineRunner")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _coroutineRunner = go.AddComponent<CoroutineRunner>();
            }
        }

        // 截图配置（可调整）
        private static float _scaleFactor = 0.5f;  // 默认缩放到50%
        private static int _jpegQuality = 50;       // 默认JPEG质量50%

        /// <summary>
        /// 异步截图，等待帧结尾
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        /// <param name="scaleFactor">缩放因子 (0.1-1.0)，默认0.5表示缩放到50%</param>
        /// <param name="jpegQuality">JPEG质量 (1-100)，默认50</param>
        public static void CaptureGameViewAsync(Action<ScreenshotResult> onComplete, float scaleFactor = 0.5f, int jpegQuality = 50)
        {
            if (_isCapturing)
            {
                onComplete?.Invoke(ScreenshotResult.Error("截图正在进行中，请稍候"));
                return;
            }

            _isCapturing = true;
            _scaleFactor = Mathf.Clamp(scaleFactor, 0.1f, 1f);
            _jpegQuality = Mathf.Clamp(jpegQuality, 1, 100);

            try
            {
                EnsureCoroutineRunner();
                _coroutineRunner.StartCoroutine(CaptureAfterEndOfFrame(onComplete));
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(ScreenshotResult.Error($"启动截图失败: {ex.Message}"));
                _isCapturing = false;
            }
        }

        /// <summary>
        /// 同步截图（立即执行）
        /// </summary>
        public static ScreenshotResult CaptureGameViewSync()
        {
            RenderTexture tempRT = null;

            try
            {
                var captureWidth = Screen.width;
                var captureHeight = Screen.height;

                if (captureWidth <= 0 || captureHeight <= 0)
                {
                    captureWidth = 1920;
                    captureHeight = 1080;
                }

                // 创建RenderTexture
                tempRT = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                tempRT.Create();

                // 使用Unity的屏幕截图功能
                ScreenCapture.CaptureScreenshotIntoRenderTexture(tempRT);

                // 读取像素数据
                return ReadPixelsFromRenderTexture(tempRT, true, _scaleFactor, _jpegQuality);
            }
            catch (Exception ex)
            {
                return ScreenshotResult.Error($"截图失败: {ex.Message}");
            }
            finally
            {
                if (tempRT != null)
                {
                    tempRT.Release();
                    Object.DestroyImmediate(tempRT);
                }
            }
        }

        private static IEnumerator CaptureAfterEndOfFrame(Action<ScreenshotResult> onComplete)
        {
            yield return new WaitForEndOfFrame();

            try
            {
                var result = CaptureGameViewSync();
                onComplete?.Invoke(result);
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(ScreenshotResult.Error($"截图失败: {ex.Message}"));
            }
            finally
            {
                _isCapturing = false;
            }
        }

        private static ScreenshotResult ReadPixelsFromRenderTexture(RenderTexture renderTexture, bool needFlip, float scaleFactor, int jpegQuality)
        {
            Texture2D texture2D = null;
            Texture2D scaledTexture = null;

            try
            {
                RenderTexture.active = renderTexture;

                texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture2D.Apply();

                RenderTexture.active = null;

                // 垂直翻转（ScreenCapture API通常需要翻转）
                if (needFlip)
                {
                    texture2D = FlipTextureVertically(texture2D);
                }

                // 计算缩放后的尺寸
                int finalWidth = Mathf.RoundToInt(texture2D.width * scaleFactor);
                int finalHeight = Mathf.RoundToInt(texture2D.height * scaleFactor);

                // 确保最小尺寸
                finalWidth = Mathf.Max(finalWidth, 320);
                finalHeight = Mathf.Max(finalHeight, 180);

                // 缩放图片
                if (scaleFactor < 1f)
                {
                    scaledTexture = ScaleTexture(texture2D, finalWidth, finalHeight);
                    Object.DestroyImmediate(texture2D);
                    texture2D = scaledTexture;
                    scaledTexture = null;
                }

                // 编码为JPEG（比PNG小很多）
                byte[] imageBytes = texture2D.EncodeToJPG(jpegQuality);

                int originalWidth = renderTexture.width;
                int originalHeight = renderTexture.height;

                Object.DestroyImmediate(texture2D);

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return ScreenshotResult.Error("JPEG编码失败");
                }

                string base64Image = Convert.ToBase64String(imageBytes);

                return new ScreenshotResult
                {
                    Success = true,
                    Base64Data = base64Image,
                    Width = finalWidth,
                    Height = finalHeight,
                    OriginalWidth = originalWidth,
                    OriginalHeight = originalHeight,
                    Format = "jpeg",
                    CompressionInfo = $"scale:{scaleFactor:P0} quality:{jpegQuality}% size:{imageBytes.Length / 1024}KB",
                    CaptureMethod = "ScreenCapture API",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                return ScreenshotResult.Error($"读取像素失败: {ex.Message}");
            }
            finally
            {
                if (texture2D != null) Object.DestroyImmediate(texture2D);
                if (scaledTexture != null) Object.DestroyImmediate(scaledTexture);
            }
        }

        /// <summary>
        /// 缩放纹理
        /// </summary>
        private static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = null;
            try
            {
                rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.filterMode = FilterMode.Bilinear;

                RenderTexture.active = rt;
                Graphics.Blit(source, rt);

                var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                result.Apply();

                return result;
            }
            finally
            {
                RenderTexture.active = null;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static Texture2D FlipTextureVertically(Texture2D original)
        {
            try
            {
                int width = original.width;
                int height = original.height;

                var pixels = original.GetPixels();
                var flippedPixels = new Color[pixels.Length];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        flippedPixels[x + (height - 1 - y) * width] = pixels[x + y * width];
                    }
                }

                var flippedTexture = new Texture2D(width, height, original.format, false);
                flippedTexture.SetPixels(flippedPixels);
                flippedTexture.Apply();

                Object.DestroyImmediate(original);

                return flippedTexture;
            }
            catch
            {
                return original;
            }
        }
    }

    /// <summary>
    /// 截图结果
    /// </summary>
    public class ScreenshotResult
    {
        public bool Success { get; set; }
        public string Base64Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
        public string Format { get; set; } = "jpeg";
        public string CompressionInfo { get; set; }
        public string CaptureMethod { get; set; }
        public string Timestamp { get; set; }
        public string ErrorMessage { get; set; }

        public static ScreenshotResult Error(string message)
        {
            return new ScreenshotResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }

    /// <summary>
    /// 协程运行器 - 用于在Editor中运行协程
    /// </summary>
    internal class CoroutineRunner : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void SafeDestroy()
        {
            StopAllCoroutines();
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}

