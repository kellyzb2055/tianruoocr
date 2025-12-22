using DocumentFormat.OpenXml.Bibliography;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text; // 确保引用
using System.Threading.Tasks;
using System.Windows.Forms;
using TrOCR.Helper;

namespace TrOCR
{
    public partial class FmMain
    {
        // === 全局状态变量 ===

        // 当前选中的厂商 (例如: DeepSeek)
        private CustomAITransProvider _currentCustomTransProvider = null;

        // 当前选中的模式 (例如: 精确识别，包含 Prompt/Temperature 等)
        private AIMode _currentCustomTransMode = null;

        /// <summary>
        /// 加载 AI 翻译配置文件并初始化动态菜单
        /// </summary>
        public void LoadCustomOpenAITransMenus()
        {
            try
            {
                // ================= Step 1: 基础清理与准备 =================
                ToolStripMenuItem parentMenu = this.ai_menu_trans;
                if (parentMenu == null) return;

                // 清理旧的动态菜单
                for (int i = parentMenu.DropDownItems.Count - 1; i >= 0; i--)
                {
                    if (parentMenu.DropDownItems[i].Tag?.ToString() == "DynamicTransProvider")
                    {
                        parentMenu.DropDownItems.RemoveAt(i);
                    }
                }

                // 读取厂商列表(CustomOpenAIProviders.json)
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "CustomOpenAITransProviders.json");
                if (!File.Exists(jsonPath)) return;

                var providers = JsonConvert.DeserializeObject<List<CustomAITransProvider>>(File.ReadAllText(jsonPath));
                if (providers == null) return;

                // 添加分割线
                parentMenu.DropDownItems.Add(new ToolStripSeparator { Tag = "DynamicTransProvider" });

                // 读取 INI 记录 (上次选了谁)
                string lastProviderName = IniHelper.GetValue("OpenAICompatibleTrans", "LastProvider");
                string lastModeName = IniHelper.GetValue("OpenAICompatibleTrans", "LastMode");

                // 标记：是否已经成功恢复了某个选项（防止多个厂商都去恢复）
                bool isRestored = false;

                // ================= Step 2: 循环构建菜单 =================
                foreach (var provider in providers)
                {
                    // 2.1 创建厂商菜单项 (一级)
                    ToolStripMenuItem providerItem = new ToolStripMenuItem(provider.Name);
                    providerItem.Tag = "DynamicTransProvider";
                    // 将构建好的菜单项加入主菜单
                    parentMenu.DropDownItems.Add(providerItem);
                    // 准备一个列表来存放这个厂商下所有的模式 (无论是配置文件里的，还是默认生成的)
                    List<AIMode> availableModes = new List<AIMode>();

                    // 2.2 尝试加载子菜单配置 (二级)
                    if (!string.IsNullOrEmpty(provider.ModelConfigPath))
                    {
                        // 处理相对路径/绝对路径
                        string configFullPath = provider.ModelConfigPath;
                        if (!Path.IsPathRooted(configFullPath))
                            configFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFullPath);

                        if (File.Exists(configFullPath))
                        {
                            try
                            {
                                var configObj = JsonConvert.DeserializeObject<AIConfig>(File.ReadAllText(configFullPath, Encoding.UTF8));
                                if (configObj != null && configObj.modes != null)
                                {
                                    availableModes.AddRange(configObj.modes);
                                }
                            }
                            catch (Exception ex)
                            {
                                /* 忽略配置读取错误 */
                                Debug.WriteLine("配置文件存在，但读取出错：" + ex.Message);
                            }
                        }
                    }

                    // 2.3 如果没有加载到任何模式，添加一个“默认模式”保底
                    if (availableModes.Count == 0)
                    {
                        availableModes.Add(new AIMode
                        {
                            mode = "默认模式",
                            prompt = "请翻译文本：",
                            temperature = 0.5
                        });
                    }

                    // 2.4 将模式列表渲染到 UI，并检查是否匹配上次记录
                    //成功匹配
                    bool foundMatchInThisProvider = false; // 标记：在这个厂商里是否找到了上次的模式

                    foreach (var mode in availableModes)
                    {
                        ToolStripMenuItem modeItem = new ToolStripMenuItem(mode.mode);
                        modeItem.ToolTipText = mode.description;
                        modeItem.Click += (s, e) => SwitchToCustomTranAI(provider, mode);

                        providerItem.DropDownItems.Add(modeItem);

                        // --- 判断逻辑：尝试恢复 ---
                        // 只有当“还没恢复过” 且 “厂商名对上了” 且 “模式名对上了”
                        if (!isRestored && provider.Name == lastProviderName && mode.mode == lastModeName)
                        {
                            SwitchToCustomTranAI(provider, mode);
                            modeItem.Checked = true;
                            isRestored = true;
                            foundMatchInThisProvider = true;
                        }
                    }

                    // ================= Step 3: 单个厂商内的兜底逻辑 / 厂商兜底=================

                    // 场景：ini记录我是选的这个厂商，但是...
                    if (!isRestored && provider.Name == lastProviderName)
                    {
                        // 情况A: 我没找到具体的模式 (foundMatchInThisProvider == false)
                        // 原因可能是：配置文件改了(模式名变了)，或者配置删了(退化成默认模式)
                        // 解决：强制选中列表里的第一个模式
                        if (!foundMatchInThisProvider && availableModes.Count > 0)
                        {
                            var fallbackMode = availableModes[0];
                            SwitchToCustomTranAI(provider, fallbackMode);

                            // UI打钩
                            if (providerItem.DropDownItems.Count > 0 && providerItem.DropDownItems[0] is ToolStripMenuItem firstItem)
                                firstItem.Checked = true;

                            isRestored = true;
                        }
                    }


                }

                // ================= Step 4: 全局终极兜底逻辑/全局兜底 =================

                // 场景：循环跑完了，isRestored 还是 false。
                // 原因可能是：上次选的厂商直接被删了，或者这是第一次运行软件。
                // 解决：强制选中所有菜单里的【第一个厂商】的【第一个模式】。
                if (!isRestored && StaticValue.Translate_Current_API == "CustomOpenAI")
                {
                    // 找到第一个厂商菜单项 (跳过分割线)
                    foreach (ToolStripItem item in parentMenu.DropDownItems)
                    {
                        if (item is ToolStripMenuItem firstProviderItem && firstProviderItem.HasDropDownItems)
                        {
                            // 模拟点击第一个子项
                            if (firstProviderItem.DropDownItems[0] is ToolStripMenuItem firstOption)
                            {
                                firstOption.PerformClick();
                                isRestored = true;
                            }
                            break; // 处理完就退出
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("加载自定义 AI 菜单失败: " + ex.Message);
            }
        }
        /// <summary>
        /// 切换当前使用的 AI 上下文 (点击菜单项时触发)
        /// </summary>
        private void SwitchToCustomTranAI(CustomAITransProvider provider, AIMode mode)
        {
            try
            {
                // 1. 更新全局变量
                this._currentCustomTransProvider = provider;
                this._currentCustomTransMode = mode;

                
                Trans_foreach("CustomOpenAI");
                /// === ★★★ 新增：保存选择到配置文件 ★★★ ===
                // 这样下次启动时，我们就能知道上次选的是谁
                try
                {
                    IniHelper.SetValue("OpenAICompatibleTrans", "LastProvider", provider.Name);
                    IniHelper.SetValue("OpenAICompatibleTrans", "LastMode", mode.mode);
                    // 同时也把主接口设为 翻译接口 (虽然 Trans_foreach 会做，但这里双重保险)
                    IniHelper.SetValue("配置", "翻译接口", "CustomOpenAI");
                }
                catch { /* 忽略保存错误 */ }

                // ================== 2. UI 视觉更新 ==================

                // --- A. 第一级：更新 "AI" 主菜单 ---
                //this.ai_menu_trans.Checked = true; // 给 "AI" 大标题打勾
                // 更新显示文本 (例如: "AI: DeepSeek - 精确识别")，直观提示用户
                this.ai_menu_trans.Text = $"AI√: {provider.Name} - {mode.mode}";

                // --- B & C. 第二级(厂商) 和 第三级(模式) 遍历更新 ---
                foreach (ToolStripItem item in this.ai_menu_trans.DropDownItems)
                {   // 1. 【调试明确化】如果是分割线，直接跳过 (这样断点就不会停在 null 上了)
                    if (item is ToolStripSeparator)
                        continue;
                    // 跳过分割线，只处理菜单项
                    if (item is ToolStripMenuItem providerItem)
                    {
                        // 判断这是否是当前选中的厂商 (例如 "DeepSeek")
                        bool isTargetProvider = (providerItem.Text == provider.Name);

                        // 勾选/取消勾选 厂商菜单
                        providerItem.Checked = isTargetProvider;

                        // 如果这个厂商有子菜单 (即模式列表)，继续深入遍历
                        if (providerItem.HasDropDownItems)
                        {
                            foreach (ToolStripItem subItem in providerItem.DropDownItems)
                            {
                                if (subItem is ToolStripMenuItem modeItem)
                                {
                                    if (isTargetProvider)
                                    {
                                        // ★ 关键逻辑：只有在厂商匹配的情况下，才去比对模式名称
                                        // 这样可以避免不同厂商有同名模式(如"默认模式")导致的误勾选
                                        bool isTargetMode = (modeItem.Text == mode.mode);
                                        modeItem.Checked = isTargetMode;
                                    }
                                    else
                                    {
                                        // 如果厂商都不是这个，那它下面的模式肯定不能勾选
                                        modeItem.Checked = false;
                                    }
                                }
                            }
                        }
                    }
                }

                //// 3. 更新状态栏提示
                //if (ai_menu_trans != null)
                //    ai_menu_trans.Text = $"AI: {provider.Name} - {mode.mode}";

            }
            catch (Exception ex)
            {
                // ★★★ 如果报错，这里会弹窗告诉你原因 ★★★
                MessageBox.Show($"切换接口时发生错误：\n{ex.Message}\n\n堆栈信息：\n{ex.StackTrace}", "错误提示");
            }
        }





        /// <summary>
        /// OpenAICompatibleTrans Translate 执行入口 (被 Main_OCR_Thread 调用)
        /// </summary>
        // 注意返回值从 void 变成了 async Task
        public async Task<string> Trans_OpenAICompatible(string text,string fromLang,string toLang)
        {
            try
            {
                // 1. 检查配置
                if (this._currentCustomTransProvider == null)
                {
                    return "错误：未选择有效的翻译接口配置。请在菜单中重新选择";
                }
                string apiurl = _currentCustomTransProvider.ApiUrl.TrimEnd('/');
                if (!apiurl.EndsWith("/chat/completions")) apiurl += "/chat/completions";
                string system_prompt = _currentCustomTransMode.system_prompt;
                if (!string.IsNullOrEmpty(system_prompt))
                {
                    system_prompt = ReplaceLangPlaceholder(_currentCustomTransMode.system_prompt, fromLang, toLang);
                }
                string user_prompt= _currentCustomTransMode.prompt;
                if (!string.IsNullOrEmpty(user_prompt))
                {
                    user_prompt = ReplaceLangPlaceholder(_currentCustomTransMode.prompt, fromLang, toLang);

                }
                string assistant_prompt=_currentCustomTransMode.assistant_prompt;
                if (!string.IsNullOrEmpty(assistant_prompt))
                {
                    assistant_prompt = ReplaceLangPlaceholder(_currentCustomTransMode.assistant_prompt, fromLang, toLang);

                }
                AIMode aIMode = new AIMode
                {
                    mode = _currentCustomTransMode.mode,
                    description = _currentCustomTransMode.description,
                    system_prompt = system_prompt,
                    prompt = user_prompt,
                    assistant_prompt=assistant_prompt,
                    temperature = _currentCustomTransMode.temperature,
                    enable_thinking= _currentCustomTransMode.enable_thinking,
                    stream=_currentCustomTransMode.stream,
                    PromptOrder = new List<string>(_currentCustomTransMode.PromptOrder)

                };
                // 2. ★★★ 关键修改：使用 Task.Run 在后台执行耗时操作 ★★★
                // 这样主线程不会卡死，而且你可以使用 await 等待它完成
                string result = await Task.Run(() =>
                {
                    // 这里调用底层的同步方法（它内部用 .Result 阻塞，但阻塞的是后台线程，不影响UI）
                    return OpenAICompatibleTranslate.Translate(
                    text,
                    apiurl,
                    _currentCustomTransProvider.ApiKey,
                    _currentCustomTransProvider.ModelName,
                    aIMode
                    );
                });

                return result;
            }
            catch (Exception ex)
            {
                return "翻译接口调用出错: " + ex.Message;
            }
        }
        // 辅助方法：安全的字符串替换
        private string ReplaceLangPlaceholder(string template, string from, string to)
        {
            // 如果原字段是 null 或空，直接返回 null，保持副本结构一致
            if (string.IsNullOrEmpty(template)) return null;

            // 处理默认语言参数
            string f = string.IsNullOrEmpty(from) || from == "auto" ? "Auto Detect" : from;
            string t = string.IsNullOrEmpty(to) ? "Simplified Chinese" : to;

            // 执行替换
            return template.Replace("${fromlang}", f).Replace("${tolang}", t);
        }
    }
}