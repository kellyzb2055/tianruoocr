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

        public static string BD_ACCURATE_API_ID = "";

        public static string BD_ACCURATE_API_KEY = "";

        public static string BD_ACCURATE_LANGUAGE = "";

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
            AutoCopyOcrResult = Convert.ToBoolean(GetValue("识别后操作", "AutoCopyOcrResult", "False"));
            // AutoTranslateOcrResult = Convert.ToBoolean(GetValue("工具栏", "翻译", "False"));
            AutoCopyOcrTranslation = Convert.ToBoolean(GetValue("翻译后操作", "AutoCopyOcrTranslation", "False"));
            AutoCopyInputTranslation = Convert.ToBoolean(GetValue("翻译后操作", "AutoCopyInputTranslation", "False"));

             // --- 新增: 加载工具栏设置 ---
            IsMergeRemoveSpace = Convert.ToBoolean(GetValue("工具栏", "IsMergeRemoveSpace", "False"));
            IsMergeAutoCopy = Convert.ToBoolean(GetValue("工具栏", "IsMergeAutoCopy", "False"));
            IsSplitAutoCopy = Convert.ToBoolean(GetValue("工具栏", "IsSplitAutoCopy", "False"));

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
