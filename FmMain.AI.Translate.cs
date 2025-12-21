using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text; // 确保引用
using System.Windows.Forms;
using Newtonsoft.Json;
using TrOCR.Helper;

namespace TrOCR
{
    public partial class FmMain
    {
        // 用于存储当前选中的 AI 模式，如果为 null 则使用默认逻辑
        private AIMode currentSelectedAITransMode = null;

        private void Trans_ai_openai_compatible_Click(object sender, EventArgs e)
		{
            //使用默认翻译模式，清除子菜单的勾选
            ClearAITransConfigSelection();
            Trans_foreach("OpenAICompatible");
		}

        /// <summary>
        /// 加载 AI 配置文件并初始化动态菜单
        /// </summary>
        private void LoadAITranConfigMenus()
        {
            try
            {
                // === 第一步：先彻底重置菜单到默认状态（三级菜单模式） ===
                // 1. 清空所有子菜单
                this.ai_openai_compatible_trans.DropDownItems.Clear();
                // 2. 移除可能存在的子菜单点击逻辑（虽然清空了Items，但习惯上解绑是个好习惯）
                // 3. 重新绑定默认的父级点击事件（防止重复绑定，先减后加）
                this.ai_openai_compatible_trans.Click -= new EventHandler(this.Trans_ai_openai_compatible_Click);
                this.ai_openai_compatible_trans.Click += new EventHandler(this.Trans_ai_openai_compatible_Click);
                // 4. 重置当前选中的模式
                this.currentSelectedAITransMode = null;
                // === 读取上次保存的模式名称 ===
                string lastSelectedModeName = TrOCRUtils.LoadSetting("OpenAICompatibleTrans", "SelectedMode", "");
                // 1. 获取配置文件路径 (假设在 Data 目录下)
                // 这里我们优先使用 Ini 中配置的路径，如果没有则尝试默认路径
                string configPath = TrOCRUtils.LoadSetting("OpenAICompatibleTrans", "Config","");
                if (string.IsNullOrEmpty(configPath))
                {
                    configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "AITranslateConfig.json");
                    if (File.Exists(configPath))
                    {
                        IniHelper.SetValue("OpenAICompatibleTrans", "Config", configPath);
                    }
                }

                // 2. 如果文件不存在，直接结束（此时菜单已重置为默认的三级菜单）
                if (!File.Exists(configPath))
                {
                    return;
                }

                // 3. 读取并解析配置
                string jsonContent = File.ReadAllText(configPath, Encoding.UTF8);
                AIConfig aiConfig = JsonConvert.DeserializeObject<AIConfig>(jsonContent);

                // 4. 如果配置有效且有 modes
                if (aiConfig != null && aiConfig.modes != null && aiConfig.modes.Count > 0)
                {
                    // === 核心逻辑：转换为四级菜单 ===

                    // A. 移除父级菜单原有的点击事件 (使其点击只展开子菜单)
                    this.ai_openai_compatible_trans.Click -= new EventHandler(this.Trans_ai_openai_compatible_Click);
                    
                    // B. 清空可能存在的旧项
                    this.ai_openai_compatible_trans.DropDownItems.Clear();

                    // C. 动态添加子菜单项
                    foreach (var mode in aiConfig.modes)
                    {
                        ToolStripMenuItem modeItem = new ToolStripMenuItem();
                        modeItem.Text = mode.mode; // 显示名称
                        modeItem.Tag = mode;       // 将 mode 对象存储在 Tag 中
                        // === 【新增】比对并恢复选中状态 ===
                        // 如果当前遍历的模式名称 等于 上次保存的名称
                        if (!string.IsNullOrEmpty(lastSelectedModeName) && mode.mode == lastSelectedModeName)
                        {
                            modeItem.Checked = true;             // 恢复UI勾选
                            this.currentSelectedAITransMode = mode;   // 恢复内存变量
                        }
                        modeItem.Click += new EventHandler(this.AI_Trans_SubMenu_Click); // 绑定点击事件

                        // 可以根据描述添加 ToolTipText
                        if (!string.IsNullOrEmpty(mode.description))
                        {
                            modeItem.ToolTipText = mode.description;
                        }

                        this.ai_openai_compatible_trans.DropDownItems.Add(modeItem);
                    }
                    // === 【关键修改点】 ===
                    // 逻辑：没存就是第一个，存的找不到就不管它

                    // 只有当 lastSelectedModeName 是空字符串（从未设置过）时，才自动选第一个
                    //    if (string.IsNullOrEmpty(lastSelectedModeName) && this.ai_openai_compatible.DropDownItems.Count > 0)
                    //    {
                    //        if (this.ai_openai_compatible.DropDownItems[0] is ToolStripMenuItem firstItem)
                    //        {
                    //            firstItem.Checked = true;
                    //            if (firstItem.Tag is AIMode firstMode)
                    //            {
                    //                this.currentSelectedAITransMode = firstMode;
                    //                // 既然自动帮你选了第一个，顺便保存一下，下次就不算"没存过"了
                    //                IniHelper.SetValue("OpenAICompatibleTrans", "SelectedMode", firstMode.mode);
                    //                // CommonHelper.ShowHelpMsg("未选择模式，将使用配置文件里第一个模式");
                    //                Debug.WriteLine("Fmmain.AI.Translate.cs--未选择模式，将使用配置文件里第一个模式");
                    //            }
                    //        }
                    //    }
                }
            }
            catch (Exception ex)
            {
                // 如果解析出错，不抛出异常，保持默认的三级菜单状态，但在调试窗口输出
                System.Diagnostics.Debug.WriteLine("加载 AI翻译菜单失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 动态生成的四级子菜单点击事件
        /// </summary>
        private void AI_Trans_SubMenu_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem clickedItem && clickedItem.Tag is AIMode mode)
            {
                // UI更新：实现单选效果
                foreach (ToolStripItem item in this.ai_openai_compatible_trans.DropDownItems)
                {
                    if (item is ToolStripMenuItem menuItem)
                    {
                        // 只有当前点击的项设为 true
                        menuItem.Checked = (menuItem == clickedItem);
                    }
                }
                // 1. 记录当前选中的模式
                this.currentSelectedAITransMode = mode;

                // 2. 更新父菜单的选中状态或文本（可选，用于提示用户当前选了哪个）
                // 比如：this.ai_openai_compatible_trans.Text = $"OpenAICompatible ({mode.mode})";

                // === 【新增】保存选中状态到配置文件 ===
                IniHelper.SetValue("OpenAICompatibleTrans", "SelectedMode", mode.mode);
                // 3. 触发 翻译 流程
                // 注意：这里调用 Trans_foreach 会触发 Main_OCR_Thread，最终调用 Trans_OpenAICompatible
                Trans_foreach("OpenAICompatible");
            }
        }

        /// <summary>
        /// 清除 OpenAI 菜单的所有勾选状态，并重置为默认模式
        /// </summary>
        public void ClearAITransConfigSelection()
        {
            // 1. 遍历取消视觉上的勾选
            foreach (ToolStripItem item in this.ai_openai_compatible_trans.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.Checked = false;
                }
            }

            // 2. 重置内部状态（下次点击父菜单时，将使用默认配置）
            this.currentSelectedAITransMode = null;
        }

        /// <summary>
        /// OpenAICompatible Translate 执行入口 (被 Main_OCR_Thread 调用)
        /// </summary>
        public void Trans_OpenAICompatible(string text,string fromLang, string toLang)
        {
            try
            {
                // === 【日志代码】开始 ===
                //string logModeName = this.currentSelectedAITransMode != null
                //                     ? this.currentSelectedAITransMode.mode
                //                     : "null (将使用程序内部硬编码默认值)";
                string logModeName = this.currentSelectedAITransMode != null
                                     ? JsonConvert.SerializeObject(currentSelectedAITransMode, Formatting.Indented)
                                     : "null (将使用程序内部硬编码翻译模式默认值)";

                System.Diagnostics.Debug.WriteLine("--------------------------------------------------");
                System.Diagnostics.Debug.WriteLine($"[FmMain] 准备开始 翻译");
                System.Diagnostics.Debug.WriteLine($"[FmMain] 当前选中的 currentSelectedAITransMode: {logModeName}");
                // === 【日志代码】结束 ===
                //传入当前选中的模式 (currentSelectedAITransMode)
                // 如果是从三级菜单（默认无配置）进来的，currentSelectedAITransMode 为 null，Helper 会处理
                googleTranslate_txt = OpenAICompatibleTranslate.Translate(
                            text,
                            this.currentSelectedAITransMode,
                            toLang,
                            fromLang
                        );

                
                // 识别完成后，建议将模式重置为空，或者保留上次选择（取决于你的需求）
                // 如果希望每次截图都重置为默认，取消下面的注释：
                // this.currentSelectedAITransMode = null; 
            }
            catch (Exception ex)
            {
                googleTranslate_txt = "OpenAICompatibleTrans 接口调用出错: " + ex.Message;
            }
        }
    }
}