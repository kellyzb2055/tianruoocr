using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TrOCR.Helper; 
using OpenCvSharp;
using Sdcb.PaddleOCR;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR.Models;
using System.Diagnostics;
//支持ppocrv5，支持64位，不支持32位，不需要 CPU 支持 AVX指令集，cpu不支持avx的使用此接口
namespace TrOCR.Helper
{
    /// <summary>
    /// PaddleOCR2离线识别帮助类 (基于Sdcb.PaddleOCR)
    /// 采用单例模式和懒加载，支持从自定义本地路径加载模型。
    /// </summary>
    public sealed class PaddleOCR2Helper : IDisposable
    {
        // --- 1. Lazy<T> ，实现简洁的线程安全单例 ---
        private static Lazy<PaddleOCR2Helper> _lazyInstance =
            new Lazy<PaddleOCR2Helper>(() => new PaddleOCR2Helper(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static PaddleOCR2Helper Instance => _lazyInstance.Value;
        
        private static readonly object _resetLock = new object();
        private readonly PaddleOcrAll _ocrEngine;
        private bool _disposed = false;

        // --- 2. 构造函数现在负责所有初始化工作 ---
        private PaddleOCR2Helper()
        {
            // 检查系统架构
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
                throw new NotSupportedException("***PaddleOCR2仅支持64位系统, 不支持32位系统***");

            try
            {
                // 1. 定义模型文件所在的根目录
                string modelBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PaddleOCR2_data","models");

                // 2. 分别构建每个模型组件的完整路径
                string detModelPath = Path.Combine(modelBasePath, "PP-OCRv4_mobile_det_infer");
                string clsModelPath = Path.Combine(modelBasePath, "ch_ppocr_mobile_v2.0_cls_infer");
                string recModelPath = Path.Combine(modelBasePath, "PP-OCRv4_mobile_rec_infer");
                string keysPath = Path.Combine(modelBasePath, "ppocr_keys.txt");

                // 3. 使用 FromDirectory 静态方法分别创建每个模型对象
                //    这些方法正是您在源码中看到的，它们是推荐的使用方式
                DetectionModel detModel = DetectionModel.FromDirectory(detModelPath,ModelVersion.V5);
                ClassificationModel clsModel = ClassificationModel.FromDirectory(clsModelPath,ModelVersion.V2);
                // 识别模型需要额外的字典文件路径
                RecognizationModel recModel = RecognizationModel.FromDirectory(recModelPath, keysPath,ModelVersion.V5);

                // 4. 将分别创建的模型对象组合成一个完整的 FullOcrModel
                FullOcrModel customModel = new FullOcrModel(detModel, clsModel, recModel);

                // 5. 使用这个自定义的、组合好的模型来初始化引擎
                //    这里的 _ocrEngine 就是您 PaddleOCR2Helper 类中的字段
                _ocrEngine = new PaddleOcrAll(customModel, PaddleDevice.Blas())
                {
                    AllowRotateDetection = true,
                    Enable180Classification = false
                };
                //_ocrEngine.Detector.MaxSize = 960;
            }
            catch (Exception ex)
            {
                // 1. 手动拼接一个更详细、更友好的错误消息
                string detailedMessage = $"从自定义路径加载模型失败，请检查路径和模型文件是否完整且版本匹配。\n根本原因: {ex.Message}";

                // 2. 抛出新异常，使用我们刚刚拼接好的详细消息，并依然保留完整的 ex 作为 InnerException
                throw new Exception(detailedMessage, ex);
            }
        }

        // --- 3. 公共方法恢复同步，调用更简单 ---
        /// <summary>
        /// 识别图像中的文字
        /// </summary>
        public static string RecognizeText(Image image)
        {
            try
            {
                return Instance.Execute(image);
            }
            catch (Exception ex)
            {
                Reset(); // 失败后重置，以便下次重试
                return $"***PaddleOCR2识别失败: {ex.Message}***";
            }
        }

        private string Execute(Image image)
        {
            if (_ocrEngine == null) return "***PaddleOCR2引擎未初始化***";
            if (image == null) return "***图像为空***";

            using (Mat src = ImageToMat(image))
            {
                PaddleOcrResult result = _ocrEngine.Run(src);
                Debug.WriteLine($"paddleOCR2识别结果: {result}");
                if (result?.Regions == null || result.Regions.Length == 0)
                    return "***该区域未发现文本***";

                var sb = new StringBuilder();
                foreach (var region in result.Regions)
                {
                    if (!string.IsNullOrWhiteSpace(region.Text))
                    {
                        sb.AppendLine(region.Text);
                    }
                }
                string finalResult = sb.ToString().Trim();
                return string.IsNullOrEmpty(finalResult) ? "***该区域未发现文本***" : finalResult;
            }
        }
        
        // --- 4. Reset 方法适配 Lazy<T> ---
        public static void Reset()
        {
            lock (_resetLock)
            {
                if (_lazyInstance.IsValueCreated)
                {
                    try { _lazyInstance.Value.Dispose(); }
                    catch { /* 忽略在失败实例上调用Dispose时可能发生的异常 */ }
                }
                // 关键：创建全新的Lazy实例以备下次使用
                _lazyInstance = new Lazy<PaddleOCR2Helper>(() => new PaddleOCR2Helper(), LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }

        // --- 其他辅助方法和Dispose模式 ---
        private Mat ImageToMat(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                return Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
            }
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _ocrEngine?.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
        ~PaddleOCR2Helper() => Dispose();
    }
}