using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using OcrLiteLib;

namespace TrOCR.Helper
{
    /// <summary>
    /// RapidOCR离线识别帮助类
    /// 基于OcrLiteLib库实现的单例模式OCR识别器
    /// </summary>
    public class RapidOCRHelper
    {
        #region 单例模式
        private static RapidOCRHelper _instance;
        private static readonly object _lock = new object();

        public static RapidOCRHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new RapidOCRHelper();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 私有字段
        private OcrLite _ocrEngine;
        private bool _isInitialized = false;
        private string _modelsPath;
        
        // 默认参数配置
        private int _padding = 50;
        private int _imgResize = 1024;
        private float _boxScoreThresh = 0.5f;
        private float _boxThresh = 0.3f;
        private float _unClipRatio = 1.6f;
        private bool _doAngle = true;
        private bool _mostAngle = true;
        private int _numThreads = 4;
        #endregion

        #region 构造函数
        private RapidOCRHelper()
        {
            // 检查系统架构
            CheckArchitecture();
            
            // 设置默认模型路径
            _modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");
        }
        #endregion

        #region 公共属性
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 模型文件路径
        /// </summary>
        public string ModelsPath
        {
            get => _modelsPath;
            set => _modelsPath = value;
        }

        /// <summary>
        /// 是否输出部分图片
        /// </summary>
        public bool IsPartImg
        {
            get => _ocrEngine?.isPartImg ?? false;
            set
            {
                if (_ocrEngine != null)
                    _ocrEngine.isPartImg = value;
            }
        }

        /// <summary>
        /// 是否输出调试图片
        /// </summary>
        public bool IsDebugImg
        {
            get => _ocrEngine?.isDebugImg ?? false;
            set
            {
                if (_ocrEngine != null)
                    _ocrEngine.isDebugImg = value;
            }
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化OCR引擎
        /// </summary>
        /// <param name="modelsPath">模型文件夹路径</param>
        /// <param name="numThreads">线程数</param>
        /// <returns>初始化是否成功</returns>
        public bool Initialize(string modelsPath = null, int numThreads = 4)
        {
            try
            {
                if (!string.IsNullOrEmpty(modelsPath))
                    _modelsPath = modelsPath;

                _numThreads = numThreads;

                // 检查模型文件
                var modelFiles = GetModelFilePaths();
                if (!ValidateModelFiles(modelFiles))
                {
                    return false;
                }

                // 创建OCR引擎实例
                _ocrEngine = new OcrLite();
                
                // 初始化模型
                _ocrEngine.InitModels(
                    modelFiles.DetPath,
                    modelFiles.ClsPath,
                    modelFiles.RecPath,
                    modelFiles.KeysPath,
                    _numThreads
                );

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"RapidOCR初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 使用默认参数初始化
        /// </summary>
        /// <returns>初始化是否成功</returns>
        public bool Initialize()
        {
            return Initialize(_modelsPath, _numThreads);
        }
        #endregion

        #region OCR识别方法
        /// <summary>
        /// 识别图片文件
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <returns>识别结果</returns>
        public OcrResult RecognizeFromFile(string imagePath)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("OCR引擎未初始化，请先调用Initialize方法");
            }

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"图片文件不存在: {imagePath}");
            }

            try
            {
                return _ocrEngine.Detect(
                    imagePath,
                    _padding,
                    _imgResize,
                    _boxScoreThresh,
                    _boxThresh,
                    _unClipRatio,
                    _doAngle,
                    _mostAngle
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"OCR识别失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 识别Bitmap图片
        /// </summary>
        /// <param name="bitmap">位图对象</param>
        /// <returns>识别结果</returns>
        public OcrResult RecognizeFromBitmap(Bitmap bitmap)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("OCR引擎未初始化，请先调用Initialize方法");
            }

            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            try
            {
                // 将Bitmap转换为临时文件
                string tempPath = Path.GetTempFileName() + ".png";
                bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

                try
                {
                    return RecognizeFromFile(tempPath);
                }
                finally
                {
                    // 清理临时文件
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"OCR识别失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 识别屏幕截图
        /// </summary>
        /// <param name="rect">截图区域</param>
        /// <returns>识别结果</returns>
        public OcrResult RecognizeFromScreen(Rectangle rect)
        {
            try
            {
                using (var bitmap = CaptureScreen(rect))
                {
                    return RecognizeFromBitmap(bitmap);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"屏幕截图OCR识别失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 快速识别，只返回文本结果
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>识别的文本</returns>
        public string RecognizeText(string imagePath)
        {
            var result = RecognizeFromFile(imagePath);
            return result?.StrRes ?? string.Empty;
        }

        /// <summary>
        /// 快速识别Bitmap，只返回文本结果
        /// </summary>
        /// <param name="bitmap">位图对象</param>
        /// <returns>识别的文本</returns>
        public string RecognizeText(Bitmap bitmap)
        {
            var result = RecognizeFromBitmap(bitmap);
            return result?.StrRes ?? string.Empty;
        }
        #endregion

        #region 参数配置方法
        /// <summary>
        /// 设置识别参数
        /// </summary>
        public void SetParameters(
            int padding = 50,
            int imgResize = 1024,
            float boxScoreThresh = 0.5f,
            float boxThresh = 0.3f,
            float unClipRatio = 1.6f,
            bool doAngle = true,
            bool mostAngle = true)
        {
            _padding = padding;
            _imgResize = imgResize;
            _boxScoreThresh = boxScoreThresh;
            _boxThresh = boxThresh;
            _unClipRatio = unClipRatio;
            _doAngle = doAngle;
            _mostAngle = mostAngle;
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 检查系统架构是否支持RapidOCR
        /// </summary>
        /// <returns>如果支持返回true，否则返回false</returns>
        private static bool CheckArchitecture()
        {
            try
            {
                // 检查是否为64位系统
                bool is64Bit = Environment.Is64BitOperatingSystem && Environment.Is64BitProcess;
                
                // 记录系统信息
                System.Diagnostics.Debug.WriteLine($"系统架构检查 - 操作系统: {(Environment.Is64BitOperatingSystem ? "64位" : "32位")}, 进程: {(Environment.Is64BitProcess ? "64位" : "32位")}");
                
                // RapidOCR通常支持32位和64位系统，但建议使用64位以获得更好性能
                if (!is64Bit)
                {
                    System.Diagnostics.Debug.WriteLine("警告: 当前运行在32位环境下，RapidOCR性能可能受限，建议使用64位系统");
                }
                
                return true; // RapidOCR支持32位和64位
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"架构检查失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取模型文件路径
        /// </summary>
        private (string DetPath, string ClsPath, string RecPath, string KeysPath) GetModelFilePaths()
        {
            return (
                DetPath: Path.Combine(_modelsPath, "ch_PP-OCRv3_det_infer.onnx"),
                ClsPath: Path.Combine(_modelsPath, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
                RecPath: Path.Combine(_modelsPath, "ch_PP-OCRv3_rec_infer.onnx"),
                KeysPath: Path.Combine(_modelsPath, "ppocr_keys_v1.txt")
            );
        }

        /// <summary>
        /// 验证模型文件是否存在
        /// </summary>
        private bool ValidateModelFiles((string DetPath, string ClsPath, string RecPath, string KeysPath) modelFiles)
        {
            var missingFiles = new System.Collections.Generic.List<string>();

            if (!File.Exists(modelFiles.DetPath))
                missingFiles.Add($"检测模型: {modelFiles.DetPath}");
            
            if (!File.Exists(modelFiles.ClsPath))
                missingFiles.Add($"分类模型: {modelFiles.ClsPath}");
            
            if (!File.Exists(modelFiles.RecPath))
                missingFiles.Add($"识别模型: {modelFiles.RecPath}");
            
            if (!File.Exists(modelFiles.KeysPath))
                missingFiles.Add($"字典文件: {modelFiles.KeysPath}");

            if (missingFiles.Count > 0)
            {
                string message = "以下模型文件不存在:\n" + string.Join("\n", missingFiles);
                MessageBox.Show(message, "模型文件缺失", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 截取屏幕指定区域
        /// </summary>
        private Bitmap CaptureScreen(Rectangle rect)
        {
            var bitmap = new Bitmap(rect.Width, rect.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
            }
            return bitmap;
        }
        #endregion

        #region 静态方法 - 用于天若OCR集成
        /// <summary>
        /// 静态方法：识别图像并返回文本结果
        /// 用于与天若OCR主程序集成
        /// </summary>
        /// <param name="image">要识别的图像</param>
        /// <returns>识别结果文本</returns>
        public static string RecognizeText(Image image)
        {
            try
            {
                if (image == null)
                    return "***图像为空***";

                var instance = Instance;
                
                // 如果未初始化，尝试自动初始化
                if (!instance.IsInitialized)
                {
                    if (!instance.Initialize())
                    {
                        return "***RapidOCR初始化失败***";
                    }
                }

                // 将Image转换为Bitmap
                Bitmap bitmap;
                if (image is Bitmap bmp)
                {
                    bitmap = bmp;
                }
                else
                {
                    bitmap = new Bitmap(image);
                }

                var result = instance.RecognizeFromBitmap(bitmap);
                
                // 如果bitmap是新创建的，需要释放
                if (!(image is Bitmap))
                {
                    bitmap.Dispose();
                }

                return result?.StrRes ?? "***该区域未发现文本***";
            }
            catch (Exception ex)
            {
                return $"***RapidOCR识别失败: {ex.Message}***";
            }
        }

        /// <summary>
        /// 重置并释放OCR引擎资源
        /// 用于天若OCR在切换引擎时调用
        /// </summary>
        public static void Reset()
        {
            try
            {
                if (_instance != null)
                {
                    _instance.Dispose();
                    _instance = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RapidOCR重置失败: {ex.Message}");
            }
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                //_ocrEngine?.Dispose();
                _ocrEngine = null;
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                // 记录日志但不抛出异常
                System.Diagnostics.Debug.WriteLine($"RapidOCR资源释放异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~RapidOCRHelper()
        {
            Dispose();
        }
        #endregion
    }
}