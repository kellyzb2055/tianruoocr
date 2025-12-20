using System;
using System.Collections.Generic;
using System.Drawing;

namespace TrOCR.Helper
{

	public static class StaticValue
	{
	       public class TranslateConfig
	       {
	           public string Source { get; set; }
	           public string Target { get; set; }
	           public string AppId { get; set; }
	           public string ApiKey { get; set; }
	       }

	       public static string Translate_Current_API = "Bing2";
	       public static readonly Dictionary<string, TranslateConfig> Translate_Configs = new Dictionary<string, TranslateConfig>();

	       public static string v_Split;

        public static string v_Restore;

        public static string v_Merge;

        public static string v_googleTranslate_txt;

        public static string v_googleTranslate_back;

        public static int image_h;

        public static int image_w;

        public static string v_single;

        public static Image image_OCR;

        public static string CurrentVersion;

        public static string copy_f;

        public static string content;

        public static bool ZH2EN;

        public static bool ZH2JP;

        public static bool ZH2KO;

        public static bool set_默认;

        public static bool set_拆分;

        public static bool set_合并;

        public static bool set_翻译;

        public static bool set_记录;

        public static bool set_截图;

        // 合并时去除空格
        public static bool IsMergeRemoveSpace = false;
        // 合并时去除所有空格
        public static bool IsMergeRemoveAllSpace = false;
        // 合并后自动复制
        public static bool IsMergeAutoCopy = false;
        // 拆分后自动复制
        public static bool IsSplitAutoCopy = false;

        public static float DpiFactor;

        public static IntPtr mainHandle;

        public static string note;

        public static string[] v_note;

        public static int NoteCount;

        public static string BD_API_ID = "";

        public static string BD_API_KEY = "";

        public static string BD_LANGUAGE = "";

        public static string TX_API_ID = "";

        public static string TX_API_KEY = "";

        public static string TX_LANGUAGE = "";

        public static string TX_ACCURATE_API_ID = "";

        public static string TX_ACCURATE_API_KEY = "";

        public static string TX_ACCURATE_LANGUAGE = "";

        // 腾讯表格识别配置
        public static string TX_TABLE_API_ID = "";
        public static string TX_TABLE_API_KEY = "";

        public static string BD_ACCURATE_API_ID = "";

        public static string BD_ACCURATE_API_KEY = "";

        public static string BD_ACCURATE_LANGUAGE = "";

        // 百度表格识别配置
        public static string BD_TABLE_API_ID = "";
        public static string BD_TABLE_API_KEY = "";
        // 【新增】百度手写识别配置
        public static string BD_HANDWRITING_API_ID { get; set; }
        public static string BD_HANDWRITING_API_KEY { get; set; }
        public static string BD_HANDWRITING_LANGUAGE { get; set; }

// --- OCR Token Caching ---

        // Baidu OCR
        public static string BaiduAccessToken = null;
        public static DateTime BaiduAccessTokenExpiry = DateTime.MinValue;

        // Baidu OCR (High Accuracy)
        public static string BaiduAccurateAccessToken = null;
        public static DateTime BaiduAccurateAccessTokenExpiry = DateTime.MinValue;

        // Baimiao OCR
        public static string BaimiaoUsername = "";
        public static string BaimiaoPassword = "";
        public static string BaimiaoToken = null;
        public static DateTime BaimiaoTokenExpiry = DateTime.MinValue;
        public static string BaimiaoDeviceUuid = null;

        public static bool IsCapture;

        public static bool v_topmost;

        // --- 缓存的配置项 ---
        public static bool InputTranslateClipboard { get; set; }
        public static bool InputTranslateAutoTranslate { get; set; }
        public static bool AutoCopyOcrResult { get; set; }
        // public static bool AutoTranslateOcrResult { get; set; }
        public static bool AutoCopyOcrTranslation { get; set; }
        public static bool AutoCopyInputTranslation { get; set; }
        
        //监听剪贴板翻译配置
        public static bool ListenClipboardTranslation { get; set; }
        public static bool AutoCopyListenClipboardTranslation { get; set; }
        public static bool ListenClipboardTranslationHideOriginal { get; set; }
        public static bool DisableToggleOriginalButton { get; set; }
        
         // 【新增】用于记忆上次的临时翻译语言，并设置初始默认值
        public static string LastTempSourceLang { get; set; } = "en";
        public static string LastTempTargetLang { get; set; } = "zh";

         // 【新增】专门用于“截图翻译”的自动复制选项
        public static bool AutoCopyScreenshotTranslation { get; set; }
        // 【新增】用于控制是否使用无窗口截图
         public static bool NoWindowScreenshotTranslation { get; set; }

        // ======================================================
        // 【新增】OpenAI 兼容接口配置缓存
        // ======================================================

        //  [OpenAICompatible]
        public static string OpenAICompatible_OCR_BASE_URL { get; set; }
        public static string OpenAICompatible_OCR_API_KEY { get; set; }
        public static string OpenAICompatible_OCR_MODEL { get; set; }
        public static string OpenAICompatible_OCR_CONFIG_PATH { get; set; }
        //  [OpenAICompatibleTrans]
        public static string OpenAICompatible_Trans_BASE_URL { get; set; }
        public static string OpenAICompatible_Trans_API_KEY { get; set; }
        public static string OpenAICompatible_Trans_MODEL { get; set; }
        public static string OpenAICompatible_Trans_CONFIG_PATH { get; set; }
        public static string OpenAICompatible_Trans_Source { get; set; }
        public static string OpenAICompatible_Trans_Target { get; set; }
        //文本改变自动翻译延时
        // public static int TextChangeAutotranslateDelay { get; set; }
        public static string TextChangeAutotranslateDelayRaw { get; set; }

        //工具栏图标放大倍数
        public static float ToolbarIconScaleFactor = 1.0f;


        /// <summary>
        /// 从config.ini加载配置到静态变量
        /// </summary>
        public static void LoadConfig()
        {
            string GetValue(string section, string key, string defaultValue)
            {
                var value = IniHelper.GetValue(section, key);
                return (value == "发生错误" || string.IsNullOrEmpty(value)) ? defaultValue : value;
            }

            InputTranslateClipboard = Convert.ToBoolean(GetValue("配置", "InputTranslateClipboard", "False"));
            InputTranslateAutoTranslate = Convert.ToBoolean(GetValue("配置", "InputTranslateAutoTranslate", "False"));
            AutoCopyInputTranslation = Convert.ToBoolean(GetValue("配置", "AutoCopyInputTranslation", "False"));
            AutoCopyOcrResult = Convert.ToBoolean(GetValue("常规识别", "AutoCopyOcrResult", "False"));
            // AutoTranslateOcrResult = Convert.ToBoolean(GetValue("工具栏", "翻译", "False"));
            AutoCopyOcrTranslation = Convert.ToBoolean(GetValue("常规翻译", "AutoCopyOcrTranslation", "False"));

            ListenClipboardTranslation = Convert.ToBoolean(GetValue("配置", "ListenClipboard", "False"));
            AutoCopyListenClipboardTranslation = Convert.ToBoolean(GetValue("配置", "AutoCopyListenClipboardTranslation", "False"));
            ListenClipboardTranslationHideOriginal = Convert.ToBoolean(GetValue("配置", "ListenClipboardTranslationHideOriginal", "False"));
            DisableToggleOriginalButton = Convert.ToBoolean(GetValue("配置", "DisableToggleOriginalButton", "False"));
            AutoCopyScreenshotTranslation = Convert.ToBoolean(GetValue("配置", "AutoCopyScreenshotTranslation", "False"));
            NoWindowScreenshotTranslation = Convert.ToBoolean(GetValue("配置", "NoWindowScreenshotTranslation", "False"));

            // --- 新增: 加载工具栏设置 ---
            IsMergeRemoveSpace = Convert.ToBoolean(GetValue("工具栏", "IsMergeRemoveSpace", "False"));
            IsMergeRemoveAllSpace = Convert.ToBoolean(GetValue("工具栏", "IsMergeRemoveAllSpace", "False"));

            IsMergeAutoCopy = Convert.ToBoolean(GetValue("工具栏", "IsMergeAutoCopy", "False"));
            IsSplitAutoCopy = Convert.ToBoolean(GetValue("工具栏", "IsSplitAutoCopy", "False"));


            // --- 新增: 加载百度表格识别密钥 ---
            // BD_TABLE_API_ID = GetValue("密钥_百度表格", "secret_id", "");
            // BD_TABLE_API_KEY = GetValue("密钥_百度表格", "secret_key", "");

            OpenAICompatible_OCR_BASE_URL = GetValue("OpenAICompatible", "BaseUrl", "");
            OpenAICompatible_OCR_API_KEY = GetValue("OpenAICompatible", "APIKey", "");
            OpenAICompatible_OCR_MODEL = GetValue("OpenAICompatible", "Model", "");
            OpenAICompatible_OCR_CONFIG_PATH = GetValue("OpenAICompatible", "Config", "");

            OpenAICompatible_Trans_BASE_URL = GetValue("OpenAICompatibleTrans", "BaseUrl", "");
            OpenAICompatible_Trans_API_KEY = GetValue("OpenAICompatibleTrans", "APIKey", "");
            OpenAICompatible_Trans_MODEL = GetValue("OpenAICompatibleTrans", "Model", "");
            OpenAICompatible_Trans_CONFIG_PATH = GetValue("OpenAICompatibleTrans", "Config", "");
            OpenAICompatible_Trans_Source= GetValue("OpenAICompatibleTrans", "Source", "Auto Detect");
            OpenAICompatible_Trans_Target= GetValue("OpenAICompatibleTrans", "Target", "自动判断");

            // TextChangeAutotranslateDelay=GetIntValue("配置", "文本改变自动翻译延时", 5000);
            TextChangeAutotranslateDelayRaw=GetValue("配置", "文本改变自动翻译延时", "5000");

            ToolbarIconScaleFactor = GetFloatValue("工具栏","图标放大倍数",1.0f);

        }
        // 1. 定义读取 Int 的辅助方法
        public static int GetIntValue(string section, string key, int defaultValue)
        {
            // 先获取字符串值
            string valueStr = IniHelper.GetValue(section, key);

            // 检查是否为空或读取错误
            if (valueStr == "发生错误" || string.IsNullOrEmpty(valueStr))
            {
                return defaultValue;
            }

            // 尝试转换为 int
            if (int.TryParse(valueStr, out int result))
            {
                return result; // 转换成功，返回读取到的值
            }

            // 如果内容是乱码或非数字，返回默认值
            return defaultValue;
        }
        // 辅助方法：安全读取浮点数配置
        public static float GetFloatValue(string section, string key, float defaultValue)
        {
            // 使用 TrOCRUtils.LoadSetting 读取字符串
            string valueStr = TrOCRUtils.LoadSetting(section, key, defaultValue.ToString());

            // 尝试转换为 float
            if (float.TryParse(valueStr, out float result))
            {
                return result;
            }

            // 如果转换失败，返回默认值
            return defaultValue;
        }

        static StaticValue()
  {
   note = "";
   NoteCount = 40;
			copy_f = "无格式";
			content = "天若OCR更新";
			ZH2EN = true;
			ZH2JP = false;
			ZH2KO = false;
			set_默认 = true;
			set_拆分 = false;
			set_合并 = false;
			set_翻译 = false;
			set_记录 = false;
			set_截图 = false;
			DpiFactor = 1f;
			// 动态获取程序集版本，确保一致性
			CurrentVersion = System.Windows.Forms.Application.ProductVersion;
		}

		
	}
}
