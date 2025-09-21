using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using OpenCvSharp;
using Sdcb.PaddleOCR;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Local;

namespace TrOCR.Helper
{
    /// <summary>
    /// PaddleOCR2离线识别帮助类 (基于Sdcb.PaddleOCR)
    /// 采用单例模式和懒加载，支持资源回收
    /// </summary>
    public sealed class PaddleOCR2Helper : IDisposable
    {
        // Windows API 函数声明，用于内存优化
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process,
            IntPtr minimumWorkingSetSize, IntPtr maximumWorkingSetSize);

        private static PaddleOCR2Helper _instance;
        private static readonly object _lock = new object();

        private PaddleOcrAll _ocrEngine;
        private readonly Architecture _architecture;
        private bool _disposed = false;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static PaddleOCR2Helper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new PaddleOCR2Helper();
                        }
                    }
                }
                return _instance;
            }
        }

        private PaddleOCR2Helper()
        {
            _architecture = RuntimeInformation.OSArchitecture;
            
            if (_architecture != Architecture.X64)
                return;

            InitializeEngine();
        }

        private void InitializeEngine()
        {
            try
            {
                // 使用本地中文V3模型
                FullOcrModel model = LocalFullModels.ChineseV5;

                // 创建PaddleOCR引擎，使用MKLDNN设备以获得更好的CPU性能
                _ocrEngine = new PaddleOcrAll(model, PaddleDevice.Mkldnn())
                {
                    AllowRotateDetection = true,     // 允许识别有角度的文字
                    Enable180Classification = false  // 禁用180度分类以提升性能
                };

                // 优化检测参数
                _ocrEngine.Detector.MaxSize = 960;      // 设置最大检测尺寸
                _ocrEngine.Detector.UnclipRatio = 1.6f; // 设置文本框扩展比例
            }
            catch (Exception ex)
            {
                _ocrEngine = null;
                throw new Exception($"PaddleOCR2引擎初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 识别图像中的文字
        /// </summary>
        /// <param name="image">要识别的图像</param>
        /// <returns>识别结果文本</returns>
        public static string RecognizeText(Image image)
        {
            return Instance.Execute(image);
        }

        private string Execute(Image image)
        {
            try
            {
                if (_architecture != Architecture.X64)
                    return "***PaddleOCR2不支持32位系统，请使用64位系统***";
                
                if (_ocrEngine == null)
                    return "***PaddleOCR2引擎未初始化***";

                if (image == null)
                    return "***图像为空***";

                // 将System.Drawing.Image转换为OpenCvSharp.Mat
                using (Mat src = ImageToMat(image))
                {
                    // 执行OCR识别
                    PaddleOcrResult result = _ocrEngine.Run(src);

                    if (result?.Regions == null || result.Regions.Length == 0)
                        return "***该区域未发现文本***";

                    // 构建识别结果
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
            catch (Exception ex)
            {
                return $"***PaddleOCR2识别失败: {ex.Message}***";
            }
        }

        /// <summary>
        /// 将System.Drawing.Image转换为OpenCvSharp.Mat
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>OpenCvSharp.Mat对象</returns>
        private Mat ImageToMat(Image image)
        {
            using (var ms = new MemoryStream())
            {
                // 将图像保存为PNG格式的字节数组
                image.Save(ms, ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                
                // 使用OpenCvSharp解码图像
                return Cv2.ImDecode(imageBytes, ImreadModes.Color);
            }
        }

        /// <summary>
        /// 获取详细的OCR识别结果（包含位置信息）
        /// </summary>
        /// <param name="image">要识别的图像</param>
        /// <returns>详细识别结果</returns>
        public static PaddleOcrResult GetDetailedResult(Image image)
        {
            return Instance.ExecuteDetailed(image);
        }

        private PaddleOcrResult ExecuteDetailed(Image image)
        {
            try
            {
                if (_architecture != Architecture.X64 || _ocrEngine == null || image == null)
                    return null;

                using (Mat src = ImageToMat(image))
                {
                    return _ocrEngine.Run(src);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 检查是否支持PaddleOCR2
        /// </summary>
        /// <returns>是否支持</returns>
        public static bool IsSupported()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64;
        }

        /// <summary>
        /// 重置引擎实例
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    _instance.Dispose();
                    _instance = null;

                    // 强制垃圾回收以释放内存
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    // Windows平台内存优化
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, 
                            (IntPtr)(-1), (IntPtr)(-1));
                    }
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _ocrEngine?.Dispose();
                _ocrEngine = null;
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~PaddleOCR2Helper()
        {
            Dispose();
        }
    }
}