﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using TrOCR.Helper;

namespace TrOCR
{
    /// <summary>
    /// 应用程序的主入口点和核心初始化类
    /// 负责应用程序启动、异常处理、配置初始化、更新检查等核心功能
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// DPI缩放因子
        /// </summary>
        public static float Factor = 1.0f;

        /// <summary>
        /// 应用程序入口点
        /// </summary>
        /// <param name="args">命令行参数</param>
        [STAThread]
        public static void Main(string[] args)
        {
        	try
        	{
        		// 设置异常处理模式和事件处理程序
        		Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        		Application.ThreadException += Application_ThreadException;
        		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      
        		// 检查是否已经运行了程序实例
        		var eventName = "TianruoOcrInstance_" + Application.ExecutablePath.Replace(Path.DirectorySeparatorChar, '_');
        		var programStarted = new EventWaitHandle(false, EventResetMode.AutoReset, eventName, out var needNew);
        		if (!needNew)
        		{
        			programStarted.Set();
        			CommonHelper.ShowHelpMsg("软件已经运行");
        			return;
        		}
        		
        		// 初始化配置文件
        		InitConfig();
        		DealErrorConfig();
        		
        		// 设置应用程序视觉样式
        		Application.EnableVisualStyles();
        		Application.SetCompatibleTextRenderingDefault(false);
        		var version = Environment.OSVersion.Version;
        		var value = new Version("6.1");
        		Factor = CommonHelper.GetDpiFactor();
        		if (version.CompareTo(value) >= 0)
        		{
        			CommonHelper.SetProcessDPIAware();
        		}
        		
        		// 处理启动参数
        		if (args.Length != 0 && args[0] == "更新")
        		{
        			new FmSetting
        			{
        				Start_set = ""
        			}.ShowDialog();
        		}
        		
        		// 启动更新检查任务并运行主窗体
        		Task.Factory.StartNew(CheckUpdate);
        		Application.Run(new FmMain());
        	}
        	catch (Exception ex)
        	{
        		// 记录详细的异常信息到日志文件
        		try
        		{
        			var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "error.log");
        			var logDir = Path.GetDirectoryName(logPath);
        			if (!Directory.Exists(logDir))
        			{
        				Directory.CreateDirectory(logDir);
        			}
        			
        			var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 未处理异常:{ex}{new string('=', 80)}";
        			File.AppendAllText(logPath, logEntry, Encoding.UTF8);
        		}
        		catch
        		{
        			// 如果日志记录失败，忽略错误
        		}
        		
        		// 显示用户友好的错误信息
        		var errorMsg = $"程序启动时发生错误:{ex.Message}，详细信息已记录到 Data/error.log 文件中。";
        		MessageBox.Show(errorMsg, "TrOCR 启动错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        	}
        }
      
        /// <summary>
        /// 处理线程异常事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">线程异常事件参数</param>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
        	MessageBox.Show("捕获到线程异常: " + e.Exception.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      
        /// <summary>
        /// 处理未处理的异常事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">未处理异常事件参数</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
        	MessageBox.Show("捕获到未经处理的异常: " + e.ExceptionObject.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        

        /// <summary>
        /// 检查应用程序更新
        /// </summary>
        public static void CheckUpdate()
        {
            try
            {
                // 直接使用 GitHub Releases 作为更新源
                var apiUrl = "https://gh-proxy.com/https://api.github.com/repos/Topkill/tianruoocr/releases/latest";

                // 获取最新版本信息
                var request = System.Net.WebRequest.Create(apiUrl) as System.Net.HttpWebRequest;
                request.UserAgent = "TianruoOCR";  // GitHub API 要求设置 User-Agent
                request.Accept = "application/vnd.github.v3+json";
                request.Timeout = 10000;  // 10秒超时
                
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var jsonText = reader.ReadToEnd();
                    var json = JObject.Parse(jsonText);
                    
                    // 获取版本号（去掉前面的 'v'）
                    var tagName = json["tag_name"].Value<string>();
                    var newVersion = tagName.TrimStart('v', 'V');
                    var curVersion = Application.ProductVersion;
                    
                    // 检查是否有新版本
                    if (!CheckVersion(newVersion, curVersion))
                    {
                        CommonHelper.ShowHelpMsg("当前已是最新版本");
                        return;
                    }
                    
                    CommonHelper.ShowHelpMsg("有新版本：" + newVersion);
                    
                    // 获取下载链接
                    var htmlUrl = json["html_url"].Value<string>();
                    var assets = json["assets"] as JArray;
                    string downloadUrl = null;
                    
                    // 查找 exe 或 zip 文件的下载链接
                    if (assets != null && assets.Count > 0)
                    {
                        foreach (var asset in assets)
                        {
                            var name = asset["name"].Value<string>();
                            if (name.EndsWith(".exe") || name.EndsWith(".zip") || name.EndsWith(".rar") || name.EndsWith(".7z"))
                            {
                                downloadUrl = asset["browser_download_url"].Value<string>();
                                break;
                            }
                        }
                    }
                    
                    // 获取更新说明（限制长度）
                    var body = json["body"]?.Value<string>() ?? "无更新说明";
                    if (body.Length > 500)
                    {
                        body = body.Substring(0, 500) + "...";
                    }
                    
                    // 显示更新提示
                    var message = $"发现新版本：{newVersion}\n";
                    message += $"当前版本：{curVersion}\n\n";
                    message += $"更新内容：\n{body}\n\n";
                    message += "是否前往下载页面？";
                    
                    if (MessageBox.Show(message, "发现新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        // 如果有直接下载链接，使用下载链接；否则打开发布页面
                        Process.Start(downloadUrl ?? htmlUrl);
                    }
                }
            }
            catch (System.Net.WebException ex)
            {
                // 网络错误，静默失败或显示简单提示
                if (ex.Status == System.Net.WebExceptionStatus.Timeout)
                {
                    // 超时不提示，避免影响用户体验
                    return;
                }
                CommonHelper.ShowHelpMsg("检查更新失败，请检查网络连接");
            }
            catch (Exception ex)
            {
                // 其他错误
                CommonHelper.ShowHelpMsg($"检查更新时出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 比较版本号大小
        /// </summary>
        /// <param name="newVersion">新版本号</param>
        /// <param name="curVersion">当前版本号</param>
        /// <returns>如果有新版本返回true，否则返回false</returns>
        private static bool CheckVersion(string newVersion, string curVersion)
        {
            var arr1 = newVersion.Split('.');
            var arr2 = curVersion.Split('.');
            for (int i = 0; i < arr1.Length; i++)
            {
                if (Convert.ToInt32(arr1[i]) > Convert.ToInt32(arr2[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 初始化配置文件(config.ini)，如果配置文件不存在则创建它
        /// </summary>
        private static void InitConfig()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory + "Data\\config.ini";
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Data"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Data");
            }

            if (!File.Exists(path))
            {
                using (File.Create(path))
                {
                }

                // 设置默认配置值
                IniHelper.SetValue("配置", "接口", "搜狗");
                IniHelper.SetValue("配置", "开机自启", "True");
                IniHelper.SetValue("配置", "快速翻译", "True");
                IniHelper.SetValue("配置", "识别弹窗", "True");
                IniHelper.SetValue("配置", "窗体动画", "窗体");
                IniHelper.SetValue("配置", "记录数目", "20");
                IniHelper.SetValue("配置", "自动保存", "True");
                IniHelper.SetValue("配置", "截图位置", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
                IniHelper.SetValue("配置", "翻译接口", "谷歌");
                IniHelper.SetValue("识别后操作", "AutoCopyOcrResult", "False");
                IniHelper.SetValue("识别后操作", "AutoTranslateOcrResult", "False");
                IniHelper.SetValue("翻译后操作", "AutoCopyOcrTranslation", "False");
                IniHelper.SetValue("翻译后操作", "AutoCopyInputTranslation", "False");
                IniHelper.SetValue("快捷键", "文字识别", "F4");
                IniHelper.SetValue("快捷键", "翻译文本", "F9");
                IniHelper.SetValue("快捷键", "记录界面", "请按下快捷键");
                IniHelper.SetValue("快捷键", "识别界面", "请按下快捷键");
                IniHelper.SetValue("快捷键", "静默识别", "请按下快捷键");
                IniHelper.SetValue("快捷键", "输入翻译", "请按下快捷键");
                IniHelper.SetValue("密钥_百度", "secret_id", "YsZKG1wha34PlDOPYaIrIIKO");
                IniHelper.SetValue("密钥_百度", "secret_key", "HPRZtdOHrdnnETVsZM2Nx7vbDkMfxrkD");
                IniHelper.SetValue("代理", "代理类型", "系统代理");
                IniHelper.SetValue("代理", "服务器", "");
                IniHelper.SetValue("代理", "端口", "");
                IniHelper.SetValue("代理", "需要密码", "False");
                IniHelper.SetValue("代理", "服务器账号", "");
                IniHelper.SetValue("代理", "服务器密码", "");
                IniHelper.SetValue("更新", "检测更新", "True");
                IniHelper.SetValue("更新", "更新间隔", "True");
                IniHelper.SetValue("更新", "间隔时间", "24");
                IniHelper.SetValue("更新", "更新地址", "https://github.com/Topkill/tianruoocr/releases");
                IniHelper.SetValue("截图音效", "自动保存", "True");
                IniHelper.SetValue("截图音效", "音效路径", "Data\\screenshot.wav");
                IniHelper.SetValue("截图音效", "粘贴板", "False");
                IniHelper.SetValue("工具栏", "合并", "False");
                IniHelper.SetValue("工具栏", "分段", "False");
                IniHelper.SetValue("工具栏", "分栏", "False");
                IniHelper.SetValue("工具栏", "拆分", "False");
                IniHelper.SetValue("工具栏", "检查", "False");
                IniHelper.SetValue("工具栏", "翻译", "False");
                IniHelper.SetValue("工具栏", "顶置", "True");
                IniHelper.SetValue("取色器", "类型", "RGB");
            }
        }

        /// <summary>
        /// 读取配置文件，处理错误的配置项
        /// </summary>
        private static void DealErrorConfig()
        {
            // 恢复发生错误的配置项为默认值
            if (IniHelper.GetValue("配置", "接口") == "发生错误")
            {
                IniHelper.SetValue("配置", "接口", "搜狗");
            }

            if (IniHelper.GetValue("配置", "开机自启") == "发生错误")
            {
                IniHelper.SetValue("配置", "开机自启", "True");
            }

            if (IniHelper.GetValue("配置", "快速翻译") == "发生错误")
            {
                IniHelper.SetValue("配置", "快速翻译", "True");
            }

            if (IniHelper.GetValue("配置", "识别弹窗") == "发生错误")
            {
                IniHelper.SetValue("配置", "识别弹窗", "True");
            }

            if (IniHelper.GetValue("配置", "窗体动画") == "发生错误")
            {
                IniHelper.SetValue("配置", "窗体动画", "窗体");
            }

            if (IniHelper.GetValue("配置", "记录数目") == "发生错误")
            {
                IniHelper.SetValue("配置", "记录数目", "20");
            }

            if (IniHelper.GetValue("配置", "自动保存") == "发生错误")
            {
                IniHelper.SetValue("配置", "自动保存", "True");
            }

            if (IniHelper.GetValue("识别后操作", "AutoCopyOcrResult") == "发生错误")
            {
                IniHelper.SetValue("识别后操作", "AutoCopyOcrResult", "False");
            }

            if (IniHelper.GetValue("识别后操作", "AutoTranslateOcrResult") == "发生错误")
            {
                IniHelper.SetValue("识别后操作", "AutoTranslateOcrResult", "False");
            }

            if (IniHelper.GetValue("翻译后操作", "AutoCopyOcrTranslation") == "发生错误")
            {
                IniHelper.SetValue("翻译后操作", "AutoCopyOcrTranslation", "False");
            }

            if (IniHelper.GetValue("翻译后操作", "AutoCopyInputTranslation") == "发生错误")
            {
                IniHelper.SetValue("翻译后操作", "AutoCopyInputTranslation", "False");
            }

            if (IniHelper.GetValue("配置", "翻译接口") == "发生错误")
            {
                IniHelper.SetValue("配置", "翻译接口", "谷歌");
            }

            if (IniHelper.GetValue("配置", "截图位置") == "发生错误")
            {
                IniHelper.SetValue("配置", "截图位置", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            }

            if (IniHelper.GetValue("快捷键", "文字识别") == "发生错误")
            {
                IniHelper.SetValue("快捷键", "文字识别", "F4");
            }

            if (IniHelper.GetValue("快捷键", "翻译文本") == "发生错误")
            {
                IniHelper.SetValue("快捷键", "翻译文本", "F9");
            }

            if (IniHelper.GetValue("快捷键", "记录界面") == "发生错误")
            {
                IniHelper.SetValue("快捷键", "记录界面", "请按下快捷键");
            }

            if (IniHelper.GetValue("快捷键", "识别界面") == "发生错误")
            {
                IniHelper.SetValue("快捷键", "识别界面", "请按下快捷键");
            }

            if (IniHelper.GetValue("快捷键", "静默识别") == "发生错误")
            {
                IniHelper.SetValue("快捷键", "静默识别", "请按下快捷键");
            }

            if (IniHelper.GetValue("快捷键", "输入翻译") == "发生错误")
            {
                IniHelper.SetValue("快捷键", "输入翻译", "请按下快捷键");
            }

            if (IniHelper.GetValue("密钥_百度", "secret_id") == "发生错误")
            {
                IniHelper.SetValue("密钥_百度", "secret_id", "YsZKG1wha34PlDOPYaIrIIKO");
            }

            if (IniHelper.GetValue("密钥_百度", "secret_key") == "发生错误")
            {
                IniHelper.SetValue("密钥_百度", "secret_key", "HPRZtdOHrdnnETVsZM2Nx7vbDkMfxrkD");
            }

            if (IniHelper.GetValue("代理", "代理类型") == "发生错误")
            {
                IniHelper.SetValue("代理", "代理类型", "系统代理");
            }

            if (IniHelper.GetValue("代理", "服务器") == "发生错误")
            {
                IniHelper.SetValue("代理", "服务器", "");
            }

            if (IniHelper.GetValue("代理", "端口") == "发生错误")
            {
                IniHelper.SetValue("代理", "端口", "");
            }

            if (IniHelper.GetValue("代理", "需要密码") == "发生错误")
            {
                IniHelper.SetValue("代理", "需要密码", "False");
            }

            if (IniHelper.GetValue("代理", "服务器账号") == "发生错误")
            {
                IniHelper.SetValue("代理", "服务器账号", "");
            }

            if (IniHelper.GetValue("代理", "服务器密码") == "发生错误")
            {
                IniHelper.SetValue("代理", "服务器密码", "");
            }

            if (IniHelper.GetValue("更新", "检测更新") == "发生错误")
            {
                IniHelper.SetValue("更新", "检测更新", "True");
            }

            if (IniHelper.GetValue("更新", "更新间隔") == "发生错误")
            {
                IniHelper.SetValue("更新", "更新间隔", "True");
            }

            if (IniHelper.GetValue("更新", "间隔时间") == "发生错误")
            {
                IniHelper.SetValue("更新", "间隔时间", "24");
            }

            if (IniHelper.GetValue("更新", "更新地址") == "发生错误")
            {
                IniHelper.SetValue("更新", "更新地址", "https://github.com/Topkill/tianruoocr/releases");
            }

            if (IniHelper.GetValue("截图音效", "自动保存") == "发生错误")
            {
                IniHelper.SetValue("截图音效", "自动保存", "True");
            }

            if (IniHelper.GetValue("截图音效", "音效路径") == "发生错误")
            {
                IniHelper.SetValue("截图音效", "音效路径", "Data\\screenshot.wav");
            }

            if (IniHelper.GetValue("截图音效", "粘贴板") == "发生错误")
            {
                IniHelper.SetValue("截图音效", "粘贴板", "False");
            }

            if (IniHelper.GetValue("工具栏", "合并") == "发生错误")
            {
                IniHelper.SetValue("工具栏", "合并", "False");
            }

            if (IniHelper.GetValue("工具栏", "拆分") == "发生错误")
            {
                IniHelper.SetValue("工具栏", "拆分", "False");
            }

            if (IniHelper.GetValue("工具栏", "检查") == "发生错误")
            {
                IniHelper.SetValue("工具栏", "检查", "False");
            }

            if (IniHelper.GetValue("工具栏", "翻译") == "发生错误")
            {
                IniHelper.SetValue("工具栏", "翻译", "False");
            }

            if (IniHelper.GetValue("工具栏", "分段") == "发生错误")
            {
                IniHelper.SetValue("工具栏", "分段", "False");
            }

            if (IniHelper.GetValue("工具栏", "分栏") == "发生错误")
            {
                IniHelper.SetValue("工具栏", "分栏", "False");
            }

            if (IniHelper.GetValue("工具栏", "顶置") == "发生错误")
            {
                IniHelper.SetValue("工具栏", "顶置", "True");
            }

            if (IniHelper.GetValue("取色器", "类型") == "发生错误")
            {
                IniHelper.SetValue("取色器", "类型", "RGB");
            }

            if (IniHelper.GetValue("特殊", "ali_cookie") == "发生错误")
            {
                IniHelper.SetValue("特殊", "ali_cookie",
                    "cna=noXhE38FHGkCAXDve7YaZ8Tn; cnz=noXhE4/VhB8CAbZ773ApeV14; isg=BGJi2c2YTeeP6FG7S_Re8kveu-jEs2bNwToQnKz7jlWAfwL5lEO23eh9q3km9N5l; ");
            }

            if (IniHelper.GetValue("特殊", "ali_account") == "发生错误")
            {
                IniHelper.SetValue("特殊", "ali_account", "");
            }

            if (IniHelper.GetValue("特殊", "ali_password") == "发生错误")
            {
                IniHelper.SetValue("特殊", "ali_password", "");
            }
        }
    }
}