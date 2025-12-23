using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using TrOCR.Helper;
using TrOCR.Helper.Models; 

namespace TrOCR
{
    public partial class FmMain
    {
        // === 全局状态变量 ===

        // 当前选中的厂商 (例如: DeepSeek)
        private CustomAIProvider _currentCustomProvider = null;

        // 当前选中的模式 (例如: 精确识别，包含 Prompt/Temperature 等)
        private AIMode _currentCustomMode = null;

        /// <summary>
        /// 【重构版】加载所有自定义 AI 接口到菜单，并自动恢复状态
        /// </summary>
        public void LoadCustomOpenAIMenus()
        {
            try
            {
                // ================= Step 1: 基础清理与准备 =================
                ToolStripMenuItem parentMenu = this.ai_menu;
                if (parentMenu == null) return;

                // 清理旧的动态菜单
                for (int i = parentMenu.DropDownItems.Count - 1; i >= 0; i--)
                {
                    if (parentMenu.DropDownItems[i].Tag?.ToString() == "DynamicProvider")
                    {
                        parentMenu.DropDownItems.RemoveAt(i);
                    }
                }

                // 读取厂商列表(CustomOpenAIProviders.json)
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "CustomOpenAIProviders.json");
                if (!File.Exists(jsonPath)) return;

                var providers = JsonConvert.DeserializeObject<List<CustomAIProvider>>(File.ReadAllText(jsonPath));
                if (providers == null) return;

                // 添加分割线
                parentMenu.DropDownItems.Add(new ToolStripSeparator { Tag = "DynamicProvider" });

                // 读取 INI 记录 (上次选了谁)
                string lastProviderName = IniHelper.GetValue("OpenAICompatible", "LastProvider");
                string lastModeName = IniHelper.GetValue("OpenAICompatible", "LastMode");

                // 标记：是否已经成功恢复了某个选项（防止多个厂商都去恢复）
                bool isRestored = false;

                // ================= Step 2: 循环构建菜单 =================
                foreach (var provider in providers)
                {
                    // 2.1 创建厂商菜单项 (一级)
                    ToolStripMenuItem providerItem = new ToolStripMenuItem(provider.Name);
                    providerItem.Tag = "DynamicProvider";
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
                            prompt = "请识别图片中的文字：",
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
                        modeItem.Click += (s, e) => SwitchToCustomAI(provider, mode);

                        providerItem.DropDownItems.Add(modeItem);

                        // --- 判断逻辑：尝试恢复 ---
                        // 只有当“还没恢复过” 且 “厂商名对上了” 且 “模式名对上了”
                        if (!isRestored && provider.Name == lastProviderName && mode.mode == lastModeName)
                        {
                            SwitchToCustomAI(provider, mode);
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
                            SwitchToCustomAI(provider, fallbackMode);

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
                if (!isRestored && this.interface_flag == "CustomOpenAI")
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
        private void SwitchToCustomAI(CustomAIProvider provider, AIMode mode)
        {
            try
            {
                // 1. 更新全局变量
                this._currentCustomProvider = provider;
                this._currentCustomMode = mode;

                // 告诉程序当前选的是自定义类型
                //StaticValue.OCR_Current_API = "CustomOpenAI";
       
                // 2. ★★★ 调用标准切换流程 ★★★
                // 这会自动设置 interface_flag = "CustomOpenAI" 并调用 Refresh() 重置菜单
                OCR_foreach("CustomOpenAI");
                /// === ★★★ 新增：保存选择到配置文件 ★★★ ===
                // 这样下次启动时，我们就能知道上次选的是谁
                try
                {
                    IniHelper.SetValue("OpenAICompatible", "LastProvider", provider.Name);
                    IniHelper.SetValue("OpenAICompatible", "LastMode", mode.mode);
                    // 同时也把主接口设为 CustomOpenAI (虽然 OCR_foreach 会做，但这里双重保险)
                    IniHelper.SetValue("配置", "接口", "CustomOpenAI");
                }
                catch { /* 忽略保存错误 */ }

                // ================== 2. UI 视觉更新 ==================

                // --- A. 第一级：更新 "AI" 主菜单 ---
                //this.ai_menu.Checked = true; // 给 "AI" 大标题打勾
                 // 更新显示文本 (例如: "AI: DeepSeek - 精确识别")，直观提示用户
                this.ai_menu.Text = $"AI√: {provider.Name} - {mode.mode}";

                // --- B & C. 第二级(厂商) 和 第三级(模式) 遍历更新 ---
                foreach (ToolStripItem item in this.ai_menu.DropDownItems)
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
                //if (ai_menu != null)
                //    ai_menu.Text = $"AI: {provider.Name} - {mode.mode}";

            }
            catch (Exception ex)
            {
                // ★★★ 如果报错，这里会弹窗告诉你原因 ★★★
                MessageBox.Show($"切换接口时发生错误：\n{ex.Message}\n\n堆栈信息：\n{ex.StackTrace}", "错误提示");
            }
        }

        /// <summary>
        /// OCR 执行入口 (需要在 Main_OCR_Thread 中调用)
        /// </summary>
        //public void OCR_OpenAICompatible()
        //{
        //    try
        //    {
        //        // 防御性检查
        //        if (this._currentCustomProvider == null)
        //        {
        //            typeset_txt = "错误：未选择 AI 接口。请在菜单中选择一个接口。";
        //            split_txt = typeset_txt;
        //            return;
        //        }

        //        // 准备 Prompt (优先用 Config 里的，没有就用默认值)
        //        // 注意：您的实体类里有 assistant_prompt，这里也一并提取
        //        string sysPrompt = _currentCustomMode?.system_prompt ?? "";
        //        string userPrompt = _currentCustomMode?.prompt ?? "请识别图片文字";
        //        string assistPrompt = _currentCustomMode?.assistant_prompt ?? "";

        //        // 处理可空类型 (如果 json 没写，传 null 给 helper，让 helper 决定是否发字段)
        //        double? temp = _currentCustomMode?.temperature;
        //        bool? thinking = _currentCustomMode?.enable_thinking;

        //        Debug.WriteLine("--------------------------------------------------");
        //        Debug.WriteLine($"[FmMain] 开始自定义 OCR: {_currentCustomProvider.Name}");
        //        Debug.WriteLine($"[FmMain] 模型: {_currentCustomProvider.ModelName}");
        //        Debug.WriteLine($"[FmMain] Temp: {temp}, Thinking: {thinking}");

        //        // ★★★ 调用 Helper ★★★
        //        // 您需要更新 OpenAICompatibleHelper.OCR_V3 方法，让它接收这些新参数
        //        string result = OpenAICompatibleHelper.OCR_V3(
        //            image_screen,
        //            _currentCustomProvider.ApiUrl,
        //            _currentCustomProvider.ApiKey,
        //            _currentCustomProvider.ModelName,
        //            sysPrompt,
        //            userPrompt,
        //            assistPrompt, // 新增参数
        //            temp,         // 新增参数
        //            thinking      // 新增参数
        //        );

        //        if (string.IsNullOrEmpty(result))
        //            typeset_txt = "接口返回为空。";
        //        else
        //            typeset_txt = result;

        //        split_txt = typeset_txt;
        //    }
        //    catch (Exception ex)
        //    {
        //        typeset_txt = $"接口调用出错: {ex.Message}";
        //        split_txt = typeset_txt;
        //    }
        //}
        /// <summary>
        /// 自定义 AI 接口的执行入口
        /// </summary>
        public void OCR_OpenAICompatible()
        {
            // 1. 防御性检查
            if (_currentCustomProvider == null)
            {
                typeset_txt = "错误：未选择有效的接口配置。请在菜单中重新选择。";
                split_txt = typeset_txt;
                return;
            }

            try
            {
                // 2. 准备 模式 参数 (如果模式为空，就给默认值)，这里就不处理了，想处理可以在OpenAICompatibleHelper的ocr方法里处理
                //string userPrompt = _currentCustomMode?.prompt ?? "请识别图片中的文字，只说识别结果";
                string apiurl = _currentCustomProvider.ApiUrl.TrimEnd('/');
                if (!apiurl.EndsWith("/chat/completions")) apiurl += "/chat/completions";



                // 3. ★★★ 直接调用 V3 接口 ★★★
                // 这里不需要 switch 判断，直接把参数传给 OpenAICompatibleHelper
                string result = OpenAICompatibleHelper.OCR(
                    image_screen,
                    apiurl,
                    _currentCustomProvider.ApiKey,
                    _currentCustomProvider.ModelName,
                   _currentCustomMode
                );

                // 4. 处理结果
                if (string.IsNullOrEmpty(result))
                {
                    typeset_txt = "接口返回为空，请检查网络或 Key 是否正确。";
                }
                else
                {
                    typeset_txt = result;
                }
                split_txt = typeset_txt;
            }
            catch (Exception ex)
            {
                typeset_txt = $"接口调用出错: {ex.Message}";
                split_txt = typeset_txt;
            }
        }
        //下面是一种未来可选的优化方式，使用总路由方法，根据不同的 Type 调用不同的 接口实现，需要配合：
        /*类要实现Type字段
         * if (interface_flag == "CustomOpenAI")
        {
            OCR_Custom_Router();
            fmloading.FmlClose = "窗体已关闭";
            Invoke(new OcrThread(Main_OCR_Thread_last));
            return;
        }
        */
        /// <summary>
        /// 自定义接口的统一执行入口 (支持 OpenAI/Anthropic 等多种协议)
        /// </summary>
        //public void OCR_Custom_Router()
        //{
        //    // 防御性检查
        //    if (_currentCustomProvider == null)
        //    {
        //        typeset_txt = "错误：未选择有效的接口配置。";
        //        split_txt = typeset_txt;
        //        return;
        //    }

        //    try
        //    {
        //        string result = "";

        //        // ★★★ 核心路由：根据配置的 Type 字段决定调用哪个 Helper ★★★
        //        // 假设您的 CustomAIProvider 类里已经加了 Type 字段
        //        //string type = _currentCustomProvider.Type ?? "OpenAI"; // 默认为 OpenAI
        //        string type =  "OpenAI"; // 默认为 OpenAI

        //        switch (type)
        //        {
        //            //case "Anthropic":
        //                // === Anthropic兼容模式 ===
        //                // 约定：ApiUrl 存 AK, ApiKey 存 SK
        //                //result = ClaudeHelper.GeneralBasic(
        //                //    image_screen, // 截图
        //                //    _currentCustomProvider.ApiUrl, // API Key
        //                //    _currentCustomProvider.ApiKey  // Secret Key
        //                //
        //                //);
        //                //break;

        //            case "OpenAI":
        //            default:
        //                // === OpenAI 兼容模式 (DeepSeek, Kimi, etc.) ===
        //                string sysPrompt = _currentCustomMode?.system_prompt ?? "";
        //                string userPrompt = _currentCustomMode?.prompt ?? "请识别图片中的文字";

        //                // 处理高级参数 (Temperature 等)
        //                double? temp = _currentCustomMode?.temperature;
        //                bool? thinking = _currentCustomMode?.enable_thinking;

        //                //result = OpenAICompatibleHelper.OCR_V3(
        //                //    image_screen,
        //                //    _currentCustomProvider.ApiUrl,
        //                //    _currentCustomProvider.ApiKey,
        //                //    _currentCustomProvider.ModelName,
        //                //    sysPrompt,
        //                //    userPrompt,
        //                //    _currentCustomMode?.assistant_prompt,
        //                //    temp,
        //                //    thinking
        //                //);
        //                break;
        //        }

        //        // 统一处理结果
        //        if (string.IsNullOrEmpty(result))
        //        {
        //            typeset_txt = "接口返回为空。";
        //        }
        //        else
        //        {
        //            typeset_txt = result;
        //        }
        //        split_txt = typeset_txt;
        //    }
        //    catch (Exception ex)
        //    {
        //        typeset_txt = $"接口调用出错: {ex.Message}";
        //        split_txt = typeset_txt;
        //    }
        //}
    }
}