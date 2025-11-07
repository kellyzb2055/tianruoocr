using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Win32;
using TrOCR.Helper;
using TrOCR.Properties;
using System.Threading.Tasks;


namespace TrOCR
{
    // 设置窗口类，用于管理OCR和翻译接口的各种配置选项
	public sealed partial class FmSetting
	{
		private Dictionary<Control, Point> _originalControlLocations;

		private readonly Dictionary<string, string> shortcutMappings = new Dictionary<string, string>
		{
			{ "txtBox_文字识别", "文字识别" },
			{ "txtBox_翻译文本", "翻译文本" },
			{ "txtBox_记录界面", "记录界面" },
			{ "txtBox_识别界面", "识别界面" },
			{ "txtBox_输入翻译", "输入翻译" },
			{ "txtBox_静默识别", "静默识别" },
			{ "txtBox_截图翻译", "截图翻译" },

		};
		private bool paddleOcrConfigChanged = false;
    	private bool paddleOcr2ConfigChanged = false;
    	private bool rapidOcrConfigChanged = false;

		// 构造函数，初始化设置窗口
		public FmSetting()
		{
			Font = new Font(Font.Name, 9f / StaticValue.DpiFactor, Font.Style, Font.Unit, Font.GdiCharSet, Font.GdiVerticalFont);
			InitializeComponent();
		}
		private void checkbox_AutoCopyScreenshotTranslation_CheckedChanged(object sender, EventArgs e)
		{
		    // 联动逻辑：只有当“自动复制”被勾选时，“不显示窗口”选项才可用
		    checkbox_NoWindowScreenshotTranslation.Enabled = checkbox_AutoCopyScreenshotTranslation.Checked;

		    // 如果“自动复制”被取消勾选，则“不显示窗口”也必须被取消勾选
		    if (!checkbox_AutoCopyScreenshotTranslation.Checked)
		    {
		        checkbox_NoWindowScreenshotTranslation.Checked = false;
		    }
		}

		// 从配置文件读取设置信息并初始化设置界面控件
		public void readIniFile()
		{
			// 读取基本配置项
			var value = IniHelper.GetValue("配置", "开机自启");
			if (value == "发生错误")
			{
				cbBox_开机.Checked = true;
			}
			try
			{
				cbBox_开机.Checked = Convert.ToBoolean(value);
			}
			catch
			{
				cbBox_开机.Checked = true;
			}

			var value2 = IniHelper.GetValue("配置", "快速翻译");
			if (value2 == "发生错误")
			{
				cbBox_翻译.Checked = true;
			}
			try
			{
				cbBox_翻译.Checked = Convert.ToBoolean(value2);
			}
			catch
			{
				cbBox_翻译.Checked = true;
			}

			var value3 = IniHelper.GetValue("配置", "识别弹窗");
			if (value3 == "发生错误")
			{
				cbBox_弹窗.Checked = true;
			}
			try
			{
				cbBox_弹窗.Checked = Convert.ToBoolean(value3);
			}
			catch
			{
				cbBox_弹窗.Checked = true;
			}

			var value_input_translate_clipboard = IniHelper.GetValue("配置", "InputTranslateClipboard");
			if (value_input_translate_clipboard == "发生错误")
			{
				cbBox_输入翻译剪贴板.Checked = false;
			}
			try
			{
				cbBox_输入翻译剪贴板.Checked = Convert.ToBoolean(value_input_translate_clipboard);
			}
			catch
			{
				cbBox_输入翻译剪贴板.Checked = false;
			}

			var value_input_translate_auto = IniHelper.GetValue("配置", "InputTranslateAutoTranslate");
			if (value_input_translate_auto == "发生错误")
			{
				cbBox_输入翻译自动翻译.Checked = false;
			}
			try
			{
				cbBox_输入翻译自动翻译.Checked = Convert.ToBoolean(value_input_translate_auto);
			}
			catch
			{
				cbBox_输入翻译自动翻译.Checked = false;
			}

			var value_autoCopy = IniHelper.GetValue("常规识别", "AutoCopyOcrResult");
			if (value_autoCopy == "发生错误")
			{
				checkBox_AutoCopyOcrResult.Checked = false;
			}
			try
			{
				checkBox_AutoCopyOcrResult.Checked = Convert.ToBoolean(value_autoCopy);
			}
			catch
			{
				checkBox_AutoCopyOcrResult.Checked = false;
			}
			var value_autoTranslate = IniHelper.GetValue("工具栏", "翻译");
			if (value_autoTranslate == "发生错误")
			{
				checkBox_AutoTranslateOcrResult.Checked = false;
			}
			try
			{
				checkBox_AutoTranslateOcrResult.Checked = Convert.ToBoolean(value_autoTranslate);
			}
			catch
			{
				checkBox_AutoTranslateOcrResult.Checked = false;
			}

			var value_autoCopyOcr = IniHelper.GetValue("常规翻译", "AutoCopyOcrTranslation");
			if (value_autoCopyOcr == "发生错误")
			{
				checkBox_AutoCopyOcrTranslation.Checked = false;
			}
			try
			{
				checkBox_AutoCopyOcrTranslation.Checked = Convert.ToBoolean(value_autoCopyOcr);
			}
			catch
			{
				checkBox_AutoCopyOcrTranslation.Checked = false;
			}

			var value_autoCopyInput = IniHelper.GetValue("配置", "AutoCopyInputTranslation");
			if (value_autoCopyInput == "发生错误")
			{
				checkBox_AutoCopyInputTranslation.Checked = false;
			}
			try
			{
				checkBox_AutoCopyInputTranslation.Checked = Convert.ToBoolean(value_autoCopyInput);
			}
			catch
			{
				checkBox_AutoCopyInputTranslation.Checked = false;
			}

			var value_IsMergeRemoveSpace = IniHelper.GetValue("工具栏", "IsMergeRemoveSpace");
			if (value_IsMergeRemoveSpace == "发生错误")
			{
				checkBox_合并去除空格.Checked = false;
			}
			try
			{
				checkBox_合并去除空格.Checked = Convert.ToBoolean(value_IsMergeRemoveSpace);
			}
			catch
			{
				checkBox_合并去除空格.Checked = false;
			}

            checkBox_合并去除所有空格.Checked = TrOCRUtils.LoadSetting("工具栏", "IsMergeRemoveAllSpace", false);

			var value_IsMergeAutoCopy = IniHelper.GetValue("工具栏", "IsMergeAutoCopy");
			if (value_IsMergeAutoCopy == "发生错误")
			{
				checkBox_合并自动复制.Checked = false;
			}
			try
			{
				checkBox_合并自动复制.Checked = Convert.ToBoolean(value_IsMergeAutoCopy);
			}
			catch
			{
				checkBox_合并自动复制.Checked = false;
			}
			var value_IsSplitAutoCopy = IniHelper.GetValue("工具栏", "IsSplitAutoCopy");
			if (value_IsSplitAutoCopy == "发生错误")
			{
				checkBox_拆分后自动复制.Checked = false;
			}
			try
			{
				checkBox_拆分后自动复制.Checked = Convert.ToBoolean(value_IsSplitAutoCopy);
			}
			catch
			{
				checkBox_拆分后自动复制.Checked = false;
			}



			var value4 = IniHelper.GetValue("配置", "窗体动画");
			cobBox_动画.Text = value4;
			if (value4 == "发生错误")
			{
				cobBox_动画.Text = "窗体";
			}

			var value5 = IniHelper.GetValue("配置", "记录数目");
			numbox_记录.Value = Convert.ToInt32(value5);
			if (value5 == "发生错误")
			{
				numbox_记录.Value = 20m;
			}

			var value6 = IniHelper.GetValue("配置", "自动保存");
			if (value6 == "发生错误")
			{
				cbBox_保存.Checked = false;
			}
			try
			{
				cbBox_保存.Checked = Convert.ToBoolean(value6);
			}
			catch
			{
				cbBox_保存.Checked = false;
			}

			if (cbBox_保存.Checked)
			{
				textBox_path.Enabled = true;
				btn_浏览.Enabled = true;
			}
			if (!cbBox_保存.Checked)
			{
				textBox_path.Enabled = false;
				btn_浏览.Enabled = false;
			}

			var value7 = IniHelper.GetValue("配置", "截图位置");
			textBox_path.Text = value7;
			if (value7 == "发生错误")
			{
				textBox_path.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			}

			// 读取快捷键设置
			var value8 = IniHelper.GetValue("快捷键", "文字识别");
			txtBox_文字识别.Text = value8;
			if (value8 == "发生错误")
			{
				txtBox_文字识别.Text = "F4";
			}

			var value9 = IniHelper.GetValue("快捷键", "翻译文本");
			txtBox_翻译文本.Text = value9;
			if (value9 == "发生错误")
			{
				txtBox_翻译文本.Text = "F9";
			}

			var value10 = IniHelper.GetValue("快捷键", "记录界面");
			txtBox_记录界面.Text = value10;
			if (value10 == "发生错误")
			{
				txtBox_记录界面.Text = "请按下快捷键";
			}

			var value11 = IniHelper.GetValue("快捷键", "识别界面");
			txtBox_识别界面.Text = value11;
			if (value11 == "发生错误")
			{
				txtBox_识别界面.Text = "请按下快捷键";
			}

			// 设置快捷键图标状态
			pictureBox_文字识别.Image = txtBox_文字识别.Text == "请按下快捷键" ? Resources.快捷键_0 : Resources.快捷键_1;
			pictureBox_翻译文本.Image = txtBox_翻译文本.Text == "请按下快捷键" ? Resources.快捷键_0 : Resources.快捷键_1;
			pictureBox_记录界面.Image = txtBox_记录界面.Text == "请按下快捷键" ? Resources.快捷键_0 : Resources.快捷键_1;
			pictureBox_识别界面.Image = txtBox_识别界面.Text == "请按下快捷键" ? Resources.快捷键_0 : Resources.快捷键_1;

			// 【新增】加载“监听剪贴板”设置
			var value_listenClipboard = IniHelper.GetValue("配置", "ListenClipboard");
			try
			{
				cbBox_ListenClipboard.Checked = Convert.ToBoolean(value_listenClipboard);
			}
			catch
			{
				cbBox_ListenClipboard.Checked = false;
			}

			// 【新增】加载“监听后自动复制”设置
			var value_autoCopyListenClipboard = IniHelper.GetValue("配置", "AutoCopyListenClipboardTranslation");
			try
			{
				cbBox_AutoCopyListenClipboardTranslation.Checked = Convert.ToBoolean(value_autoCopyListenClipboard);
			}
			catch
			{
				cbBox_AutoCopyListenClipboardTranslation.Checked = false;
			}
			// 【新增】加载“监听剪贴板翻译后默认隐藏原文”设置
			var value_ListenClipboardHideOriginal = IniHelper.GetValue("配置", "ListenClipboardTranslationHideOriginal");
			try
			{
				cbBox_ListenHideOriginal.Checked = Convert.ToBoolean(value_ListenClipboardHideOriginal);
			}
			catch
			{
				cbBox_ListenHideOriginal.Checked = false;
			}
			//【新增】加载“全局禁用显示/隐藏原文按钮”设置
			cbBox_禁用隐藏原文按钮.Checked = TrOCRUtils.LoadSetting("配置", "DisableToggleOriginalButton", false);
			//加载“截图翻译后自动复制译文”设置
			checkbox_AutoCopyScreenshotTranslation.Checked = TrOCRUtils.LoadSetting("配置", "AutoCopyScreenshotTranslation", false);
			// 加载“无窗口截图翻译”设置
			checkbox_NoWindowScreenshotTranslation.Checked = TrOCRUtils.LoadSetting("配置", "NoWindowScreenshotTranslation", false);
			// 【重要】在加载后，立即执行一次联动逻辑，以确保初始状态正确
			checkbox_AutoCopyScreenshotTranslation_CheckedChanged(null, null);

			var value_input_translate = IniHelper.GetValue("快捷键", "输入翻译");
			txtBox_输入翻译.Text = value_input_translate;
			if (value_input_translate == "发生错误")
			{
				txtBox_输入翻译.Text = "请按下快捷键";
			}
			pictureBox_输入翻译.Image = txtBox_输入翻译.Text == "请按下快捷键" ? Resources.快捷键_0 : Resources.快捷键_1;

			var value_silent_ocr = IniHelper.GetValue("快捷键", "静默识别");
			txtBox_静默识别.Text = value_silent_ocr;
			if (value_silent_ocr == "发生错误")
			{
				txtBox_静默识别.Text = "请按下快捷键";
			}
			pictureBox_静默识别.Image = txtBox_静默识别.Text == "请按下快捷键" ? Resources.快捷键_0 : Resources.快捷键_1;

			txtBox_截图翻译.Text=TrOCRUtils.LoadSetting("快捷键", "截图翻译", "请按下快捷键");
			pictureBox_截图翻译.Image = txtBox_截图翻译.Text == "请按下快捷键" ? Resources.快捷键_0 : Resources.快捷键_1;


			// 读取百度OCR密钥信息
			var value12 = IniHelper.GetValue("密钥_百度", "secret_id");
			text_baiduaccount.Text = value12;
			if (value12 == "发生错误")
			{
				text_baiduaccount.Text = "YsZKG1wha34PlDOPYaIrIIKO";
			}

			var value13 = IniHelper.GetValue("密钥_百度", "secret_key");
			text_baidupassword.Text = value13;
			if (value13 == "发生错误")
			{
				text_baidupassword.Text = "HPRZtdOHrdnnETVsZM2Nx7vbDkMfxrkD";
			}

			// 读取百度OCR语言设置
			var valueBaiduLanguage = IniHelper.GetValue("密钥_百度", "language_code");
			BaiduOcrHelper.GetStandardLanguages().TryGetValue(valueBaiduLanguage, out string langName);
			comboBox_Baidu_Language.SelectedItem = string.IsNullOrEmpty(langName) ? "中英文混合" : langName;

			// 读取百度高精度OCR密钥信息
			var valueBaiduAccurateId = IniHelper.GetValue("密钥_百度高精度", "secret_id");
			text_baidu_accurate_apikey.Text = valueBaiduAccurateId == "发生错误" ? "" : valueBaiduAccurateId;
			var valueBaiduAccurateKey = IniHelper.GetValue("密钥_百度高精度", "secret_key");
			text_baidu_accurate_secretkey.Text = valueBaiduAccurateKey == "发生错误" ? "" : valueBaiduAccurateKey;

			// 读取百度高精度OCR语言设置
			var valueBaiduAccurateLanguage = IniHelper.GetValue("密钥_百度高精度", "language_code");
			BaiduOcrHelper.GetAccurateLanguages().TryGetValue(valueBaiduAccurateLanguage, out string accurateLangName);
			comboBox_Baidu_Accurate_Language.SelectedItem = string.IsNullOrEmpty(accurateLangName) ? "中英文混合" : accurateLangName;

			// 读取百度表格识别密钥信息
			var valueBaiduTableId = IniHelper.GetValue("密钥_百度表格", "secret_id");
			textBox2.Text = valueBaiduTableId == "发生错误" ? "" : valueBaiduTableId;
			var valueBaiduTableKey = IniHelper.GetValue("密钥_百度表格", "secret_key");
			textBox1.Text = valueBaiduTableKey == "发生错误" ? "" : valueBaiduTableKey;

			// 【新增】加载百度手写识别密钥
			text_baidu_handwriting_apikey.Text = TrOCRUtils.LoadSetting("密钥_百度手写", "secret_id", "");
			text_baidu_handwriting_secretkey.Text = TrOCRUtils.LoadSetting("密钥_百度手写", "secret_key", "");
			// 【新增】加载百度手写识别语言设置
			var valueBaiduHandwritingLang = IniHelper.GetValue("密钥_百度手写", "language_code");
			BaiduOcrHelper.GetAccurateLanguages().TryGetValue(valueBaiduHandwritingLang, out string handwritingLangName);
			comboBox_Baidu_Handwriting_Language.SelectedItem = string.IsNullOrEmpty(handwritingLangName) ? "中英文混合" : handwritingLangName;


			// 读取腾讯OCR密钥信息
			var valueTencentId = IniHelper.GetValue("密钥_腾讯", "secret_id");
			BoxTencentId.Text = valueTencentId;
			if (valueTencentId == "发生错误")
			{
				BoxTencentId.Text = "";
			}

			var valueTencentKey = IniHelper.GetValue("密钥_腾讯", "secret_key");
			BoxTencentKey.Text = valueTencentKey;
			if (valueTencentKey == "发生错误")
			{
				BoxTencentKey.Text = "";
			}

			// 读取腾讯OCR语言设置
			var valueTencentLanguage = IniHelper.GetValue("密钥_腾讯", "language_code");
			TencentOcrHelper.GetStandardLanguages().TryGetValue(valueTencentLanguage, out string tencentLangName);
			comboBox_Tencent_Language.SelectedItem = string.IsNullOrEmpty(tencentLangName) ? "中英混合" : tencentLangName;

			// 读取腾讯高精度OCR密钥信息
			var valueTencentAccurateId = IniHelper.GetValue("密钥_腾讯高精度", "secret_id");
			text_tencent_accurate_secretid.Text = valueTencentAccurateId == "发生错误" ? "" : valueTencentAccurateId;
			var valueTencentAccurateKey = IniHelper.GetValue("密钥_腾讯高精度", "secret_key");
			text_tencent_accurate_secretkey.Text = valueTencentAccurateKey == "发生错误" ? "" : valueTencentAccurateKey;

			// 读取腾讯高精度OCR语言设置
			var valueTencentAccurateLanguage = IniHelper.GetValue("密钥_腾讯高精度", "language_code");
			TencentOcrHelper.GetAccurateLanguages().TryGetValue(valueTencentAccurateLanguage, out string tencentAccurateLangName);
			comboBox_Tencent_Accurate_Language.SelectedItem = string.IsNullOrEmpty(tencentAccurateLangName) ? "自动检测" : tencentAccurateLangName;

			// 读取腾讯表格API密钥信息
			var valueTencentTableId = IniHelper.GetValue("密钥_腾讯表格v3", "secret_id");
			textBox3.Text = valueTencentTableId == "发生错误" ? "" : valueTencentTableId;
			var valueTencentTableKey = IniHelper.GetValue("密钥_腾讯表格v3", "secret_key");
			textBox4.Text = valueTencentTableKey == "发生错误" ? "" : valueTencentTableKey;

			// 读取白描账号信息
			var valueBaimiaoUsername = IniHelper.GetValue("密钥_白描", "username");
			BoxBaimiaoUsername.Text = valueBaimiaoUsername;
			if (valueBaimiaoUsername == "发生错误")
			{
				BoxBaimiaoUsername.Text = "";
			}

			var valueBaimiaoPassword = IniHelper.GetValue("密钥_白描", "password");
			BoxBaimiaoPassword.Text = valueBaimiaoPassword;
			if (valueBaimiaoPassword == "发生错误")
			{
				BoxBaimiaoPassword.Text = "";
			}

			// 读取代理设置
			var value14 = IniHelper.GetValue("代理", "代理类型");
			combox_代理.Text = value14;
			if (value14 == "发生错误")
			{
				combox_代理.Text = "系统代理";
			}

			if (combox_代理.Text == "不使用代理" || combox_代理.Text == "系统代理")
			{
				text_账号.Enabled = false;
				text_密码.Enabled = false;
				chbox_代理服务器.Enabled = false;
				text_端口.Enabled = false;
				text_服务器.Enabled = false;
			}

			if (combox_代理.Text == "自定义代理")
			{
				text_端口.Enabled = true;
				text_服务器.Enabled = true;
			}

			var value15 = IniHelper.GetValue("代理", "服务器");
			text_服务器.Text = value15;
			if (value15 == "发生错误")
			{
				text_服务器.Text = "127.0.0.1";
			}

			var value16 = IniHelper.GetValue("代理", "端口");
			text_端口.Text = value16;
			if (value16 == "发生错误")
			{
				text_端口.Text = "1080";
			}

			var value17 = IniHelper.GetValue("代理", "需要密码");
			if (value17 == "发生错误")
			{
				chbox_代理服务器.Checked = false;
			}

			try
			{
				chbox_代理服务器.Checked = Convert.ToBoolean(value17);
			}
			catch
			{
				chbox_代理服务器.Checked = false;
			}

			var value18 = IniHelper.GetValue("代理", "服务器账号");
			text_账号.Text = value18;
			if (value18 == "发生错误")
			{
				text_账号.Text = "";
			}

			var value19 = IniHelper.GetValue("代理", "服务器密码");
			text_密码.Text = value19;
			if (value19 == "发生错误")
			{
				text_密码.Text = "";
			}

			if (chbox_代理服务器.Checked)
			{
				text_账号.Enabled = true;
				text_密码.Enabled = true;
			}

			if (!chbox_代理服务器.Checked)
			{
				text_账号.Enabled = false;
				text_密码.Enabled = false;
			}

			// 读取更新设置
			var value20 = IniHelper.GetValue("更新", "检测更新");
			if (value20 == "发生错误")
			{
				check_检查更新.Checked = false;
			}

			try
			{
				check_检查更新.Checked = Convert.ToBoolean(value20);
			}
			catch
			{
				check_检查更新.Checked = false;
			}

			if (check_检查更新.Checked)
			{
				checkBox_更新间隔.Enabled = true;
			}

			if (!check_检查更新.Checked)
			{
				checkBox_更新间隔.Enabled = false;
				numbox_间隔时间.Enabled = false;
			}

			var value21 = IniHelper.GetValue("更新", "更新间隔");
			if (value21 == "发生错误")
			{
				checkBox_更新间隔.Checked = false;
			}

			try
			{
				checkBox_更新间隔.Checked = Convert.ToBoolean(value21);
			}
			catch
			{
				checkBox_更新间隔.Checked = false;
			}

			if (checkBox_更新间隔.Checked)
			{
				numbox_间隔时间.Enabled = true;
			}

			if (!checkBox_更新间隔.Checked)
			{
				numbox_间隔时间.Enabled = false;
			}

			var value22 = IniHelper.GetValue("更新", "间隔时间");
			numbox_间隔时间.Value = Convert.ToInt32(value22);
			if (value5 == "发生错误")
			{
				numbox_间隔时间.Value = 24m;
			}

			var value_pre_release = IniHelper.GetValue("更新", "CheckPreRelease");
			if (value_pre_release == "发生错误")
			{
				checkBox_PreRelease.Checked = false;
			}
			try
			{
				checkBox_PreRelease.Checked = Convert.ToBoolean(value_pre_release);
			}
			catch
			{
				checkBox_PreRelease.Checked = false;
			}

			// 读取截图音效设置
			var value23 = IniHelper.GetValue("截图音效", "粘贴板");
			if (value23 == "发生错误")
			{
				chbox_copy.Checked = false;
			}

			try
			{
				chbox_copy.Checked = Convert.ToBoolean(value23);
			}
			catch
			{
				chbox_copy.Checked = false;
			}

			var value24 = IniHelper.GetValue("截图音效", "自动保存");
			if (value24 == "发生错误")
			{
				chbox_save.Checked = true;
			}

			try
			{
				chbox_save.Checked = Convert.ToBoolean(value24);
			}
			catch
			{
				chbox_save.Checked = true;
			}

			var value25 = IniHelper.GetValue("截图音效", "音效路径");
			text_音效path.Text = value25;
			if (value25 == "发生错误")
			{
				text_音效path.Text = "Data\\screenshot.wav";
			}

			// 读取取色器设置
			var value26 = IniHelper.GetValue("取色器", "类型");
			if (value26 == "发生错误")
			{
				chbox_取色.Checked = false;
			}

			if (value26 == "RGB")
			{
				chbox_取色.Checked = false;
			}

			if (value26 == "HEX")
			{
				chbox_取色.Checked = true;
			}

			// 读取各翻译接口设置
			var googleSource = IniHelper.GetValue("Translate_Google", "Source");
			textBox_Google_Source.Text = (googleSource == "发生错误") ? "auto" : googleSource;
			var googleTarget = IniHelper.GetValue("Translate_Google", "Target");
			textBox_Google_Target.Text = (googleTarget == "发生错误") ? "自动判断" : googleTarget;

			var baiduSource = IniHelper.GetValue("Translate_Baidu", "Source");
			textBox_Baidu_Source.Text = (baiduSource == "发生错误") ? "auto" : baiduSource;
			var baiduTarget = IniHelper.GetValue("Translate_Baidu", "Target");
			textBox_Baidu_Target.Text = (baiduTarget == "发生错误") ? "自动判断" : baiduTarget;
			var baiduAK = IniHelper.GetValue("Translate_Baidu", "APP_ID");
			textBox_Baidu_AK.Text = (baiduAK == "发生错误") ? "" : baiduAK;
			var baiduSK = IniHelper.GetValue("Translate_Baidu", "APP_KEY");
			textBox_Baidu_SK.Text = (baiduSK == "发生错误") ? "" : baiduSK;

			var tencentSource = IniHelper.GetValue("Translate_Tencent", "Source");
			textBox_Tencent_Source.Text = (tencentSource == "发生错误") ? "auto" : tencentSource;
			var tencentTarget = IniHelper.GetValue("Translate_Tencent", "Target");
			textBox_Tencent_Target.Text = (tencentTarget == "发生错误") ? "自动判断" : tencentTarget;
			var tencentAK = IniHelper.GetValue("Translate_Tencent", "SecretId");
			textBox_Tencent_AK.Text = (tencentAK == "发生错误") ? "" : tencentAK;
			var tencentSK = IniHelper.GetValue("Translate_Tencent", "SecretKey");
			textBox_Tencent_SK.Text = (tencentSK == "发生错误") ? "" : tencentSK;

			var bingSource = IniHelper.GetValue("Translate_Bing", "Source");
			textBox_Bing_Source.Text = (bingSource == "发生错误") ? "auto" : bingSource;
			var bingTarget = IniHelper.GetValue("Translate_Bing", "Target");
			textBox_Bing_Target.Text = (bingTarget == "发生错误") ? "自动判断" : bingTarget;

			var bing2Source = IniHelper.GetValue("Translate_Bing2", "Source");
			textBox_Bing2_Source.Text = (bing2Source == "发生错误") ? "auto" : bing2Source;
			var bing2Target = IniHelper.GetValue("Translate_Bing2", "Target");
			textBox_Bing2_Target.Text = (bing2Target == "发生错误") ? "自动判断" : bing2Target;

			var microsoftSource = IniHelper.GetValue("Translate_Microsoft", "Source");
			textBox_Microsoft_Source.Text = (microsoftSource == "发生错误") ? "auto" : microsoftSource;
			var microsoftTarget = IniHelper.GetValue("Translate_Microsoft", "Target");
			textBox_Microsoft_Target.Text = (microsoftTarget == "发生错误") ? "自动判断" : microsoftTarget;

			var yandexSource = IniHelper.GetValue("Translate_Yandex", "Source");
			textBox_Yandex_Source.Text = (yandexSource == "发生错误") ? "auto" : yandexSource;
			var yandexTarget = IniHelper.GetValue("Translate_Yandex", "Target");
			textBox_Yandex_Target.Text = (yandexTarget == "发生错误") ? "自动判断" : yandexTarget;

			// 腾讯交互翻译
			var tencentInteractiveSource = IniHelper.GetValue("Translate_TencentInteractive", "Source");
			textBox_TencentInteractive_Source.Text = (tencentInteractiveSource == "发生错误") ? "auto" : tencentInteractiveSource;
			var tencentInteractiveTarget = IniHelper.GetValue("Translate_TencentInteractive", "Target");
			textBox_TencentInteractive_Target.Text = (tencentInteractiveTarget == "发生错误") ? "自动判断" : tencentInteractiveTarget;

			// 彩云小译
			var caiyunSource = IniHelper.GetValue("Translate_Caiyun", "Source");
			textBox_Caiyun_Source.Text = (caiyunSource == "发生错误") ? "auto" : caiyunSource;
			var caiyunTarget = IniHelper.GetValue("Translate_Caiyun", "Target");
			textBox_Caiyun_Target.Text = (caiyunTarget == "发生错误") ? "自动判断" : caiyunTarget;

			// 火山翻译
			var volcanoSource = IniHelper.GetValue("Translate_Volcano", "Source");
			textBox_Volcano_Source.Text = (volcanoSource == "发生错误") ? "auto" : volcanoSource;
			var volcanoTarget = IniHelper.GetValue("Translate_Volcano", "Target");
			textBox_Volcano_Target.Text = (volcanoTarget == "发生错误") ? "自动判断" : volcanoTarget;

			//百度翻译2
			TrOCRUtils.LoadSetting("Translate_Baidu2", "Source", "auto");
			TrOCRUtils.LoadSetting("Translate_Baidu2", "Target", "自动判断");
			// 彩云小译2
			var caiyun2Source = IniHelper.GetValue("Translate_Caiyun2", "Source");
			textBox_Caiyun2_Source.Text = (caiyun2Source == "发生错误") ? "auto" : caiyun2Source;
			var caiyun2Target = IniHelper.GetValue("Translate_Caiyun2", "Target");
			textBox_Caiyun2_Target.Text = (caiyun2Target == "发生错误") ? "自动判断" : caiyun2Target;
			var caiyun2Token = IniHelper.GetValue("Translate_Caiyun2", "Token");
			textBox_Caiyun2_Token.Text = (caiyun2Token == "发生错误") ? "3975l6lr5pcbvidl6jl2" : caiyun2Token;

			// 设置页的翻译接口可见性
			Action<string, CheckBox, TabPage> setTranVisibility = (apiName, checkBox, tabPage) =>
			{
				string visibilityValue = IniHelper.GetValue("翻译接口显示", apiName);
				bool isVisible;
				if (apiName == "TencentInteractive" || apiName == "Caiyun" || apiName == "Volcano" || apiName == "Baidu2")
				{
					isVisible = visibilityValue != "发生错误" && Convert.ToBoolean(visibilityValue);
				}
				else
				{
					isVisible = visibilityValue == "发生错误" || Convert.ToBoolean(visibilityValue);
				}
				checkBox.Checked = isVisible;
				if (!isVisible)
				{
					tabControl_Trans.TabPages.Remove(tabPage);
				}
			};

			setTranVisibility("Google", checkBox_ShowGoogle, tabPage_Google);
			setTranVisibility("Baidu", checkBox_ShowBaidu, tabPage_Baidu);
			setTranVisibility("Tencent", checkBox_ShowTencent, tabPage_Tencent);
			setTranVisibility("Bing", checkBox_ShowBing, tabPage_Bing);
			setTranVisibility("Bing2", checkBox_ShowBing2, tabPage_Bing2);
			setTranVisibility("Microsoft", checkBox_ShowMicrosoft, tabPage_Microsoft);
			setTranVisibility("Yandex", checkBox_ShowYandex, tabPage_Yandex);
			setTranVisibility("TencentInteractive", checkBox_ShowTencentInteractive, tabPage_TencentInteractive);
			setTranVisibility("Caiyun", checkBox_ShowCaiyun, tabPage_Caiyun);
			setTranVisibility("Volcano", checkBox_ShowVolcano, tabPage_Volcano);
			setTranVisibility("Caiyun2", checkBox_ShowCaiyun2, tabPage_Caiyun2);
			setTranVisibility("Baidu2", checkBox_ShowBaidu2, tabPage_Baidu2);

			// 设置页的OCR接口可见性
			Action<string, CheckBox, TabPage> setOcrVisibility = (apiName, checkBox, tabPage) =>
			{
				string visibilityValue = IniHelper.GetValue("Ocr接口显示", apiName);
				bool isVisible;
				if (apiName == "Baimiao")
				{
					isVisible = visibilityValue != "发生错误" && Convert.ToBoolean(visibilityValue);
				}
				else
				{
					isVisible = visibilityValue == "发生错误" || Convert.ToBoolean(visibilityValue);
				}
				checkBox.Checked = isVisible;
				if (!isVisible && tabPage != null)
				{
					tabControl2.TabPages.Remove(tabPage);
				}
			};
			//此处百度和腾讯的接口隐藏后移除密钥设置页的标签页功能已失效，暂时就这样吧
			setOcrVisibility("Baidu", checkBox_ShowOcrBaidu, inPage_百度接口);
			setOcrVisibility("BaiduAccurate", checkBox_ShowOcrBaiduAccurate, inPage_百度高精度接口);
			setOcrVisibility("Tencent", checkBox_ShowOcrTencent, inPage_腾讯接口);
			setOcrVisibility("TencentAccurate", checkBox_ShowOcrTencentAccurate, inPage_腾讯高精度接口);
			setOcrVisibility("Baimiao", checkBox_ShowOcrBaimiao, tabPage_白描接口);
			setOcrVisibility("Sougou", checkBox_ShowOcrSougou, null);
			setOcrVisibility("Youdao", checkBox_ShowOcrYoudao, null);
			setOcrVisibility("WeChat", checkBox_ShowOcrWeChat, null);
			setOcrVisibility("Mathfuntion", checkBox_ShowOcrMathfuntion, null);
			setOcrVisibility("Table", checkBox_ShowOcrTable, null);
			setOcrVisibility("Shupai", checkBox_ShowOcrShupai, null);
			setOcrVisibility("TableBaidu", checkBox_ShowOcrTableBaidu, null);
			setOcrVisibility("TableAli", checkBox_ShowOcrTableAli, null);
			setOcrVisibility("ShupaiLR", checkBox_ShowOcrShupaiLR, null);
			setOcrVisibility("ShupaiRL", checkBox_ShowOcrShupaiRL, null);
			setOcrVisibility("TencentTable", checkBox_ShowOcrTableTencent, null);
			setOcrVisibility("PaddleOCR", checkBox_ShowOcrPaddleOCR, inPage_PaddleOCR);
			setOcrVisibility("PaddleOCR2", checkBox_ShowOcrPaddleOCR2, inPage_PaddleOCR2);
			setOcrVisibility("RapidOCR", checkBox_ShowOcrRapidOCR, inPage_RapidOCR);

			// 读取OCR模型配置
			ReadOcrModelConfigs();
		}

		/// <summary>
		/// 窗口加载事件处理函数，用于初始化界面控件和读取配置文件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void Form1_Load(object sender, EventArgs e)
		{
			var componentResourceManager = new ComponentResourceManager(typeof(FmMain));
			Icon = (Icon)componentResourceManager.GetObject("minico.Icon");

			// 设置记录数目的数值范围
			var numericUpDown = numbox_记录;
			var array = new int[4];
			array[0] = 99;
			numericUpDown.Maximum = new decimal(array);

			var numericUpDown2 = numbox_记录;
			var array2 = new int[4];
			array2[0] = 1;
			numericUpDown2.Minimum = new decimal(array2);

			var numericUpDown3 = numbox_记录;
			var array3 = new int[4];
			array3[0] = 1;
			numericUpDown3.Value = new decimal(array3);

			// 设置更新间隔时间的数值范围
			var numericUpDown4 = numbox_间隔时间;
			var array4 = new int[4];
			array4[0] = 24;
			numericUpDown4.Maximum = new decimal(array4);

			var numericUpDown5 = numbox_间隔时间;
			var array5 = new int[4];
			array5[0] = 1;
			numericUpDown5.Minimum = new decimal(array5);

			var numericUpDown6 = numbox_间隔时间;
			var array6 = new int[4];
			array6[0] = 1;
			numericUpDown6.Value = new decimal(array6);

			// 添加ocr语言选项到下拉列表
			comboBox_Baidu_Language.Items.AddRange(BaiduOcrHelper.GetStandardLanguages().Values.ToArray());
			comboBox_Baidu_Accurate_Language.Items.AddRange(BaiduOcrHelper.GetAccurateLanguages().Values.ToArray());
			// 【新增】填充百度手写识别的语言下拉框
			comboBox_Baidu_Handwriting_Language.Items.AddRange(BaiduOcrHelper.GetAccurateLanguages().Values.ToArray());
			comboBox_Tencent_Accurate_Language.Items.AddRange(TencentOcrHelper.GetAccurateLanguages().Values.ToArray());
			comboBox_Tencent_Language.Items.AddRange(TencentOcrHelper.GetStandardLanguages().Values.ToArray());

			// 为百度标准版添加重置按钮
			Button btnResetBaiduLang = new Button();
			btnResetBaiduLang.Name = "btnResetBaiduLang";
			btnResetBaiduLang.Text = "重置";
			btnResetBaiduLang.Size = new Size(50, 23);
			btnResetBaiduLang.Location = new Point(comboBox_Baidu_Language.Right + 6, comboBox_Baidu_Language.Top - 2);
			btnResetBaiduLang.Click += (s, ev) => { comboBox_Baidu_Language.SelectedItem = "中英文混合"; };
			inPage_百度接口.Controls.Add(btnResetBaiduLang);

			// 为百度高精度版添加重置按钮
			Button btnResetBaiduAccurateLang = new Button();
			btnResetBaiduAccurateLang.Name = "btnResetBaiduAccurateLang";
			btnResetBaiduAccurateLang.Text = "重置";
			btnResetBaiduAccurateLang.Size = new Size(50, 23);
			btnResetBaiduAccurateLang.Location = new Point(comboBox_Baidu_Accurate_Language.Right + 6, comboBox_Baidu_Accurate_Language.Top - 2);
			btnResetBaiduAccurateLang.Click += (s, ev) => { comboBox_Baidu_Accurate_Language.SelectedItem = "中英文混合"; };
			inPage_百度高精度接口.Controls.Add(btnResetBaiduAccurateLang);

			// 为腾讯高精度版添加重置按钮
			Button btnResetTencentAccurateLang = new Button();
			btnResetTencentAccurateLang.Name = "btnResetTencentAccurateLang";
			btnResetTencentAccurateLang.Text = "重置";
			btnResetTencentAccurateLang.Size = new Size(50, 23);
			btnResetTencentAccurateLang.Location = new Point(comboBox_Tencent_Accurate_Language.Right + 6, comboBox_Tencent_Accurate_Language.Top - 2);
			btnResetTencentAccurateLang.Click += (s, ev) => { comboBox_Tencent_Accurate_Language.SelectedItem = "自动检测"; };
			inPage_腾讯高精度接口.Controls.Add(btnResetTencentAccurateLang);

			// 为腾讯标准版添加重置按钮
			Button btnResetTencentLang = new Button();
			btnResetTencentLang.Name = "btnResetTencentLang";
			btnResetTencentLang.Text = "重置";
			btnResetTencentLang.Size = new Size(50, 23);
			btnResetTencentLang.Location = new Point(comboBox_Tencent_Language.Right + 6, comboBox_Tencent_Language.Top - 2);
			btnResetTencentLang.Click += (s, ev) => { comboBox_Tencent_Language.SelectedItem = "中英混合"; };
			inPage_腾讯接口.Controls.Add(btnResetTencentLang);

			readIniFile();
			// 使用程序集的实际版本号，而不是写死的值
			label_VersionInfo.Text = "版本号：" + System.Windows.Forms.Application.ProductVersion;
			label_AuthorInfo.Text = "作者：topkill";
			chbox_代理服务器.CheckedChanged += chbox_代理服务器_CheckedChanged;
			更新Button_check.Click += 更新Button_check_Click;

			StoreOriginalLocations(this);
			tab_标签.SelectedIndexChanged += AdjustPageSize;
			tabControl2.SelectedIndexChanged += AdjustPageSize;
			tabControl_Trans.SelectedIndexChanged += AdjustPageSize;
			AdjustPageSize(tab_标签, EventArgs.Empty);

			// 为所有接口可见性复选框附加事件处理程序
			checkBox_ShowOcrBaidu.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrBaiduAccurate.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrTencent.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrTencentAccurate.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrBaimiao.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrSougou.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrYoudao.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrWeChat.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrMathfuntion.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrTable.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrShupai.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrTableBaidu.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrTableAli.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrShupaiLR.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrShupaiRL.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrTableTencent.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrPaddleOCR.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrPaddleOCR2.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowOcrRapidOCR.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowGoogle.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowBaidu.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowTencent.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowBing.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowBing2.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowMicrosoft.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowYandex.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowTencentInteractive.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowCaiyun.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowVolcano.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowCaiyun2.CheckedChanged += ApiVisibility_CheckedChanged;
			checkBox_ShowBaidu2.CheckedChanged += ApiVisibility_CheckedChanged;

			// 为OCR模型配置按钮添加事件处理程序
			// PaddleOCR事件
			btn_PaddleOCR_Det_Browse.Click += Btn_PaddleOCR_Det_Browse_Click;
			btn_PaddleOCR_Cls_Browse.Click += Btn_PaddleOCR_Cls_Browse_Click;
			btn_PaddleOCR_Rec_Browse.Click += Btn_PaddleOCR_Rec_Browse_Click;
			btn_PaddleOCR_Keys_Browse.Click += Btn_PaddleOCR_Keys_Browse_Click;
			button2.Click += Btn_PaddleOCR_AdvancedConfig_Browse_Click;
			textBox_PaddleOCR_Det.TextChanged += TextBox_PaddleOCR_TextChanged;
			textBox_PaddleOCR_Cls.TextChanged += TextBox_PaddleOCR_TextChanged;
			textBox_PaddleOCR_Rec.TextChanged += TextBox_PaddleOCR_TextChanged;
			textBox_PaddleOCR_Keys.TextChanged += TextBox_PaddleOCR_TextChanged;
			textBox5.TextChanged += TextBox_PaddleOCR_TextChanged;;

			// PaddleOCR2事件
			btn_PaddleOCR2_Det_Browse.Click += Btn_PaddleOCR2_Det_Browse_Click;
			btn_PaddleOCR2_Cls_Browse.Click += Btn_PaddleOCR2_Cls_Browse_Click;
			btn_PaddleOCR2_Rec_Browse.Click += Btn_PaddleOCR2_Rec_Browse_Click;
			btn_PaddleOCR2_Keys_Browse.Click += Btn_PaddleOCR2_Keys_Browse_Click;
			button3.Click += Btn_PaddleOCR2_AdvancedConfig_Browse_Click;
			textBox_PaddleOCR2_Det.TextChanged += TextBox_PaddleOCR2_TextChanged;
			textBox_PaddleOCR2_Cls.TextChanged += TextBox_PaddleOCR2_TextChanged;
			textBox_PaddleOCR2_Rec.TextChanged += TextBox_PaddleOCR2_TextChanged;
			textBox_PaddleOCR2_Keys.TextChanged += TextBox_PaddleOCR2_TextChanged;
			textBox6.TextChanged += TextBox_PaddleOCR2_TextChanged;
			comboBox_PaddleOCR2_det_Version.SelectedIndexChanged += ComboBox_PaddleOCR2_Version_SelectedIndexChanged;
			comboBox_PaddleOCR2_cls_Version.SelectedIndexChanged += ComboBox_PaddleOCR2_Version_SelectedIndexChanged;
			comboBox_PaddleOCR2_rec_Version.SelectedIndexChanged += ComboBox_PaddleOCR2_Version_SelectedIndexChanged;

			// RapidOCR事件
			btn_RapidOCR_Det_Browse.Click += Btn_RapidOCR_Det_Browse_Click;
			btn_RapidOCR_Cls_Browse.Click += Btn_RapidOCR_Cls_Browse_Click;
			btn_RapidOCR_Rec_Browse.Click += Btn_RapidOCR_Rec_Browse_Click;
			btn_RapidOCR_Keys_Browse.Click += Btn_RapidOCR_Keys_Browse_Click;
			button4.Click += Btn_RapidOCR_AdvancedConfig_Browse_Click;
			textBox_RapidOCR_Det.TextChanged += TextBox_RapidOCR_TextChanged;
			textBox_RapidOCR_Cls.TextChanged += TextBox_RapidOCR_TextChanged;
			textBox_RapidOCR_Rec.TextChanged += TextBox_RapidOCR_TextChanged;
			textBox_RapidOCR_Keys.TextChanged += TextBox_RapidOCR_TextChanged;
			textBox7.TextChanged += TextBox_RapidOCR_TextChanged;
		}

		/// <summary>
		/// ocr接口申请按钮点击事件处理函数，根据当前选中的标签页打开相应的OCR服务申请页面
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void 百度申请_Click(object sender, EventArgs e)
		{
			if (tabControl2.SelectedTab == tabPage_腾讯OCR)
    		{
    		    Process.Start("https://console.cloud.tencent.com/ocr/general");
    		}
			else if (tabControl2.SelectedTab == tabPage_白描接口)
			{
				// 白描不提供传统的API申请，显示提示信息
				MessageBox.Show("白描OCR使用账号登录方式，无需申请API密钥。\n\n请直接输入您的白描账号（手机号/邮箱）和密码即可使用。\n\n如需注册账号，请前往白描官网或下载白描App。",
					"白描OCR说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
				// 可选：打开白描官网
				// Process.Start("https://web.baimiaoapp.com");
			}
			else
			{
				Process.Start("https://console.bce.baidu.com/ai/");
			}
		}
		/// <summary>
		/// 通过HTTP POST请求获取指定URL的响应内容
		/// </summary>
		/// <param name="url">需要请求的URL地址</param>
		/// <returns>服务器返回的响应内容，如果请求失败则返回空字符串</returns>

		public static string Get_html(string url)
		{
			string result;
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						result = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				httpWebRequest.Abort();
			}
			catch
			{
				result = "";
			}
			return result;
		}

		/// <summary>
		/// 标签页选中索引变更事件处理函数，用于调整页面大小以适应内容
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void tab_标签_SelectedIndexChanged(object sender, EventArgs e)
		{
		          AdjustPageSize(sender, e);
		}

		/// <summary>
		/// 帮助图片点击事件处理函数，用于打开帮助窗口
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void pic_help_Click(object sender, EventArgs e)
		{
			new FmHelp().Show();
		}

		/// <summary>
		/// 开机自启复选框状态变更事件处理函数，用于设置程序是否开机自启
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void cbBox_开机_CheckedChanged(object sender, EventArgs e)
		{
			AutoStart(cbBox_开机.Checked);
		}

		/// <summary>
		/// 翻译复选框状态变更事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void cbBox_翻译_CheckedChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 弹窗复选框状态变更事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void cbBox_弹窗_CheckedChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 动画下拉框选中索引变更事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void cobBox_动画_SelectedIndexChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 记录数值框值变更事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void numbox_记录_ValueChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 保存复选框状态变更事件处理函数，用于控制路径文本框和浏览按钮的启用状态
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void cbBox_保存_CheckedChanged(object sender, EventArgs e)
		{
			if (cbBox_保存.Checked)
			{
				textBox_path.Enabled = true;
				btn_浏览.Enabled = true;
			}
			if (!cbBox_保存.Checked)
			{
				textBox_path.Enabled = false;
				btn_浏览.Enabled = false;
			}
		}

		/// <summary>
		/// 浏览按钮点击事件处理函数，用于选择文件夹路径
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void btn_浏览_Click(object sender, EventArgs e)
		{
			var folderBrowserDialog = new FolderBrowserDialog();
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				textBox_path.Text = folderBrowserDialog.SelectedPath;
			}
		}

		/// <summary>
		/// 密钥按钮点击事件处理函数，根据当前选中的标签页恢复对应接口的默认设置
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void 密钥Button_Click(object sender, EventArgs e)
		{
			// 根据当前选中的标签页，只恢复对应接口的默认设置
			if (tabControl2.SelectedTab == tabPage_百度OCR)
			{
				// 再判断内部是哪个子选项卡
				if (tabControl_BaiduApiType.SelectedTab == inPage_百度接口)
				{
					text_baiduaccount.Text = "YsZKG1wha34PlDOPYaIrIIKO";
					text_baidupassword.Text = "HPRZtdOHrdnnETVsZM2Nx7vbDkMfxrkD";
					comboBox_Baidu_Language.SelectedItem = "中英文混合";
				}
				else if (tabControl_BaiduApiType.SelectedTab == inPage_百度高精度接口)
				{
					text_baidu_accurate_apikey.Text = "";
					text_baidu_accurate_secretkey.Text = "";
					comboBox_Baidu_Accurate_Language.SelectedItem = "中英文混合";
				}
				else if (tabControl_BaiduApiType.SelectedTab == inPage_百度表格)
				{
					textBox2.Text = "";
					textBox1.Text = "";
				}
				else if (tabControl_BaiduApiType.SelectedTab == inPage_百度手写)
        		{
        		    // 清空专用密钥输入框
        		    text_baidu_handwriting_apikey.Text = "";
        		    text_baidu_handwriting_secretkey.Text = "";
        		    // 将语言下拉框重置为默认值
        		    comboBox_Baidu_Handwriting_Language.SelectedItem = "中英文混合";
        		}
			}
			else if (tabControl2.SelectedTab == tabPage_腾讯OCR)
			{
				// 同理，判断腾讯的内部子选项卡
				if (tabControl_TXApiType.SelectedTab == inPage_腾讯接口)
				{
					BoxTencentId.Text = "";
					BoxTencentKey.Text = "";
					comboBox_Tencent_Language.SelectedItem = "中英混合";
				}
				else if (tabControl_TXApiType.SelectedTab == inPage_腾讯高精度接口)
				{
					text_tencent_accurate_secretid.Text = "";
					text_tencent_accurate_secretkey.Text = "";
					comboBox_Tencent_Accurate_Language.SelectedItem = "自动检测";
				}
				else if (tabControl_TXApiType.SelectedTab == inPage_腾讯表格v3)
				{
					textBox3.Text = "";
					textBox4.Text = "";
				}
			}
			else if (tabControl2.SelectedTab == tabPage_白描接口)
			{
				// 清空白描账号密码
				BoxBaimiaoUsername.Text = "";
				BoxBaimiaoPassword.Text = "";
			}
		}

		/// <summary>
		/// 文件夹浏览器对话框帮助请求事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 常规设置按钮点击事件处理函数，恢复常规设置的默认值
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void 常规Button_Click(object sender, EventArgs e)
		{
			// 启用开机启动
			cbBox_开机.Checked = true;
			// 启用翻译功能
			cbBox_翻译.Checked = true;
			// 启用弹窗功能
			cbBox_弹窗.Checked = true;
			// 设置动画效果为第一项
			cobBox_动画.SelectedIndex = 0;
			// 设置记录数量为20条
			numbox_记录.Value = 20m;
			// 启用保存功能
			cbBox_保存.Checked = true;
			// 启用路径文本框和浏览按钮
			textBox_path.Enabled = true;
			textBox_path.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			btn_浏览.Enabled = true;
			// 设置默认保存路径为桌面
			textBox_path.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			// 启用保存选项
			chbox_save.Checked = true;
			// 设置音效路径
			text_音效path.Text = "Data\\screenshot.wav";
			// 禁用复制和取色功能
			chbox_copy.Checked = false;
			chbox_取色.Checked = false;
			// 禁用输入翻译剪贴板功能
			cbBox_输入翻译剪贴板.Checked = false;
			cbBox_输入翻译自动翻译.Checked = false;
			checkBox_AutoCopyOcrResult.Checked = false;
			checkBox_AutoTranslateOcrResult.Checked = false;
			checkBox_AutoCopyOcrTranslation.Checked = false;
			checkBox_AutoCopyInputTranslation.Checked = false;
		}

		/// <summary>
		/// 文本框按键抬起事件处理函数，用于设置快捷键
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">按键事件参数</param>
		private void txtBox_KeyUp(object sender, KeyEventArgs e)
		{
			var textBox = sender as TextBox;
			if (textBox == null) return;
			if (!shortcutMappings.TryGetValue(textBox.Name, out string shortcutName)) return;

			var pictureBox = this.Controls.Find("pictureBox_" + shortcutName, true).FirstOrDefault() as PictureBox;
			if (pictureBox == null) return;

			if (e.KeyData == Keys.Back)
			{
				textBox.Text = "请按下快捷键";
				pictureBox.Image = Resources.快捷键_0;
				IniHelper.SetValue("快捷键", shortcutName, textBox.Text);
				return;
			}

			if (e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.Menu)
			{
				var sb = new StringBuilder();
				if (e.Control) sb.Append("Ctrl + ");
				if (e.Shift) sb.Append("Shift + ");
				if (e.Alt) sb.Append("Alt + ");
				sb.Append(e.KeyCode);

				textBox.Text = sb.ToString();
				pictureBox.Image = Resources.快捷键_1;
				IniHelper.SetValue("快捷键", shortcutName, textBox.Text);
			}
		}

		/// <summary>
		/// 处理命令键事件，重写此方法以自定义按键处理逻辑
		/// </summary>
		/// <param name="msg">通过引用传递的Windows消息</param>
		/// <param name="keyData">表示按下的键的Keys值</param>
		/// <returns>如果处理了命令键则返回true，否则返回false</returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			return (keyData == Keys.Tab && txtBox_文字识别.Focused) || (keyData == Keys.Tab && txtBox_翻译文本.Focused) || (keyData == Keys.Tab && txtBox_记录界面.Focused) || (keyData == Keys.Tab && txtBox_识别界面.Focused)
			|| (keyData == Keys.Tab && txtBox_输入翻译.Focused) || (keyData == Keys.Tab && txtBox_静默识别.Focused) || (keyData == Keys.Tab && txtBox_截图翻译.Focused) ;
		}

		/// <summary>
		/// 文本框按键按下事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">按键事件参数</param>
		private void txtBox_KeyDown(object sender, KeyEventArgs e)
		{
			// 阻止按键声音
			e.SuppressKeyPress = true;
		}

		/// <summary>
		/// 快捷键按钮点击事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void 快捷键Button_Click(object sender, EventArgs e)
		{
			new ComponentResourceManager(typeof(FmSetting));
			txtBox_文字识别.Text = "F4";
			pictureBox_文字识别.Image = Resources.快捷键_1;
			txtBox_翻译文本.Text = "F9";
			pictureBox_翻译文本.Image = Resources.快捷键_1;
			txtBox_记录界面.Text = "请按下快捷键";
			pictureBox_记录界面.Image = Resources.快捷键_0;
			txtBox_识别界面.Text = "请按下快捷键";
			pictureBox_识别界面.Image = Resources.快捷键_0;
			txtBox_输入翻译.Text = "请按下快捷键";
			pictureBox_输入翻译.Image = Resources.快捷键_0;
			txtBox_静默识别.Text = "请按下快捷键";
			pictureBox_静默识别.Image = Resources.快捷键_0;
			txtBox_截图翻译.Text = "请按下快捷键";
			pictureBox_截图翻译.Image = Resources.快捷键_0;
		}

		/// <summary>
		/// OCR验证按钮点击事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private async void 百度_btn_Click(object sender, EventArgs e)
		{
			if (tabControl2.SelectedTab == tabPage_百度OCR)
			{
				// 判断内部子选项卡
				if (tabControl_BaiduApiType.SelectedTab == inPage_百度接口)
				{
					if (await BaiduOcrHelper.VerifyKeys(text_baiduaccount.Text, text_baidupassword.Text))
					{
						MessageBox.Show("密钥正确!", "提醒");
					}
					else
					{
						MessageBox.Show("请确保密钥正确!", "提醒");
					}
				}
				else if (tabControl_BaiduApiType.SelectedTab == inPage_百度高精度接口)
				{
					if (await BaiduOcrHelper.VerifyKeys(text_baidu_accurate_apikey.Text, text_baidu_accurate_secretkey.Text))
					{
						MessageBox.Show("密钥正确!", "提醒");
					}
					else
					{
						MessageBox.Show("请确保密钥正确!", "提醒");
					}
				}
				else if (tabControl_BaiduApiType.SelectedTab == inPage_百度表格)
        		{
					if (textBox2.Text != "" || textBox1.Text != "")
					{
						if (await BaiduOcrHelper.VerifyKeys(textBox2.Text, textBox1.Text))
						{
							MessageBox.Show("密钥正确!", "提醒");
						}
						else
						{
							MessageBox.Show("请确保密钥正确!", "提醒");
						}
					}
					else
					{
						MessageBox.Show("使用的百度标准版密钥,请验证标准版密钥是否有效!", "提醒");
					}
				    
        		}
    		}
			else if (tabControl2.SelectedTab == tabPage_白描接口)
			{
				string username = BoxBaimiaoUsername.Text;
				string password = BoxBaimiaoPassword.Text;

				if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
				{
					MessageBox.Show("账号和密码不能为空！", "提醒");
					return;
				}

				// 禁用按钮，防止重复点击
				var button = sender as Button;
				if (button != null)
				{
					button.Enabled = false;
					button.Text = "验证中...";
				}

				try
				{
					// 异步调用白描登录验证
					var loginResult = await OcrHelper.BaimiaoVerifyAccount(username, password);

					if (loginResult != null && loginResult.ContainsKey("code"))
					{
						int code = Convert.ToInt32(loginResult["code"]);
						string message = loginResult.ContainsKey("message") ? loginResult["message"].ToString() : "";
						bool success = loginResult.ContainsKey("success") ? (bool)loginResult["success"] : false;

						// 白描API: code=1 表示成功
						if (code == 1 || success)
						{
							MessageBox.Show("白描账号验证成功！", "验证成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
						else if (code == 0 && message.Contains("密码错误"))
						{
							MessageBox.Show("账号或密码错误！\n\n请确认：\n1. 账号（手机号/邮箱）输入正确\n2. 密码输入正确\n3. 该账号已在白描App或网页版注册", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
						else
						{
							// 显示原始的code和msg
							MessageBox.Show($"验证失败\n\n错误码(code): {code}\n错误信息(msg): {message}", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					}
					else
					{
						MessageBox.Show("验证失败：未收到有效响应\n\n请检查网络连接后重试", "无响应", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				catch (System.Threading.Tasks.TaskCanceledException)
				{
					MessageBox.Show("验证超时，请检查网络连接后重试", "超时", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"验证时发生异常：{ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				finally
				{
					// 恢复按钮状态
					if (button != null)
					{
						button.Enabled = true;
						button.Text = "验证";
					}
				}
			}
			else if (tabControl2.SelectedTab == tabPage_腾讯OCR)
			{
				string secretId, secretKey;
				if (tabControl_TXApiType.SelectedTab == inPage_腾讯接口)
				{
					secretId = BoxTencentId.Text;
					secretKey = BoxTencentKey.Text;
				}
				else if (tabControl_TXApiType.SelectedTab == inPage_腾讯高精度接口)
				{
					secretId = text_tencent_accurate_secretid.Text;
					secretKey = text_tencent_accurate_secretkey.Text;
				}
				else if (tabControl_TXApiType.SelectedTab == inPage_腾讯表格v3)
        		{
					secretId = textBox3.Text;
					secretKey = textBox4.Text;
        		}else
				{
					secretId = "";
					secretKey = "";
				}

				if (string.IsNullOrEmpty(secretId) || string.IsNullOrEmpty(secretKey) || secretId.Contains("secret_id"))
				{
					MessageBox.Show("SecretId 和 SecretKey 不能为空!", "提醒");
					return;
				}

				try
				{
					string jsonResult = TencentOcrHelper.VerifyTencentKey(secretId, secretKey);
					var jObject = Newtonsoft.Json.Linq.JObject.Parse(jsonResult);
					var error = jObject?["Response"]?["Error"];

					if (error == null)
					{
						MessageBox.Show("测试响应异常，未检测到错误信息，请重试。", "未知状态", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						return;
					}

					string code = error["Code"]?.ToString();
					string message = error["Message"]?.ToString();

					if (code.StartsWith("AuthFailure"))
					{
						MessageBox.Show($"密钥验证失败！请确保密钥正确无误且服务已开通。\n\n错误码: {code}\n信息: {message}", "密钥无效", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					else if (code.Contains("InvalidParameter") || code.Contains("MissingParameter"))
					{
						MessageBox.Show("密钥有效，接口可正常访问！", "验证成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						MessageBox.Show($"测试时发生未知API错误。\n\n错误码: {code}\n信息: {message}", "测试失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
				catch (Newtonsoft.Json.JsonReaderException)
				{
					MessageBox.Show("测试失败，无法解析API返回的非JSON格式响应。请检查网络或代理设置。", "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"测试时发生代码异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			//else
			//{
			//	if (Get_html(string.Format("{0}?{1}", "https://aip.baidubce.com/oauth/2.0/token", "grant_type=client_credentials&client_id=" + text_baiduaccount.Text + "&client_secret=" + text_baidupassword.Text)) != "")
			//	{
			//		MessageBox.Show("密钥正确!", "提醒");
			//		return;
			//	}
			//	MessageBox.Show("请确保密钥正确!", "提醒");
			//}
		}

		/// <summary>
		/// 代理下拉框选项改变事件处理函数
		/// 当用户在代理下拉框中选择不同选项时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void combox_代理_SelectedIndexChanged(object sender, EventArgs e)
		{
			// 当选择"不使用代理"或"系统代理"时，禁用相关输入框并清空内容
			if (combox_代理.Text == "不使用代理" || combox_代理.Text == "系统代理")
			{
				text_账号.Enabled = false;
				text_密码.Enabled = false;
				chbox_代理服务器.Enabled = false;
				text_端口.Enabled = false;
				chbox_代理服务器.Checked = false;
				text_服务器.Enabled = false;
				text_服务器.Text = "";
				text_端口.Text = "";
				text_服务器.Text = "";
				text_账号.Text = "";
				text_密码.Text = "";
			}
			// 当选择"自定义代理"时，启用相关输入框
			if (combox_代理.Text == "自定义代理")
			{
				text_端口.Enabled = true;
				text_服务器.Enabled = true;
				chbox_代理服务器.Enabled = true;
			}
		}

		/// <summary>
		/// 端口文本框输入拒绝事件处理函数
		/// 当端口文本框拒绝输入时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">掩码输入拒绝事件参数</param>
		private void text_端口_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
		{
		}

		/// <summary>
		/// 百度账号文本框文本改变事件处理函数
		/// 当百度账号文本框内容发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void text_baiduaccount_TextChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 百度密码文本框文本改变事件处理函数
		/// 当百度密码文本框内容发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void text_baidupassword_TextChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 服务器文本框文本改变事件处理函数
		/// 当服务器文本框内容发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void text_服务器_TextChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 端口文本框文本改变事件处理函数
		/// 当端口文本框内容发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void text_端口_TextChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 代理服务器复选框状态改变事件处理函数
		/// 当代理服务器复选框选中状态发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void chbox_代理服务器_CheckedChanged(object sender, EventArgs e)
		{
			// 当复选框被选中时，启用账号和密码输入框
			if (chbox_代理服务器.Checked)
			{
				text_账号.Enabled = true;
				text_密码.Enabled = true;
			}
			// 当复选框未被选中时，禁用账号和密码输入框
			if (!chbox_代理服务器.Checked)
			{
				text_账号.Enabled = false;
				text_密码.Enabled = false;
			}
		}

		/// <summary>
		/// 账号文本框文本改变事件处理函数
		/// 当账号文本框内容发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void text_账号_TextChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 密码文本框文本改变事件处理函数
		/// 当密码文本框内容发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void text_密码_TextChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 代理按钮点击事件处理函数
		/// 当用户点击代理设置按钮时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void 代理Button_Click(object sender, EventArgs e)
		{
			combox_代理.Text = "系统代理";
			text_账号.Enabled = false;
			text_密码.Enabled = false;
			chbox_代理服务器.Enabled = false;
			text_端口.Enabled = false;
			text_服务器.Enabled = false;
		}
        /// <summary>
        /// 【新增】处理“智能去除空格”复选框的互斥逻辑
        /// </summary>
        private void checkBox_合并去除空格_CheckedChanged(object sender, EventArgs e)
        {
            // 只有当用户“勾选”此项时，才触发互斥
            if (checkBox_合并去除空格.Checked)
            {
                // 移除另一个事件的监听，以防止循环触发
                this.checkBox_合并去除所有空格.CheckedChanged -= this.checkBox_合并去除所有空格_CheckedChanged;

                // 取消“去除所有空格”的勾选
                checkBox_合并去除所有空格.Checked = false;

                // 重新订阅事件
                this.checkBox_合并去除所有空格.CheckedChanged += new System.EventHandler(this.checkBox_合并去除所有空格_CheckedChanged);
            }
        }

        /// <summary>
        /// 【新增】处理“去除所有空格”复选框的互斥逻辑
        /// </summary>
        private void checkBox_合并去除所有空格_CheckedChanged(object sender, EventArgs e)
        {
            // 只有当用户“勾选”此项时，才触发互斥
            if (checkBox_合并去除所有空格.Checked)
            {
                // 移除另一个事件的监听，以防止循环触发
                this.checkBox_合并去除空格.CheckedChanged -= this.checkBox_合并去除空格_CheckedChanged;

                // 取消“智能去除空格”的勾选
                checkBox_合并去除空格.Checked = false;

                // 重新订阅事件
                this.checkBox_合并去除空格.CheckedChanged += new System.EventHandler(this.checkBox_合并去除空格_CheckedChanged);
            }
        }

        /// <summary>
        /// 检查更新复选框状态改变事件处理函数
        /// 当检查更新复选框选中状态发生改变时触发此事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void check_检查更新_CheckedChanged(object sender, EventArgs e)
		{
			// 当复选框被选中时，启用更新间隔相关控件
			if (check_检查更新.Checked)
			{
				checkBox_更新间隔.Enabled = true;
				checkBox_更新间隔.Checked = true;
				numbox_间隔时间.Enabled = true;
			}
			// 当复选框未被选中时，禁用更新间隔相关控件
			if (!check_检查更新.Checked)
			{
				checkBox_更新间隔.Checked = false;
				checkBox_更新间隔.Enabled = false;
				numbox_间隔时间.Enabled = false;
			}
		}

		/// <summary>
		/// 更新间隔复选框状态改变事件处理函数
		/// 当更新间隔复选框选中状态发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void checkBox_更新间隔_CheckedChanged(object sender, EventArgs e)
		{
			// 当复选框被选中时，启用间隔时间数字框
			if (checkBox_更新间隔.Checked)
			{
				numbox_间隔时间.Enabled = true;
			}
			// 当复选框未被选中时，禁用间隔时间数字框
			if (!checkBox_更新间隔.Checked)
			{
				numbox_间隔时间.Enabled = false;
			}
		}

		/// <summary>
		/// 间隔时间数字框值改变事件处理函数
		/// 当间隔时间数字框的值发生改变时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void numbox_间隔时间_ValueChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 更新设置页的恢复默认按钮点击事件处理函数
		/// 当用户点击更新设置的恢复默认按钮时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void 更新Button_Click(object sender, EventArgs e)
		{
			numbox_间隔时间.Value = 24m;
			check_检查更新.Checked = true;
			checkBox_更新间隔.Checked = true;
			checkBox_PreRelease.Checked = false;
		}

		/// <summary>
		/// 更新检查按钮点击事件处理函数
		/// 当用户点击检查更新按钮时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void 更新Button_check_Click(object sender, EventArgs e)
		{
			new Thread(Program.CheckUpdate).Start();
		}

		/// <summary>
		/// 反馈按钮点击事件处理函数
		/// 当用户点击反馈按钮时触发此事件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void 反馈Button_Click(object sender, EventArgs e)
		{
			new Thread(反馈send).Start();
		}

		/// <summary>
		/// 发送POST请求并获取HTML响应内容
		/// </summary>
		/// <param name="url">请求的目标URL地址</param>
		/// <param name="post_str">POST请求的数据字符串</param>
		/// <returns>服务器返回的HTML响应内容</returns>
		public string Post_Html(string url, string post_str)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 6000;
			httpWebRequest.Proxy = null;
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		// 窗口关闭事件处理函数，保存所有设置到配置文件
		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			// 保存基本配置
			IniHelper.SetValue("配置", "开机自启", cbBox_开机.Checked.ToString());
			IniHelper.SetValue("配置", "快速翻译", cbBox_翻译.Checked.ToString());
			IniHelper.SetValue("配置", "识别弹窗", cbBox_弹窗.Checked.ToString());
			IniHelper.SetValue("配置", "InputTranslateClipboard", cbBox_输入翻译剪贴板.Checked.ToString());
			IniHelper.SetValue("配置", "InputTranslateAutoTranslate", cbBox_输入翻译自动翻译.Checked.ToString());
			IniHelper.SetValue("配置", "AutoCopyInputTranslation", checkBox_AutoCopyInputTranslation.Checked.ToString());
			IniHelper.SetValue("配置", "窗体动画", cobBox_动画.Text);
			IniHelper.SetValue("配置", "记录数目", numbox_记录.Text);
			IniHelper.SetValue("配置", "自动保存", cbBox_保存.Checked.ToString());
			IniHelper.SetValue("配置", "截图位置", textBox_path.Text);
			IniHelper.SetValue("常规识别", "AutoCopyOcrResult", checkBox_AutoCopyOcrResult.Checked.ToString());
			IniHelper.SetValue("工具栏", "翻译", checkBox_AutoTranslateOcrResult.Checked.ToString());
			IniHelper.SetValue("常规翻译", "AutoCopyOcrTranslation", checkBox_AutoCopyOcrTranslation.Checked.ToString());

			IniHelper.SetValue("工具栏", "IsMergeRemoveSpace", checkBox_合并去除空格.Checked.ToString());
            IniHelper.SetValue("工具栏", "IsMergeRemoveAllSpace", checkBox_合并去除所有空格.Checked.ToString());
            IniHelper.SetValue("工具栏", "IsMergeAutoCopy", checkBox_合并自动复制.Checked.ToString());
			IniHelper.SetValue("工具栏", "IsSplitAutoCopy", checkBox_拆分后自动复制.Checked.ToString());
			
			// 保存快捷键设置
			IniHelper.SetValue("快捷键", "文字识别", txtBox_文字识别.Text);
			IniHelper.SetValue("快捷键", "翻译文本", txtBox_翻译文本.Text);
			IniHelper.SetValue("快捷键", "记录界面", txtBox_记录界面.Text);
			IniHelper.SetValue("快捷键", "识别界面", txtBox_识别界面.Text);
			IniHelper.SetValue("快捷键", "输入翻译", txtBox_输入翻译.Text);
			IniHelper.SetValue("快捷键", "静默识别", txtBox_静默识别.Text);
			IniHelper.SetValue("快捷键", "截图翻译", txtBox_截图翻译.Text);

			// 【新增】保存剪贴板相关的设置
			IniHelper.SetValue("配置", "ListenClipboard", cbBox_ListenClipboard.Checked.ToString());
			IniHelper.SetValue("配置", "AutoCopyListenClipboardTranslation", cbBox_AutoCopyListenClipboardTranslation.Checked.ToString());
			IniHelper.SetValue("配置", "ListenClipboardTranslationHideOriginal", cbBox_ListenHideOriginal.Checked.ToString());
			IniHelper.SetValue("配置", "DisableToggleOriginalButton", cbBox_禁用隐藏原文按钮.Checked.ToString());
			
			//保存截图翻译相关配置
			IniHelper.SetValue("配置", "AutoCopyScreenshotTranslation", checkbox_AutoCopyScreenshotTranslation.Checked.ToString());
			IniHelper.SetValue("配置", "NoWindowScreenshotTranslation", checkbox_NoWindowScreenshotTranslation.Checked.ToString());
			// 保存百度OCR密钥和语言设置
			IniHelper.SetValue("密钥_百度", "secret_id", text_baiduaccount.Text);
			IniHelper.SetValue("密钥_百度", "secret_key", text_baidupassword.Text);
			var selectedLang = comboBox_Baidu_Language.SelectedItem?.ToString();
			var langCode = BaiduOcrHelper.GetStandardLanguages().FirstOrDefault(x => x.Value == selectedLang).Key;
			IniHelper.SetValue("密钥_百度", "language_code", langCode ?? "CHN_ENG");

			// 保存百度高精度OCR密钥和语言设置
			IniHelper.SetValue("密钥_百度高精度", "secret_id", text_baidu_accurate_apikey.Text);
			IniHelper.SetValue("密钥_百度高精度", "secret_key", text_baidu_accurate_secretkey.Text);
			var selectedAccurateLang = comboBox_Baidu_Accurate_Language.SelectedItem?.ToString();
			var accurateLangCode = BaiduOcrHelper.GetAccurateLanguages().FirstOrDefault(x => x.Value == selectedAccurateLang).Key;
			IniHelper.SetValue("密钥_百度高精度", "language_code", accurateLangCode ?? "CHN_ENG");

			// 保存百度表格识别密钥
			IniHelper.SetValue("密钥_百度表格", "secret_id", textBox2.Text);
			IniHelper.SetValue("密钥_百度表格", "secret_key", textBox1.Text);

			// 【新增】保存百度手写识别密钥
			IniHelper.SetValue("密钥_百度手写", "secret_id", text_baidu_handwriting_apikey.Text);
			IniHelper.SetValue("密钥_百度手写", "secret_key", text_baidu_handwriting_secretkey.Text);
			// 【新增】保存百度手写识别语言设置
			var selectedHandwritingLang = comboBox_Baidu_Handwriting_Language.SelectedItem?.ToString();
			var handwritingLangCode = BaiduOcrHelper.GetAccurateLanguages().FirstOrDefault(x => x.Value == selectedHandwritingLang).Key;
			IniHelper.SetValue("密钥_百度手写", "language_code", handwritingLangCode ?? "CHN_ENG");

			// 保存腾讯OCR密钥和语言设置
			IniHelper.SetValue("密钥_腾讯", "secret_id", BoxTencentId.Text);
			IniHelper.SetValue("密钥_腾讯", "secret_key", BoxTencentKey.Text);
			var selectedTencentLang = comboBox_Tencent_Language.SelectedItem?.ToString();
			var tencentLangCode = TencentOcrHelper.GetStandardLanguages().FirstOrDefault(x => x.Value == selectedTencentLang).Key;
			IniHelper.SetValue("密钥_腾讯", "language_code", tencentLangCode ?? "zh");

			// 保存腾讯高精度OCR密钥和语言设置
			IniHelper.SetValue("密钥_腾讯高精度", "secret_id", text_tencent_accurate_secretid.Text);
			IniHelper.SetValue("密钥_腾讯高精度", "secret_key", text_tencent_accurate_secretkey.Text);
			var selectedTencentAccurateLang = comboBox_Tencent_Accurate_Language.SelectedItem?.ToString();
			var tencentAccurateLangCode = TencentOcrHelper.GetAccurateLanguages().FirstOrDefault(x => x.Value == selectedTencentAccurateLang).Key;
			IniHelper.SetValue("密钥_腾讯高精度", "language_code", tencentAccurateLangCode ?? "auto");

			// 保存腾讯表格API密钥信息
			IniHelper.SetValue("密钥_腾讯表格v3", "secret_id", textBox3.Text);
			IniHelper.SetValue("密钥_腾讯表格v3", "secret_key", textBox4.Text);

			// 保存白描OCR账号信息
			IniHelper.SetValue("密钥_白描", "username", BoxBaimiaoUsername.Text);
			IniHelper.SetValue("密钥_白描", "password", BoxBaimiaoPassword.Text);
			
			// 保存代理设置
			IniHelper.SetValue("代理", "代理类型", combox_代理.Text);
			IniHelper.SetValue("代理", "服务器", text_服务器.Text);
			IniHelper.SetValue("代理", "端口", text_端口.Text);
			IniHelper.SetValue("代理", "需要密码", chbox_代理服务器.Checked.ToString());
			IniHelper.SetValue("代理", "服务器账号", text_账号.Text);
			IniHelper.SetValue("代理", "服务器密码", text_密码.Text);
			
			// 保存更新设置
			IniHelper.SetValue("更新", "检测更新", check_检查更新.Checked.ToString());
			IniHelper.SetValue("更新", "更新间隔", checkBox_更新间隔.Checked.ToString());
			IniHelper.SetValue("更新", "间隔时间", numbox_间隔时间.Value.ToString());
			IniHelper.SetValue("更新", "CheckPreRelease", checkBox_PreRelease.Checked.ToString());
			
			// 保存截图音效设置
			IniHelper.SetValue("截图音效", "自动保存", chbox_save.Checked.ToString());
			IniHelper.SetValue("截图音效", "音效路径", text_音效path.Text);
			IniHelper.SetValue("截图音效", "粘贴板", chbox_copy.Checked.ToString());
			
			// 保存取色器设置
			if (!chbox_取色.Checked)
			{
				IniHelper.SetValue("取色器", "类型", "RGB");
			}
			if (chbox_取色.Checked)
			{
				IniHelper.SetValue("取色器", "类型", "HEX");
			}
			
			// 保存各翻译接口设置
			IniHelper.SetValue("Translate_Google", "Source", textBox_Google_Source.Text);
			IniHelper.SetValue("Translate_Google", "Target", textBox_Google_Target.Text);
			         
			IniHelper.SetValue("Translate_Baidu", "Source", textBox_Baidu_Source.Text);
			IniHelper.SetValue("Translate_Baidu", "Target", textBox_Baidu_Target.Text);
			IniHelper.SetValue("Translate_Baidu", "APP_ID", textBox_Baidu_AK.Text);
			IniHelper.SetValue("Translate_Baidu", "APP_KEY", textBox_Baidu_SK.Text);
	
			IniHelper.SetValue("Translate_Tencent", "Source", textBox_Tencent_Source.Text);
			IniHelper.SetValue("Translate_Tencent", "Target", textBox_Tencent_Target.Text);
			IniHelper.SetValue("Translate_Tencent", "SecretId", textBox_Tencent_AK.Text);
			IniHelper.SetValue("Translate_Tencent", "SecretKey", textBox_Tencent_SK.Text);

			IniHelper.SetValue("Translate_Bing", "Source", textBox_Bing_Source.Text);
			IniHelper.SetValue("Translate_Bing", "Target", textBox_Bing_Target.Text);
			
			IniHelper.SetValue("Translate_Bing2", "Source", textBox_Bing2_Source.Text);
			IniHelper.SetValue("Translate_Bing2", "Target", textBox_Bing2_Target.Text);
			
			IniHelper.SetValue("Translate_Microsoft", "Source", textBox_Microsoft_Source.Text);
			IniHelper.SetValue("Translate_Microsoft", "Target", textBox_Microsoft_Target.Text);

			IniHelper.SetValue("Translate_Yandex", "Source", textBox_Yandex_Source.Text);
			IniHelper.SetValue("Translate_Yandex", "Target", textBox_Yandex_Target.Text);
			         
			// 腾讯交互翻译
			IniHelper.SetValue("Translate_TencentInteractive", "Source", textBox_TencentInteractive_Source.Text);
			IniHelper.SetValue("Translate_TencentInteractive", "Target", textBox_TencentInteractive_Target.Text);
			         
			// 彩云小译
			IniHelper.SetValue("Translate_Caiyun", "Source", textBox_Caiyun_Source.Text);
			IniHelper.SetValue("Translate_Caiyun", "Target", textBox_Caiyun_Target.Text);
			         
			// 火山翻译
			IniHelper.SetValue("Translate_Volcano", "Source", textBox_Volcano_Source.Text);
			IniHelper.SetValue("Translate_Volcano", "Target", textBox_Volcano_Target.Text);

			//百度翻译2
			IniHelper.SetValue("Translate_Baidu2", "Source", textBox_Baidu2_Source.Text);
			IniHelper.SetValue("Translate_Baidu2", "Target", textBox_Baidu2_Target.Text);

			// 彩云小译2
			IniHelper.SetValue("Translate_Caiyun2", "Source", textBox_Caiyun2_Source.Text);
			IniHelper.SetValue("Translate_Caiyun2", "Target", textBox_Caiyun2_Target.Text);
			IniHelper.SetValue("Translate_Caiyun2", "Token", textBox_Caiyun2_Token.Text);

			// 保存翻译接口显示设置
			IniHelper.SetValue("翻译接口显示", "Google", checkBox_ShowGoogle.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Baidu", checkBox_ShowBaidu.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Tencent", checkBox_ShowTencent.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Bing", checkBox_ShowBing.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Bing2", checkBox_ShowBing2.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Microsoft", checkBox_ShowMicrosoft.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Yandex", checkBox_ShowYandex.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "TencentInteractive", checkBox_ShowTencentInteractive.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Caiyun", checkBox_ShowCaiyun.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Volcano", checkBox_ShowVolcano.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Caiyun2", checkBox_ShowCaiyun2.Checked.ToString());
			IniHelper.SetValue("翻译接口显示", "Baidu2", checkBox_ShowBaidu2.Checked.ToString());

			// 保存OCR接口显示设置
			IniHelper.SetValue("Ocr接口显示", "Baidu", checkBox_ShowOcrBaidu.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "BaiduAccurate", checkBox_ShowOcrBaiduAccurate.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "Tencent", checkBox_ShowOcrTencent.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "TencentAccurate", checkBox_ShowOcrTencentAccurate.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "Baimiao", checkBox_ShowOcrBaimiao.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "Sougou", checkBox_ShowOcrSougou.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "Youdao", checkBox_ShowOcrYoudao.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "WeChat", checkBox_ShowOcrWeChat.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "Mathfuntion", checkBox_ShowOcrMathfuntion.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "Table", checkBox_ShowOcrTable.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "Shupai", checkBox_ShowOcrShupai.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "TableBaidu", checkBox_ShowOcrTableBaidu.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "TableAli", checkBox_ShowOcrTableAli.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "ShupaiLR", checkBox_ShowOcrShupaiLR.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "ShupaiRL", checkBox_ShowOcrShupaiRL.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "TencentTable", checkBox_ShowOcrTableTencent.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "PaddleOCR", checkBox_ShowOcrPaddleOCR.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "PaddleOCR2", checkBox_ShowOcrPaddleOCR2.Checked.ToString());
			IniHelper.SetValue("Ocr接口显示", "RapidOCR", checkBox_ShowOcrRapidOCR.Checked.ToString());

			// 保存OCR模型配置
			// PaddleOCR配置
			IniHelper.SetValue("模型配置_PaddleOCR", "Det", textBox_PaddleOCR_Det.Text);
			IniHelper.SetValue("模型配置_PaddleOCR", "Cls", textBox_PaddleOCR_Cls.Text);
			IniHelper.SetValue("模型配置_PaddleOCR", "Rec", textBox_PaddleOCR_Rec.Text);
			IniHelper.SetValue("模型配置_PaddleOCR", "Keys", textBox_PaddleOCR_Keys.Text);
			IniHelper.SetValue("模型配置_PaddleOCR", "AdvancedConfig", textBox5.Text);
			
			
			// PaddleOCR2配置
			IniHelper.SetValue("模型配置_PaddleOCR2", "Det", textBox_PaddleOCR2_Det.Text);
			IniHelper.SetValue("模型配置_PaddleOCR2", "Cls", textBox_PaddleOCR2_Cls.Text);
			IniHelper.SetValue("模型配置_PaddleOCR2", "Rec", textBox_PaddleOCR2_Rec.Text);
			IniHelper.SetValue("模型配置_PaddleOCR2", "Keys", textBox_PaddleOCR2_Keys.Text);
			IniHelper.SetValue("模型配置_PaddleOCR2", "AdvancedConfig", textBox6.Text);
			IniHelper.SetValue("模型配置_PaddleOCR2", "Det_Version", comboBox_PaddleOCR2_det_Version.SelectedItem?.ToString() ?? "v5");
			IniHelper.SetValue("模型配置_PaddleOCR2", "Cls_Version", comboBox_PaddleOCR2_cls_Version.SelectedItem?.ToString() ?? "v5");
			IniHelper.SetValue("模型配置_PaddleOCR2", "Rec_Version", comboBox_PaddleOCR2_rec_Version.SelectedItem?.ToString() ?? "v5");
			
			// RapidOCR配置
			IniHelper.SetValue("模型配置_RapidOCR", "Det", textBox_RapidOCR_Det.Text);
			IniHelper.SetValue("模型配置_RapidOCR", "Cls", textBox_RapidOCR_Cls.Text);
			IniHelper.SetValue("模型配置_RapidOCR", "Rec", textBox_RapidOCR_Rec.Text);
			IniHelper.SetValue("模型配置_RapidOCR", "Keys", textBox_RapidOCR_Keys.Text);
			IniHelper.SetValue("模型配置_RapidOCR", "AdvancedConfig", textBox7.Text);

			ResetOcrEngineOnConfigChange();
			DialogResult = DialogResult.OK;
			StaticValue.LoadConfig();
		}

		/// <summary>
		/// 设置程序是否开机自动启动
		/// </summary>
		/// <param name="isAuto">true表示设置为开机自启，false表示取消开机自启</param>
		public static void AutoStart(bool isAuto)
		{
			try
			{
				// 获取当前程序路径并格式化
				var value = Application.ExecutablePath.Replace("/", "\\");
				if (isAuto)
				{
					// 在注册表中添加开机启动项
					var currentUser = Registry.CurrentUser;
					var registryKey = currentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
					registryKey.SetValue("tianruoOCR", value);
					registryKey.Close();
					currentUser.Close();
				}
				else
				{
					// 从注册表中删除开机启动项
					var currentUser2 = Registry.CurrentUser;
					var registryKey2 = currentUser2.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
					registryKey2.DeleteValue("tianruoOCR", false);
					registryKey2.Close();
					currentUser2.Close();
				}
			}
			catch (Exception)
			{
				// 权限不足时提示用户
				MessageBox.Show("您需要管理员权限修改", "提示");
			}
		}

		/// <summary>
		/// 发送用户反馈信息到服务器
		/// </summary>
		private void 反馈send()
		{
			// 检查反馈内容是否为空
			if (string.IsNullOrEmpty(txt_问题反馈.Text))
			{
                CommonHelper.ShowHelpMsg("反馈文本不能为空");
                return;
			}
            // 构造请求参数并发送反馈
            var str = "sm=%E5%A4%A9%E8%8B%A5OCR%E6%96%87%E5%AD%97%E8%AF%86%E5%88%AB" + StaticValue.CurrentVersion + "&nr=";
            Post_Html("http://cd.ys168.com/f_ht/ajcx/lyd.aspx?cz=lytj&pdgk=1&pdgly=0&pdzd=0&tou=1&yzm=undefined&_dlmc=tianruoyouxin&_dlmm=", str + HttpUtility.UrlEncode(txt_问题反馈.Text));
            txt_问题反馈.Text = "";
            CommonHelper.ShowHelpMsg("感谢您的反馈！");
		}

		/// <summary>
		/// 播放指定的音频文件
		/// </summary>
		/// <param name="file">要播放的音频文件路径</param>
		public void PlaySong(string file)
		{
			HelpWin32.mciSendString("close media", null, 0, IntPtr.Zero);
			HelpWin32.mciSendString("open \"" + file + "\" type mpegvideo alias media", null, 0, IntPtr.Zero);
			HelpWin32.mciSendString("play media notify", null, 0, Handle);
		}

		/// <summary>
		/// 音效按钮点击事件处理函数，播放音效文件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void btn_音效_Click(object sender, EventArgs e)
		{
			PlaySong(text_音效path.Text);
		}

		/// <summary>
		/// 音效路径选择按钮点击事件处理函数，打开文件选择对话框选择音效文件
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void btn_音效路径_Click(object sender, EventArgs e)
		{
			var openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "请选择音效文件";
			openFileDialog.Filter = "All files（*.*）|*.*|All files(*.*)|*.* ";
			openFileDialog.RestoreDirectory = true;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				text_音效path.Text = Path.GetFullPath(openFileDialog.FileName);
			}
		}

		/// <summary>
		/// 复制到剪贴板复选框状态改变事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void chbox_copy_CheckedChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 截图自动保存复选框状态改变事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void chbox_save_CheckedChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 取色器类型复选框状态改变事件处理函数
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void chbox_取色_CheckedChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// 重置指定翻译服务的源语言设置为默认值
		/// </summary>
		/// <param name="sender">触发事件的按钮控件</param>
		/// <param name="e">事件参数</param>
		private void btn_Reset_Source_Click(object sender, EventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;

			// 从按钮名称 "btn_Reset_Google_Source" 中提取 "Google"
			string serviceName = button.Name.Split('_')[2];
    
			// 构建目标TextBox的名称 "textBox_Google_Source"
			string textBoxName = $"textBox_{serviceName}_Source";

			// 在窗体中递归查找该TextBox控件
			var textBox = this.Controls.Find(textBoxName, true).FirstOrDefault() as TextBox;

			if (textBox != null)
			{
				textBox.Text = "auto";
				string sectionName = $"Translate_{serviceName}";
				IniHelper.SetValue(sectionName, "Source", "auto");
			}
		}

		/// <summary>
		/// 重置指定翻译服务的目标语言设置为默认值
		/// </summary>
		/// <param name="sender">触发事件的按钮控件</param>
		/// <param name="e">事件参数</param>
		private void btn_Reset_Target_Click(object sender, EventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;

			// 从按钮名称 "btn_Reset_Google_Target" 中提取 "Google"
			string serviceName = button.Name.Split('_')[2];
				
			// 构建目标TextBox的名称 "textBox_Google_Target"
			string textBoxName = $"textBox_{serviceName}_Target";

			// 在窗体中递归查找该TextBox控件
			var textBox = this.Controls.Find(textBoxName, true).FirstOrDefault() as TextBox;

			if (textBox != null)
			{
				textBox.Text = "自动判断";
				string sectionName = $"Translate_{serviceName}";
				IniHelper.SetValue(sectionName, "Target", "自动判断");
			}
		}
		/// <summary>
		/// 设置属性，用于设置开始选项卡索引为5
		/// </summary>
		public string Start_set
		{
			set
			{
				tab_标签.SelectedIndex = 5;
			}
		}

		/// <summary>
		/// 存储控件的原始位置信息
		/// </summary>
		/// <param name="container">需要存储控件位置的容器控件</param>
		private void StoreOriginalLocations(Control container)
		{
		    if (_originalControlLocations == null)
		    {
		        _originalControlLocations = new Dictionary<Control, Point>();
		    }
		    foreach (Control control in container.Controls)
		    {
		        if (!_originalControlLocations.ContainsKey(control))
		        {
		            _originalControlLocations[control] = control.Location;
		        }
		        if (control.Controls.Count > 0)
		        {
		            StoreOriginalLocations(control);
		        }
		    }
		}

		/// <summary>
		/// 重置控件到原始位置
		/// </summary>
		private void ResetControlLocations()
		{
		    if (_originalControlLocations == null) return;
		    foreach (var entry in _originalControlLocations)
		    {
		        entry.Key.Location = entry.Value;
		    }
		}

		/// <summary>
		/// 根据选中的选项卡调整页面大小
		/// </summary>
		/// <param name="sender">事件发送者</param>
		/// <param name="e">事件参数</param>
		private void AdjustPageSize(object sender, EventArgs e)
		{
		    ResetControlLocations();

			// Determine which TabPage is ultimately visible.
			TabPage selectedPage = tab_标签.SelectedTab;

			int newHeight;
    		
        	if (selectedPage == Page_密钥)
        	{
        		selectedPage = tabControl2.SelectedTab;
        	}
        	else if (selectedPage == Page_翻译接口)
        	{
        	    selectedPage = tabControl_Trans.SelectedTab;
        	}
	
        	if (selectedPage == null) return;

			// Calculate the bottom-most point of any visible control on that page.
			int maxBottom = 0;
        	var visibleControls = selectedPage.Controls.OfType<Control>().Where(c => c.Visible).ToList();
        	if (visibleControls.Any())
        	{
        	    maxBottom = visibleControls.Max(c => c.Bottom);
        	}
			int requiredContentHeight; 

			if (selectedPage == page_常规)
			{
				requiredContentHeight = maxBottom + 80; // Add 80px padding.

			} else {
				// This is the total height required for the content within the main TabControl area.
			 	requiredContentHeight = maxBottom + 40; // Add 40px padding.
			}
			

        	// The total form height is the position of the main TabControl plus the content height.
        	int requiredFormHeight = tab_标签.Top + requiredContentHeight;
	
        	const int minFormHeight = 435;
        	newHeight = Math.Max(requiredFormHeight, minFormHeight);

		    // Apply DPI scaling and update the form's client size.
		    int scaledHeight = (int)(newHeight / StaticValue.DpiFactor);
		    if (this.ClientSize.Height != scaledHeight)
		    {
		         this.ClientSize = new System.Drawing.Size(this.ClientSize.Width, scaledHeight);
		    }
		}

		/// <summary>
		/// 检查接口可见性复选框的状态变化
		/// </summary>
		private void ApiVisibility_CheckedChanged(object sender, EventArgs e)
		{
		 CheckBox checkBox = sender as CheckBox;
		 if (checkBox == null || checkBox.Checked)
		 {
		  return; // 只在取消勾选时处理
		 }

		 string currentOcrApi = IniHelper.GetValue("配置", "接口");
		 string currentTranslateApi = IniHelper.GetValue("配置", "翻译接口");

		 bool isInUse = false;

		 // OCR API 检查
		 if (checkBox == checkBox_ShowOcrBaidu && (new[] { "中英", "日语", "韩语" }).Contains(currentOcrApi)) isInUse = true;
		 else if (checkBox == checkBox_ShowOcrBaiduAccurate && currentOcrApi == "百度-高精度") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrTencent && currentOcrApi == "腾讯") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrTencentAccurate && currentOcrApi == "腾讯-高精度") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrBaimiao && currentOcrApi == "白描") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrSougou && currentOcrApi == "搜狗") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrYoudao && currentOcrApi == "有道") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrWeChat && currentOcrApi == "微信") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrMathfuntion && currentOcrApi == "公式") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrTable && (new[] { "百度表格", "阿里表格" }).Contains(currentOcrApi)) isInUse = true;
		 else if (checkBox == checkBox_ShowOcrShupai && (new[] { "从左向右", "从右向左" }).Contains(currentOcrApi)) isInUse = true;
		 else if (checkBox == checkBox_ShowOcrTableBaidu && currentOcrApi == "百度表格") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrTableAli && currentOcrApi == "阿里表格") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrTableTencent && currentOcrApi == "腾讯表格") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrShupaiLR && currentOcrApi == "从左向右") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrShupaiRL && currentOcrApi == "从右向左") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrPaddleOCR && currentOcrApi == "PaddleOCR") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrPaddleOCR2 && currentOcrApi == "PaddleOCR2") isInUse = true;
		 else if (checkBox == checkBox_ShowOcrRapidOCR && currentOcrApi == "RapidOCR") isInUse = true;
			// 翻译 API 检查
			else if (checkBox == checkBox_ShowGoogle && currentTranslateApi == "谷歌") isInUse = true;
			else if (checkBox == checkBox_ShowBaidu && currentTranslateApi == "百度") isInUse = true;
			else if (checkBox == checkBox_ShowTencent && currentTranslateApi == "腾讯") isInUse = true;
			else if (checkBox == checkBox_ShowBing && currentTranslateApi == "Bing") isInUse = true;
			else if (checkBox == checkBox_ShowBing2 && currentTranslateApi == "Bing2") isInUse = true;
			else if (checkBox == checkBox_ShowMicrosoft && currentTranslateApi == "Microsoft") isInUse = true;
			else if (checkBox == checkBox_ShowYandex && currentTranslateApi == "Yandex") isInUse = true;
			else if (checkBox == checkBox_ShowTencentInteractive && currentTranslateApi == "腾讯交互翻译") isInUse = true;
			else if (checkBox == checkBox_ShowCaiyun && currentTranslateApi == "彩云小译") isInUse = true;
			else if (checkBox == checkBox_ShowVolcano && currentTranslateApi == "火山翻译") isInUse = true;
			else if (checkBox == checkBox_ShowCaiyun2 && currentTranslateApi == "彩云小译2") isInUse = true;
			else if (checkBox == checkBox_ShowBaidu2 && currentTranslateApi == "百度2") isInUse = true;
		//如果接口正在使用，弹出提示
		 if (isInUse)
		 {
		  MessageBox.Show("该接口正在使用中，不能隐藏。", "提示", MessageBoxButtons.OK, MessageBoxIcon.None);
		  // 重新勾选复选框，并临时移除事件处理程序以避免无限循环
		  checkBox.CheckedChanged -= ApiVisibility_CheckedChanged;
		  checkBox.Checked = true;
		  checkBox.CheckedChanged += ApiVisibility_CheckedChanged;
		 }
		}

        private async void button1_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // 禁用按钮，防止重复点击，并提示用户
            button.Enabled = false;
            button.Text = "测试中...";

            try
            {
                string testImagePath = "test.png";

                // 检查测试文件是否存在
                if (!File.Exists(testImagePath))
                {
                    MessageBox.Show($"测试图片 '{testImagePath}' 未找到！\n\n请将一张图片放在程序的运行目录下（和.exe文件一起），并重命名为 test.png。", "测试文件缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string resultText = "";
                // 使用 Task.Run 将耗时的OCR操作放到后台线程，避免UI卡死
                //await Task.Run(() =>
                //{
                //	// 使用 using 语句确保从文件加载的图片资源能被正确释放
                //	using (var localImage = new Bitmap(testImagePath))
                //	{
                //        // 这里我们直接调用我们之前优化好的静态PaddleOCRHelper
                //        // 它内部已经处理了引擎的创建、使用和销毁
                //        // resultText = PaddleOCRHelper.RecognizeText(localImage);

                        
                //    }
                //});
                await Task.Run(async() =>
                {
                    // 使用 using 语句确保从文件加载的图片资源能被正确释放
                    using (var localImage = new Bitmap(testImagePath))
                    {
                        //  使用 await 关键字来异步等待识别结果
                        // 调用 RecognizeTextAsync 方法，它会返回一个 Task<string>
                        // await 会“暂停”这个方法，直到后台线程完成识别并将结果返回，期间UI不会卡死
                        //resultText = await PaddleOCRHelper.Instance.RecognizeTextAsync(localImage);

                    }
                });
                //// 使用 Task.Run 将耗时的OCR操作放到后台线程，避免UI卡死
                //await Task.Run(() =>
                //{
                //	// 使用 using 语句确保从文件加载的图片资源能被正确释放
                //	using (var localImage = new Bitmap(testImagePath))
                //	{
                //		// --- 这里是修改的核心 ---
                //		// 1. 获取搜狗OCR所需的图片（这里我们直接使用原图，不进行缩放）
                //		// 依然建议创建一个克隆来传递，这是一个很好的隔离实践
                //		using (var imageForOcr = new Bitmap(localImage))
                //		{
                //			try
                //			{
                //				// 2. 调用OcrHelper中的搜狗识别方法
                //				// 这个方法在 FmMain.cs 的 SougouOCR 方法中被使用
                //				var jsonResult = OcrHelper.SgBasicOpenOcr(imageForOcr);

                //				// 3. 简单解析返回的JSON结果以获取文本
                //				// 模仿 FmMain.cs 中对搜狗结果的处理
                //				var jObject = Newtonsoft.Json.Linq.JObject.Parse(jsonResult);
                //				var jArray = (Newtonsoft.Json.Linq.JArray)jObject["result"];

                //				var sb = new System.Text.StringBuilder();
                //				foreach (var item in jArray)
                //				{
                //					sb.AppendLine(item["content"].ToString());
                //				}
                //				resultText = sb.ToString().Trim();

                //				if (string.IsNullOrEmpty(resultText))
                //				{
                //					resultText = "***搜狗OCR未识别到文本***";
                //				}
                //			}
                //			catch (Exception ex)
                //			{
                //				resultText = $"搜狗OCR识别出错: {ex.Message}";
                //			}
                //		}
                //	}
                //});
                // // 使用 Task.Run 将耗时的OCR操作放到后台线程，避免UI卡死
                // await Task.Run(() =>
                // {
                //     // 使用 using 语句确保从文件加载的图片资源能被正确释放
                //     using (var localImage = new Bitmap(testImagePath))
                //     {
                //         try
                //         {
                //             // --- 这里是修改的核心 ---
                //             // 1. 调用OcrHelper中的方法，将图片转换为字节数组
                //             byte[] imageBytes = OcrHelper.ImgToBytes(localImage);

                //             // 2. 调用微信OCR的核心识别方法
                //             // OcrHelper.WeChat 是一个异步方法，我们用 .GetAwaiter().GetResult() 在后台线程中同步等待它完成
                //             string result = OcrHelper.WeChat(imageBytes).GetAwaiter().GetResult();

                //             resultText = string.IsNullOrEmpty(result) ? "***微信OCR未识别到文本***" : result;
                //         }
                //         catch (Exception ex)
                //         {
                //             resultText = $"微信OCR识别出错: {ex.Message}";
                //         }
                //     }
                // });
                // 在UI线程上显示结果，确认测试已执行
                MessageBox.Show($"paddle识别完成！\n\n识别结果（前50个字符）:\n{new string(resultText.Take(50).ToArray())}", "测试完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试过程中发生错误: {ex.Message}", "测试失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 无论成功与否，最后都恢复按钮状态
                button.Enabled = true;
                button.Text = "button1";
            }
        }
		/// <summary>
		/// 读取OCR模型配置
		/// </summary>
		private void ReadOcrModelConfigs()
		{
			// 读取PaddleOCR配置
			textBox_PaddleOCR_Det.Text = GetConfigValue("模型配置_PaddleOCR", "Det");
			textBox_PaddleOCR_Cls.Text = GetConfigValue("模型配置_PaddleOCR", "Cls");
			textBox_PaddleOCR_Rec.Text = GetConfigValue("模型配置_PaddleOCR", "Rec");
			textBox_PaddleOCR_Keys.Text = GetConfigValue("模型配置_PaddleOCR", "Keys");
			textBox5.Text = GetConfigValue("模型配置_PaddleOCR", "AdvancedConfig");


			// 读取PaddleOCR2配置
			textBox_PaddleOCR2_Det.Text = GetConfigValue("模型配置_PaddleOCR2", "Det");
			textBox_PaddleOCR2_Cls.Text = GetConfigValue("模型配置_PaddleOCR2", "Cls");
			textBox_PaddleOCR2_Rec.Text = GetConfigValue("模型配置_PaddleOCR2", "Rec");
			textBox_PaddleOCR2_Keys.Text = GetConfigValue("模型配置_PaddleOCR2", "Keys");
			textBox6.Text = GetConfigValue("模型配置_PaddleOCR2", "AdvancedConfig");
			var paddleOcr2DetVersion = GetConfigValue("模型配置_PaddleOCR2", "Det_Version");
			var paddleOcr2ClsVersion = GetConfigValue("模型配置_PaddleOCR2", "Cls_Version");
			var paddleOcr2RecVersion = GetConfigValue("模型配置_PaddleOCR2", "Rec_Version");
			comboBox_PaddleOCR2_det_Version.SelectedItem = string.IsNullOrEmpty(paddleOcr2DetVersion) ? "v5" : paddleOcr2DetVersion;
			comboBox_PaddleOCR2_cls_Version.SelectedItem = string.IsNullOrEmpty(paddleOcr2ClsVersion) ? "v5" : paddleOcr2ClsVersion;
			comboBox_PaddleOCR2_rec_Version.SelectedItem = string.IsNullOrEmpty(paddleOcr2RecVersion) ? "v5" : paddleOcr2RecVersion;


			// 读取RapidOCR配置
			textBox_RapidOCR_Det.Text = GetConfigValue("模型配置_RapidOCR", "Det");
			textBox_RapidOCR_Cls.Text = GetConfigValue("模型配置_RapidOCR", "Cls");
			textBox_RapidOCR_Rec.Text = GetConfigValue("模型配置_RapidOCR", "Rec");
			textBox_RapidOCR_Keys.Text = GetConfigValue("模型配置_RapidOCR", "Keys");
			textBox7.Text = GetConfigValue("模型配置_RapidOCR", "AdvancedConfig");
        }

        /// <summary>
        /// 获取配置值的辅助方法
        /// </summary>
        private string GetConfigValue(string section, string key)
        {
            var value = IniHelper.GetValue(section, key);
            return value == "发生错误" ? "" : value;
        }

        /// <summary>
        /// 浏览模型文件的通用方法
        /// </summary>
        private void BrowseModelFile(TextBox textBox, string description)
        {
			// 1. 创建 VistaFolderBrowserDialog 实例
    		using (var vistaFolderDialog = new Ookii.Dialogs.WinForms.VistaFolderBrowserDialog())
    		{
    		    // 2. 设置属性，它和新版文件选择框的体验一致
    		    vistaFolderDialog.Description = description;
    		    vistaFolderDialog.UseDescriptionForTitle = true; // 将描述作为标题

        		// 3. 显示对话框并获取结果
        		if (vistaFolderDialog.ShowDialog() == DialogResult.OK)
        		{
        		    textBox.Text = vistaFolderDialog.SelectedPath;
        		}
    		}
        }
		/// <summary>
        /// 浏览模型字典文件的通用方法
        /// </summary>
        private void BrowseKeysModelFile(TextBox textBox, string description)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = $"请选择{description}";
                openFileDialog.Filter = "字典文件 (*.txt)|*.txt";
                openFileDialog.FilterIndex = 1;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = openFileDialog.FileName;
                }
            }
        }

        /// <summary>
		/// 浏览RapidOCR模型文件的专用方法（只选择onnx文件）
		/// </summary>
		private void BrowseRapidOcrModelFile(TextBox textBox, string description)
		{
			using (var openFileDialog = new OpenFileDialog())
			{
				openFileDialog.Title = $"请选择{description}";
				openFileDialog.Filter = "ONNX模型文件 (*.onnx)|*.onnx";
				openFileDialog.FilterIndex = 1;

				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					textBox.Text = openFileDialog.FileName;
				}
			}
		}
		/// <summary>
        /// 离线接口浏览高级配置文件的专用方法（只选择json文件）
        /// </summary>
        private void BrowseAdvancedConfigModelFile(TextBox textBox, string description)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = $"请选择{description}";
                openFileDialog.Filter = "json文件 (*.json)|*.json";
                openFileDialog.FilterIndex = 1;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = openFileDialog.FileName;
                }
            }
        }

        // PaddleOCR事件处理方法
		private void Btn_PaddleOCR_Det_Browse_Click(object sender, EventArgs e) => BrowseModelFile(textBox_PaddleOCR_Det, "PaddleOCR检测模型");
        private void Btn_PaddleOCR_Cls_Browse_Click(object sender, EventArgs e) => BrowseModelFile(textBox_PaddleOCR_Cls, "PaddleOCR方向模型");
        private void Btn_PaddleOCR_Rec_Browse_Click(object sender, EventArgs e) => BrowseModelFile(textBox_PaddleOCR_Rec, "PaddleOCR识别模型");
        private void Btn_PaddleOCR_Keys_Browse_Click(object sender, EventArgs e) => BrowseKeysModelFile(textBox_PaddleOCR_Keys, "PaddleOCR字典文件");
		private void Btn_PaddleOCR_AdvancedConfig_Browse_Click(object sender, EventArgs e) => BrowseAdvancedConfigModelFile(textBox5, "PaddleOCR高级配置文件");
        private void TextBox_PaddleOCR_TextChanged(object sender, EventArgs e)
		{
			this.paddleOcrConfigChanged = true; // 只设置标志位
		}

        // PaddleOCR2事件处理方法
        private void Btn_PaddleOCR2_Det_Browse_Click(object sender, EventArgs e) => BrowseModelFile(textBox_PaddleOCR2_Det, "PaddleOCR2检测模型");
        private void Btn_PaddleOCR2_Cls_Browse_Click(object sender, EventArgs e) => BrowseModelFile(textBox_PaddleOCR2_Cls, "PaddleOCR2方向模型");
        private void Btn_PaddleOCR2_Rec_Browse_Click(object sender, EventArgs e) => BrowseModelFile(textBox_PaddleOCR2_Rec, "PaddleOCR2识别模型");
        private void Btn_PaddleOCR2_Keys_Browse_Click(object sender, EventArgs e) => BrowseKeysModelFile(textBox_PaddleOCR2_Keys, "PaddleOCR2字典文件");
		private void Btn_PaddleOCR2_AdvancedConfig_Browse_Click(object sender, EventArgs e) => BrowseAdvancedConfigModelFile(textBox6, "PaddleOCR2高级配置文件");
        private void TextBox_PaddleOCR2_TextChanged(object sender, EventArgs e)
		{
			this.paddleOcr2ConfigChanged = true;
		}
        private void ComboBox_PaddleOCR2_Version_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.paddleOcr2ConfigChanged = true;
        }

        // RapidOCR事件处理方法
        private void Btn_RapidOCR_Det_Browse_Click(object sender, EventArgs e) => BrowseRapidOcrModelFile(textBox_RapidOCR_Det, "RapidOCR检测模型");
        private void Btn_RapidOCR_Cls_Browse_Click(object sender, EventArgs e) => BrowseRapidOcrModelFile(textBox_RapidOCR_Cls, "RapidOCR方向模型");
        private void Btn_RapidOCR_Rec_Browse_Click(object sender, EventArgs e) => BrowseRapidOcrModelFile(textBox_RapidOCR_Rec, "RapidOCR识别模型");
        private void Btn_RapidOCR_Keys_Browse_Click(object sender, EventArgs e) => BrowseKeysModelFile(textBox_RapidOCR_Keys, "RapidOCR字典文件");
		private void Btn_RapidOCR_AdvancedConfig_Browse_Click(object sender, EventArgs e) => BrowseAdvancedConfigModelFile(textBox7, "RapidOCR高级配置文件");
        private void TextBox_RapidOCR_TextChanged(object sender, EventArgs e)
		{
			this.rapidOcrConfigChanged = true;
		}

        
        /// <summary>
        /// 配置变更时重置OCR引擎
        /// </summary>
        private void ResetOcrEngineOnConfigChange()
        {
			try
			{
				// 检查标志位，只重置那些配置被修改过的引擎
				if (this.paddleOcrConfigChanged)
				{
					PaddleOCRHelper.Reset();
					Debug.WriteLine("PaddleOCR configuration changed, engine has been reset.");
				}

				if (this.paddleOcr2ConfigChanged)
				{
					PaddleOCR2Helper.Reset();
					Debug.WriteLine("PaddleOCR2 configuration changed, engine has been reset.");
				}

				if (this.rapidOcrConfigChanged)
				{
					RapidOCRHelper.Reset();
					Debug.WriteLine("RapidOCR configuration changed, engine has been reset.");
    			}

			}
			catch (Exception ex)
			{
				// 处理重置异常，避免影响用户体验
				System.Diagnostics.Debug.WriteLine($"重置引擎时发生异常: {ex.Message}");
				MessageBox.Show($"重置引擎时发生异常: {ex.Message}");
			}
        }

      


        private void tabControl2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            // 获取当前在 tabControl2 中被选中的选项卡页
            TabPage selectedTab = tabControl2.SelectedTab;

            // 默认先显示按钮和说明区域
            bool showApiButtons = true;
            bool showHelpBox = false;
            string helpText = "";

            // 使用 switch 语句，让逻辑更清晰
            switch (selectedTab.Name)
            {
                case "inPage_PaddleOCR":
                    showApiButtons = false; // 隐藏在线API按钮
                    showHelpBox = true;   // 显示说明区域
                    helpText = "说明1：此接口仅支持64位操作系统，支持ppocrv5及更低版本模型，推荐优先使用。\n\n" +
                               "说明2：需要CPU支持AVX指令集，不支持请使用PaddleOCR2接口。\n\n" +
                               "说明3：高级配置文件留空则使用默认值，普通用户推荐留空";
                    break;

                case "inPage_PaddleOCR2":
                    showApiButtons = false;
                    showHelpBox = true;
                    helpText = "说明1：此接口仅支持64位操作系统，支持ppocrv5及更低版本模型。\n\n" +
                               "说明2：无需CPU支持AVX指令集，兼容性更好，但识别速度可能较慢。\n\n" +
                               "说明3：高级配置文件留空则使用默认值，普通用户推荐留空";
                    break;

                case "inPage_RapidOCR":
                    showApiButtons = false;
                    showHelpBox = true;
                    helpText = "说明1：此接口支持32位和64位操作系统，不支持ppocrv5版本模型，支持ppocrv4及更低版本模型。\n\n" +
                               "说明2：不知道CPU是否需要支持AVX指令集，自行测试。\n\n" +
                               "说明3：高级配置文件留空则使用默认值，普通用户推荐留空";
                    break;

                // 默认情况，适用于百度OCR、腾讯OCR等所有其他选项卡
                default:
                    showApiButtons = true;  // 显示在线API按钮
                    showHelpBox = false;  // 隐藏说明区域
                    helpText = "";
                    break;
            }

            // --- 统一更新UI ---

            // 更新三个按钮的可见性
            密钥Button_apply.Visible = showApiButtons; // "接口申请"
            百度_btn.Visible = showApiButtons;         // "密钥测试"
            密钥Button.Visible = showApiButtons;       // "恢复默认"

            // 更新说明区域的可见性和文本内容
            // (请将 "groupBox_Help" 替换为您在第一步中添加的GroupBox的实际名称)
            groupBox7.Visible = showHelpBox;
            label_OcrApiHelpText.Text = helpText;
        }

    }
}
