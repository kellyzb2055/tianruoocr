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
        // 1. 修改：将 Lazy<T> 实例替换为可空的静态实例字段。
        private static PaddleOCRHelper _instance;

        private PaddleOCREngine _engine;
        private readonly Architecture _architecture;
        private bool _disposed = false;

        // 2. 修改：更改 Instance 属性的实现逻辑。
        public static PaddleOCRHelper Instance
        {
            get
            {
                // 如果实例不存在，则创建一个新的。
                if (_instance == null)
                {
                    _instance = new PaddleOCRHelper();
                }
                return _instance;
            }
        }

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
                // 初始化失败时，确保 _engine 为 null，以便 Execute 方法能正确报告错误。
                _engine = null; 
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
                
                // 此处的检查现在可以捕获初始化失败的情况。
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

        // 3. 修改：Reset 方法现在会销毁实例，允许垃圾回收。
        public static void Reset()
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null; // 关键：移除静态引用，使对象可被回收。

                // 新增：强制进行垃圾回收，以立即释放引擎占用的内存
                // 这对于处理大型非托管资源（如OCR模型）非常有效
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public static bool IsSupported()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64;
        }
    }
}