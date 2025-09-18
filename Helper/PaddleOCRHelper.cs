using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent; // 引入线程安全集合
using PaddleOCRSharp;

namespace TrOCR.Helper
{
    /// <summary>
    /// PaddleOCR离线识别帮助类
    /// 采用带有专用线程的单例模式，确保引擎的创建、使用和销毁都在同一线程，避免跨线程资源泄漏。
    /// </summary>
    public sealed class PaddleOCRHelper : IDisposable
    {
        // 懒加载单例
        private static readonly Lazy<PaddleOCRHelper> _instance = new Lazy<PaddleOCRHelper>(() => new PaddleOCRHelper());
        public static PaddleOCRHelper Instance => _instance.Value;

        // 任务队列，用于从其他线程向专用线程传递识别任务
        private readonly BlockingCollection<OcrTask> _taskQueue = new BlockingCollection<OcrTask>();
        private readonly Thread _workerThread; // 专用的后台工作线程
        private bool _disposed = false;

        // 私有构造函数
        private PaddleOCRHelper()
        {
            // 仅支持64位
            if (RuntimeInformation.OSArchitecture != Architecture.X64)
                return;

            // 创建并启动专用线程
            _workerThread = new Thread(ProcessQueue)
            {
                IsBackground = true,
                Name = "PaddleOCRWorker"
            };
            _workerThread.Start();
        }

        // 专用线程的工作循环
        private void ProcessQueue()
        {
            PaddleOCREngine engine = null;

            try
            {
                // 只要任务队列没有被标记为“完成”，就一直等待新任务
                foreach (var task in _taskQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        // 1. 初始化引擎 (如果尚未初始化)
                        // 所有操作都在这个线程内，非常安全
                        if (engine == null)
                        {
                            string rootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "paddleOCR", "win_x64");
                            string modelPath = Path.Combine(rootDir, "inference");
                            string configJsonPath = Path.Combine(modelPath, "PaddleOCR.config.json");
                            string ocrParamsJson = File.Exists(configJsonPath) ? File.ReadAllText(configJsonPath) : "";
                            engine = new PaddleOCREngine(null, ocrParamsJson);
                        }

                        // 2. 执行识别
                        var ocrResult = engine.DetectText(task.ImageBytes);

                        // 3. 处理结果并返回
                        if (ocrResult?.TextBlocks == null || ocrResult.TextBlocks.Count == 0)
                        {
                            task.CompletionSource.TrySetResult("***该区域未发现文本***");
                            continue;
                        }

                        var sb = new StringBuilder();
                        foreach (var textBlock in ocrResult.TextBlocks)
                        {
                            if (!string.IsNullOrWhiteSpace(textBlock.Text))
                                sb.AppendLine(textBlock.Text);
                        }
                        string result = sb.ToString().Trim();
                        task.CompletionSource.TrySetResult(string.IsNullOrEmpty(result) ? "***该区域未发现文本***" : result);
                    }
                    catch (Exception ex)
                    {
                        // 如果任务执行失败，通知调用者
                        task.CompletionSource.TrySetException(new Exception($"PaddleOCR识别失败: {ex.Message}"));
                    }
                }
            }
            finally
            {
                // 队列结束后，确保引擎被销毁（仍然在这个专用线程内）
                engine?.Dispose();
            }
        }

        // 公共的识别方法，现在是异步的
        public Task<string> RecognizeTextAsync(Image image)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PaddleOCRHelper));

            if (RuntimeInformation.OSArchitecture != Architecture.X64)
                return Task.FromResult("***PaddleOCR不支持32位系统，请使用64位系统***");

            if (image == null)
                return Task.FromResult("***图像为空***");

            // 将图片转换为字节数组
            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                imageBytes = ms.ToArray();
            }
            
            // 创建一个任务，并将其放入队列
            var tcs = new TaskCompletionSource<string>();
            _taskQueue.Add(new OcrTask(imageBytes, tcs));

            // 返回这个任务，调用者可以等待(await)它的结果
            return tcs.Task;
        }

        // 销毁单例的方法
        public void Dispose()
        {
            if (!_disposed)
            {
                // 标记队列为“完成”，这将使专用线程的循环结束
                _taskQueue.CompleteAdding();
                // 等待专用线程执行完毕并退出
                _workerThread?.Join();
                _taskQueue.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        // 静态重置方法，用于外部调用
        public static void Reset()
        {
            if (_instance.IsValueCreated)
            {
                Instance.Dispose();
                // 注意：因为Lazy<T>的实例一旦创建就无法重置，所以这里实际上是销毁了实例，
                // 但应用生命周期内无法再创建新的了。这要求 Reset只在程序退出时调用。
                // 如果需要在运行时重建，需要更复杂的单例模式。
            }
        }

        // 内部类，用于在队列中传递任务数据
        private class OcrTask
        {
            public byte[] ImageBytes { get; }
            public TaskCompletionSource<string> CompletionSource { get; }

            public OcrTask(byte[] imageBytes, TaskCompletionSource<string> tcs)
            {
                ImageBytes = imageBytes;
                CompletionSource = tcs;
            }
        }
    }
}