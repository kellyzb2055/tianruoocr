using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using TrOCR.Helper; // 确保引用了 CustomAIProvider 类所在的命名空间

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
        /// 【核心方法】加载所有自定义 AI 接口到菜单
        /// 这个方法应该在 FmMain_Load 和 设置窗口关闭后被调用
        /// </summary>
        public void LoadCustomOpenAIMenus()
        {
            try
            {
                // 1. 获取父级菜单
                // 请确保你的菜单栏里有一个叫 "AI" 的项，Name 是 ai_menu
                ToolStripMenuItem parentMenu = this.ai_menu;
                if (parentMenu == null) return;

                // 2. 【关键】彻底清空 AI 菜单下的旧内容
                // 这行代码会把 Designer 里画的那个 "OpenAICompatible" 删掉
                // 这样 DeepSeek、GLM 就会直接显示在 AI 菜单下面
                //parentMenu.DropDownItems.Clear();//不加这个代码，手动修改designer.cs删除AI菜单下的OpenAICompatible菜单也行

                // 2. 清理旧的动态菜单
                // 我们约定：所有动态生成的菜单 Tag 都是 "DynamicProvider"
                for (int i = parentMenu.DropDownItems.Count - 1; i >= 0; i--)
                {
                    if (parentMenu.DropDownItems[i].Tag?.ToString() == "DynamicProvider")
                    {
                        parentMenu.DropDownItems.RemoveAt(i);
                    }
                }

                // 3. 读取厂商列表 (CustomOpenAIProviders.json)
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "CustomOpenAIProviders.json");
                if (!File.Exists(jsonPath)) return;

                var providers = JsonConvert.DeserializeObject<List<CustomAIProvider>>(File.ReadAllText(jsonPath));
                if (providers == null) return;

                // 添加一条分割线 (美观)
                parentMenu.DropDownItems.Add(new ToolStripSeparator { Tag = "DynamicProvider" });
                // === ★★★ 新增：读取上次保存的配置 ★★★ ===
                string lastProviderName = IniHelper.GetValue("OpenAICompatible", "LastProvider");
                string lastModeName = IniHelper.GetValue("OpenAICompatible", "LastMode");
                // 标记是否已经成功恢复了状态
                bool isRestored = false;

                // 4. 循环生成菜单
                foreach (var provider in providers)
                {
                   
                    // === 一级菜单：厂商名 (如 "DeepSeek") ===
                    ToolStripMenuItem providerItem = new ToolStripMenuItem(provider.Name);
                    providerItem.Tag = "DynamicProvider"; // 标记，方便下次删除

                    // 检查是否配置了 Prompt 配置文件 (例如 Data/AIOCRConfig.json)
                    bool hasSubMenu = false;
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
                                // 读取 Prompt 配置文件
                                string configJson = File.ReadAllText(configFullPath, Encoding.UTF8);
                                // ★ 使用您的实体类 AIConfig 进行解析
                                var configObj = JsonConvert.DeserializeObject<AIConfig>(configJson);

                                if (configObj != null && configObj.modes != null)
                                {
                                    foreach (var mode in configObj.modes)
                                    {
                                        // === 二级菜单：模式名 (如 "精确识别") ===
                                        ToolStripMenuItem modeItem = new ToolStripMenuItem(mode.mode);
                                        modeItem.ToolTipText = mode.description;

                                        // 点击事件：切换到该厂商 + 该模式
                                        modeItem.Click += (s, e) => SwitchToCustomAI(provider, mode);

                                        providerItem.DropDownItems.Add(modeItem);
                                        // === ★★★ 核心恢复逻辑 1 (有子菜单情况) ★★★ ===
                                        // 如果当前遍历到的厂商和模式，等于上次保存的 -> 自动切换过去
                                        if (!isRestored &&
                                            provider.Name == lastProviderName &&
                                            mode.mode == lastModeName)
                                        {
                                            SwitchToCustomAI(provider, mode);
                                            isRestored = true;
                                        }
                                    }
                                    hasSubMenu = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"加载 {provider.Name} 的子菜单失败: {ex.Message}");
                            }
                        }
                    }
                    // 创建一个临时的默认 mode
                    var defaultMode = new AIMode
                    {
                        mode = "默认模式",
                        prompt = "请识别图片中的文字：",
                        temperature = 0.5 // 默认温度
                    };

                    // 如果没有子菜单配置 (或者加载失败)，添加一个默认项
                    if (!hasSubMenu)
                    {
                       
                        ToolStripMenuItem defaultItem = new ToolStripMenuItem(defaultMode.mode);
                        defaultItem.Click += (s, e) =>
                        {
                   
                          
                            SwitchToCustomAI(provider, defaultMode);
                            // 如果循环完了还没找到上次用的（可能被删了），或者第一次运行
                            // 且当前 interface_flag 是 CustomOpenAI，则默认选第一个
                            //if (!isRestored && this.interface_flag == "CustomOpenAI" && parentMenu.DropDownItems.Count > 0)
                            //{
                            //    // 这里可以写逻辑自动选中列表里的第一个，防止 _currentCustomProvider 为空
                            //    // 简单处理：用户下次点击菜单时会生效，或者在这里强制选第一个
                            //}
                        };
                        providerItem.DropDownItems.Add(defaultItem);
                        
                    }

                    // 将厂商菜单加入到 "AI" 菜单下
                    parentMenu.DropDownItems.Add(providerItem);
                    // 如果还没恢复过，且当前厂商名字匹配 INI 记录
                    if (!isRestored && provider.Name == lastProviderName)
                    {
                        

                        // 只有当 INI 里的模式也是 "默认模式" (或者是空的) 时才恢复
                        // 这样比较严谨，防止未来有其他模式时误判
                        if (string.IsNullOrEmpty(lastModeName) || lastModeName == "默认模式")
                        {
                            SwitchToCustomAI(provider, defaultMode);
                            isRestored = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("加载自定义 AI 菜单总流程失败: " + ex.Message);
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

                // 重要：告诉程序当前选的是自定义类型
                //StaticValue.OCR_Current_API = "CustomOpenAI";
                // 2. ★★★ 关键修改：设置主窗体的接口标志 ★★★
                // 告诉 Main_OCR_Thread，现在要用“自定义接口”了
                //this.interface_flag = "CustomOpenAI";
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
                // 2. 准备 Prompt 参数 (如果模式为空，就给默认值)
                string sysPrompt = _currentCustomMode?.system_prompt ?? "";
                string userPrompt = _currentCustomMode?.prompt ?? "请识别图片中的文字";
                string assistPrompt = _currentCustomMode?.assistant_prompt;
                double? temp = _currentCustomMode?.temperature;
                bool? thinking = _currentCustomMode?.enable_thinking;
                string baseUrl = _currentCustomProvider.ApiUrl;
                string apiurl = baseUrl.TrimEnd('/');
                if (!apiurl.EndsWith("/chat/completions")) apiurl += "/chat/completions";

                // 3. ★★★ 直接调用 V3 接口 ★★★
                // 这里不需要 switch 判断，直接把参数传给 OpenAICompatibleHelper
                string result = OpenAICompatibleHelper.OCR_V3(
                    image_screen,
                    apiurl,   // URL
                    _currentCustomProvider.ApiKey,   // Key
                    _currentCustomProvider.ModelName,// Model
                    sysPrompt,
                    userPrompt,
                    assistPrompt,
                    temp,
                    thinking
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
        public void OCR_Custom_Router()
        {
            // 防御性检查
            if (_currentCustomProvider == null)
            {
                typeset_txt = "错误：未选择有效的接口配置。";
                split_txt = typeset_txt;
                return;
            }

            try
            {
                string result = "";

                // ★★★ 核心路由：根据配置的 Type 字段决定调用哪个 Helper ★★★
                // 假设您的 CustomAIProvider 类里已经加了 Type 字段
                //string type = _currentCustomProvider.Type ?? "OpenAI"; // 默认为 OpenAI
                string type =  "OpenAI"; // 默认为 OpenAI

                switch (type)
                {
                    //case "Anthropic":
                        // === Anthropic兼容模式 ===
                        // 约定：ApiUrl 存 AK, ApiKey 存 SK
                        //result = ClaudeHelper.GeneralBasic(
                        //    image_screen, // 截图
                        //    _currentCustomProvider.ApiUrl, // API Key
                        //    _currentCustomProvider.ApiKey  // Secret Key
                        //
                        //);
                        //break;

                    case "OpenAI":
                    default:
                        // === OpenAI 兼容模式 (DeepSeek, Kimi, etc.) ===
                        string sysPrompt = _currentCustomMode?.system_prompt ?? "";
                        string userPrompt = _currentCustomMode?.prompt ?? "请识别图片中的文字";

                        // 处理高级参数 (Temperature 等)
                        double? temp = _currentCustomMode?.temperature;
                        bool? thinking = _currentCustomMode?.enable_thinking;

                        result = OpenAICompatibleHelper.OCR_V3(
                            image_screen,
                            _currentCustomProvider.ApiUrl,
                            _currentCustomProvider.ApiKey,
                            _currentCustomProvider.ModelName,
                            sysPrompt,
                            userPrompt,
                            _currentCustomMode?.assistant_prompt,
                            temp,
                            thinking
                        );
                        break;
                }

                // 统一处理结果
                if (string.IsNullOrEmpty(result))
                {
                    typeset_txt = "接口返回为空。";
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
    }

    
}