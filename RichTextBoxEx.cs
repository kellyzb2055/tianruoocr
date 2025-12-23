using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TrOCR.Helper;

namespace TrOCR
{

	public class RichTextBoxEx : HelpRepaint.AdvRichTextBox
	{

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			components = new Container();
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr LoadLibrary(string path);
        // ====================【升级 RichEdit 内核】开始 ====================
        protected override CreateParams CreateParams
        {
            get
            {
                // 获取基类的参数
                CreateParams cp = base.CreateParams;
                try
                {
                    // 尝试加载 Windows 自带的高级 RichEdit 内核 (msftedit.dll)
                    // 该文件在 XP SP1 及以上系统均存在
                    if (LoadLibrary("msftedit.dll") != IntPtr.Zero)
                    {
                        // 强行将类名修改为 RichEdit50W
                        // 这将启用高级排版功能（包括彩色 Emoji 支持和更好的字体回退机制）
                        cp.ClassName = "RichEdit50W";
                    }
                }
                catch
                {
					// 如果加载失败（极少见），什么都不做，使用默认内核防止崩溃
					CommonHelper.ShowHelpMsg("RichEdit加载内核失败,使用默认内核",10000);
                }
                return cp;
            }
        }
        // ====================【升级 RichEdit 内核】结束 ====================


        [Bindable(true)]
		[RefreshProperties(RefreshProperties.All)]
		[SettingsBindable(true)]
		[DefaultValue(false)]
		[Category("Appearance")]
		public string Rtf2
		{
			get
			{
				return Rtf;
			}
			set
			{
				Rtf = value;
			}
		}

		private IContainer components;

		private static IntPtr moduleHandle;
	}
}
