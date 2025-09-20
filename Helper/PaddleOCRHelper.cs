using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using PaddleOCRSharp;

namespace TrOCR.Helper
{
    /// <summary>
    /// PaddleOCR离线识别帮助类
    /// 采用单例模式和懒加载，支持资源回收
    /// </summary>
    public sealed class PaddleOCRHelper : IDisposable
    {
        private static readonly Lazy<PaddleOCRHelper> _instance = new Lazy<PaddleOCRHelper>(() => new PaddleOCRHelper());
        private PaddleOCREngine _engine;
        private readonly Architecture _architecture;
        private bool _disposed = false;

        public static PaddleOCRHelper Instance => _instance.Value;

        private PaddleOCRHelper()
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
                // 1. 获取 paddleOCR 文件夹的根路径
                string rootDir =Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "paddleOCR","win_x64");

                // 2. 组合出 inference 模型文件夹的完整路径
                string modelPath = Path.Combine(rootDir, "inference");

                // 3. 创建模型配置对象，并明确指定每个模型文件的路径
                // 注意：下面的路径请根据您实际的模型版本调整
                //  OCRModelConfig config = null;
                // OCRModelConfig config = new OCRModelConfig();
                // config.det_infer = Path.Combine(modelPath, "PP-OCRv5_mobile_det_infer");
                // config.cls_infer = Path.Combine(modelPath, "ch_ppocr_mobile_v5.0_cls_infer");
                // config.rec_infer = Path.Combine(modelPath, "PP-OCRv5_mobile_rec_infer");
                // config.keys = Path.Combine(modelPath, "ppocr_keys.txt");
               
                 // 定义参数配置文件的路径
                // string configJsonPath = Path.Combine(modelPath, "PaddleOCR.config.json");

                // string ocrParamsJson = ""; // 如果文件不存在或为空，则使用引擎内部的默认参数

                // if (File.Exists(configJsonPath))
                // {
                //     ocrParamsJson = File.ReadAllText(configJsonPath);
                // }
                OCRParameter param = new OCRParameter();
                param.enable_mkldnn = false;

                _engine = new PaddleOCREngine(null, param);
            }
            catch (Exception ex)
            {
                
                throw new Exception($"PaddleOCR引擎初始化失败: {ex.Message}");
            }
        }

        public static string RecognizeText(Image image)
        {
            return Instance.Execute(image);
        }

        private string Execute(Image image)
        {
            try
            {
                if (_architecture != Architecture.X64)
                    return "***PaddleOCR不支持32位系统，请使用64位系统***";

                if (_engine == null)
                    return "***PaddleOCR引擎未初始化***";

                if (image == null)
                    return "***图像为空***";

                byte[] imageBytes = ImageToBytes(image);
                var ocrResult = _engine.DetectText(imageBytes);

                if (ocrResult?.TextBlocks == null || ocrResult.TextBlocks.Count == 0)
                    return "***该区域未发现文本***";

                var sb = new StringBuilder();
                foreach (var textBlock in ocrResult.TextBlocks)
                {
                    if (!string.IsNullOrWhiteSpace(textBlock.Text))
                        sb.AppendLine(textBlock.Text);
                }

                string result = sb.ToString().Trim();
                return string.IsNullOrEmpty(result) ? "***该区域未发现文本***" : result;
            }
            catch (Exception ex)
            {
                return $"***PaddleOCR识别失败: {ex.Message}***";
            }
        }

        private byte[] ImageToBytes(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _engine?.Dispose();
                _engine = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        public static void Reset()
        {
            if (_instance.IsValueCreated)
                Instance.Dispose();
        }

        public static bool IsSupported()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64;
        }
    }
}