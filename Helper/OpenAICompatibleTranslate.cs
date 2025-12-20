using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Diagnostics; // 用于获取 StartupPath

namespace TrOCR.Helper
{
    internal class OpenAICompatibleTranslate
    {

        private static readonly HttpClient httpClient = new HttpClient();

        // === 缓存相关的静态变量 ===
        // 线程锁，防止并发读写文件冲突
        private static readonly object _configLock = new object();
        // 缓存的配置对象（内存变量）
        private static AIConfig _cachedConfig = null;
        // 上次读取配置文件的时间
        private static DateTime _lastConfigWriteTime = DateTime.MinValue;
        // 用于保留顺序的 JObject
        private static JObject _cachedJsonRoot = null;
        // 是否已经显示过通知
        private static bool hasnotified = false;
        //通知相关代码移到fmmain.ai.translate.cs里比较好，暂时不移


        /// <summary>
        /// 执行 AI 翻译 / 文本处理
        /// </summary>
        /// <param name="inputContent">用户选中的文本（或输入框文本）</param>
        /// <param name="manualMode">当前选中的翻译模式（从菜单或配置获取）</param>
        /// <param name="targetLang">目标语言（如 "Simplified Chinese"）</param>
        /// <param name="sourceLang">源语言（如 "Auto Detect"）</param>
        /// <returns>翻译结果字符串</returns>
        public static string Translate(string inputContent, AIMode manualMode, string targetLang = "Simplified Chinese", string sourceLang = "Auto Detect")
        {
            Debug.WriteLine("传入AI翻译接口的模式是" + JsonConvert.SerializeObject(manualMode, Formatting.Indented));

            // 1. 从 StaticValue 缓存读取配置路径等信息
            string baseUrl = StaticValue.OpenAICompatible_Trans_BASE_URL;
            string apiKey = StaticValue.OpenAICompatible_Trans_API_KEY;
            string modelName = StaticValue.OpenAICompatible_Trans_MODEL;
            string configJsonPath = StaticValue.OpenAICompatible_Trans_CONFIG_PATH;

            // 基础校验
            if (string.IsNullOrEmpty(baseUrl)) return "错误：未配置 AI 翻译接口地址 (Base URL)";
            if (string.IsNullOrEmpty(apiKey)) return "错误：未配置 AI 翻译 API Key";
            if (string.IsNullOrEmpty(modelName)) return "错误：未配置 AI 翻译模型";

            // === 智能刷新配置逻辑 (检测文件变动，热重载) ===
            AIConfig freshConfig = null;
            JObject freshJsonRoot = null;

            lock (_configLock)
            {
                if (!string.IsNullOrEmpty(configJsonPath) && File.Exists(configJsonPath))
                {
                    try
                    {
                        // 获取配置文件当前的修改时间
                        var fileInfo = new FileInfo(configJsonPath);
                        DateTime currentWriteTime = fileInfo.LastWriteTime;

                        // 比较时间：如果文件被修改过(时间变了)，或者缓存是空的 -> 重新读取
                        if (_cachedConfig == null || currentWriteTime != _lastConfigWriteTime)
                        {
                            Debug.WriteLine("[配置更新] 检测到文件变化，正在重新读取...");
                            string jsonContent = File.ReadAllText(configJsonPath, Encoding.UTF8);
                            
                            // 1. 反序列化为强类型对象 (用于取值)
                            var loadedConfig = JsonConvert.DeserializeObject<AIConfig>(jsonContent);
                            // 2. 解析为 JObject (用于取顺序)
                            var loadedJsonRoot = JObject.Parse(jsonContent);

                            // === 检查配置文件类型是否为 translate ===
                            if (loadedConfig != null && !string.Equals(loadedConfig.type, "translate", StringComparison.OrdinalIgnoreCase))
                            {
                                return "配置错误：所选配置文件类型不匹配。\r\n当前文件 type 为: " + (loadedConfig.type ?? "null") + "，翻译 功能仅支持 type: \"translate\"。";
                            }
                            else
                            {
                                // 更新缓存和时间戳
                                _cachedConfig = loadedConfig;
                                _lastConfigWriteTime = currentWriteTime;
                                _cachedJsonRoot = loadedJsonRoot; // 更新 JObject 缓存
                            }
                        }

                        // 拿到最新的配置
                        freshConfig = _cachedConfig;
                        freshJsonRoot = _cachedJsonRoot;
                    }
                    catch (Exception ex)
                    {
                        return "读取配置文件出错" + ex.Message;
                    }
                }
            }

            // 1. 先准备一个“兜底”的内置安全模式 (Hardcoded Default)
            // 作用：当配置文件不存在、或者配置文件里找不到对应的模式时，使用这个模式。
            AIMode defaultSafeMode = new AIMode
            {
                mode = "默认模式翻译 (内置)",
                system_prompt = "You are a professional translator. Translate the user input directly, without any explanations",
                prompt = "Translate the following text. If it is in Chinese, translate to English. Otherwise, translate to Simplified Chinese. Do not explain:",
                temperature = 1.0,
                enable_thinking = false,
            };

            // 2. 确定使用的模式
            AIMode currentMode = null;
            JToken currentModeJToken = null; // 对应模式的 JToken 节点 (用于获取顺序)

            if (manualMode != null)
            {
                // 情况A：从菜单选中了特定模式
                // === 即使传入了 manualMode，也尝试从 freshConfig 里找同名的最新版 ===
                if (freshConfig != null && freshConfig.modes != null)
                {
                    var foundFreshMode = freshConfig.modes.FirstOrDefault(m => m.mode == manualMode.mode);
                    // 找到了就用新的（热更新生效），找不到就用传入的旧的
                    currentMode = foundFreshMode ?? manualMode;

                    // 同时找到对应的 JToken 节点
                    if (freshJsonRoot != null && freshJsonRoot["modes"] is JArray modesArray)
                    {
                        currentModeJToken = modesArray.FirstOrDefault(m => m["mode"]?.ToString() == manualMode.mode);
                    }
                }
                else
                {
                    currentMode = manualMode;
                }
            }
            else
            {
                // 情况B：没有特定模式（默认行为），尝试读取 Config 文件
                if (freshConfig != null && freshConfig.modes != null && freshConfig.modes.Count > 0)
                {
                    // 尝试读取配置文件中保存的“模式名称”
                    string savedModeName = TrOCRUtils.LoadSetting("OpenAICompatibleTrans", "SelectedMode", "");
                    AIMode foundMode = null;

                    if (!string.IsNullOrEmpty(savedModeName))
                    {
                        // 查找名称匹配的模式
                        foundMode = freshConfig.modes.FirstOrDefault(m => m.mode == savedModeName);

                        if (foundMode == null)
                        {
                            return $"配置错误：无法找到模式“{savedModeName}”。\r\n原因：该模式可能已被从配置文件中删除或重命名。\r\n解决方法：请点击菜单重新选择一个有效的模式。";
                        }
                        hasnotified = false;
                        currentMode = foundMode;
                        // 同时查找 JToken
                        if (freshJsonRoot != null && freshJsonRoot["modes"] is JArray modesArray)
                        {
                            currentModeJToken = modesArray.FirstOrDefault(m => m["mode"]?.ToString() == savedModeName);
                        }
                    }
                    else
                    {
                        // === 用户没选模式 (savedModeName 为空) ===
                        // 【优化策略】：优先尝试使用配置文件里的“第一个模式”
                        if (freshConfig.modes != null && freshConfig.modes.Count > 0)
                        {
                            // 自动选中第一个
                            currentMode = freshConfig.modes[0];

                            // 顺便帮用户查找对应的 JToken（以便保持顺序）
                            if (freshJsonRoot != null && freshJsonRoot["modes"] is JArray modesArray)
                            {
                                currentModeJToken = modesArray.FirstOrDefault(); // 取第一个 JObject
                            }
                            Debug.WriteLine($"用户未选模式，自动加载配置文件中的第一个模式: {currentMode.mode}");

                            IniHelper.SetValue("OpenAICompatibleTrans", "SelectedMode", currentMode.mode);


                            hasnotified = false;

                            if (!hasnotified)
                            {
                                CommonHelper.ShowHelpMsg("未选择模式，将使用配置文件里第一个模式");
                            }

                        }
                    }

                }
                else
                {
                    //CommonHelper.ShowHelpMsg("配置文件不存在，将使用程序内置的默认模式");
                    Debug.WriteLine("配置文件不存在，将使用程序内置的默认模式翻译");
                    // 4. 连配置文件都读不到 -> 直接用内置默认
                    currentMode = defaultSafeMode;
                    if (!hasnotified)
                    {
                        CommonHelper.ShowHelpMsg("配置文件不存在，将使用程序内置的默认翻译模式");
                    }
                    hasnotified = true;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[TranslateHelper] 最终使用的模式名称: {currentMode.mode}");
            System.Diagnostics.Debug.WriteLine($"[TranslateHelper] 最终使用的模式详情: {JsonConvert.SerializeObject(currentMode, Formatting.Indented)}");          

            try
            {
                // === 3. 动态组装请求体 (按 JSON 顺序) ===
                var messagesList = new List<object>();

                if (currentModeJToken != null && currentModeJToken is JObject modeObj)
                {
                    // 定义标记：是否已处理 User 消息
                    bool hasUserMessage = false;

                    // 【方案 A】: 按照 JSON 文件里的顺序遍历属性
                    foreach (var property in modeObj.Properties())
                    {
                        string key = property.Name;

                        // 1. 处理 System Prompt
                        if (key == "system_prompt")
                        {
                            string rawVal = currentMode.system_prompt; // 从强类型取值
                            if (!string.IsNullOrEmpty(rawVal))
                            {
                                string finalVal = rawVal.Replace("${tolang}", targetLang).Replace("${fromlang}", sourceLang);
                                messagesList.Add(new { role = "system", content = finalVal });
                            }
                        }
                        // 2. 处理 Assistant Prompt
                        else if (key == "assistant_prompt")
                        {
                            string rawVal = currentMode.assistant_prompt;
                            if (!string.IsNullOrEmpty(rawVal))
                            {
                                string finalVal = rawVal.Replace("${tolang}", targetLang).Replace("${fromlang}", sourceLang);
                                messagesList.Add(new { role = "assistant", content = finalVal });
                            }
                        }
                        // 3. 处理 User Prompt (prompt 键) -> 拼接用户输入
                        else if (key == "prompt")
                        {
                            hasUserMessage = true; // 标记已处理

                            string rawTemplate = currentMode.prompt;
                            
                            // 构造最终的用户内容 = Template + 输入文本
                            string finalUserContent = inputContent;

                            if (!string.IsNullOrEmpty(rawTemplate))
                            {
                                string finalTemplate = rawTemplate.Replace("${tolang}", targetLang).Replace("${fromlang}", sourceLang);
                                finalUserContent = finalTemplate + "\n\n" + inputContent;
                            }
                            
                            // 只要遇到了 "prompt" 键，就发送 User 消息 (包含输入文本)
                            messagesList.Add(new { role = "user", content = finalUserContent });
                        }
                    }

                    // === 【兜底逻辑】 ===
                    // 如果循环结束了，发现 JSON 里压根没写 "prompt" 键
                    // 强制补发用户输入的内容，防止请求为空
                    if (!hasUserMessage)
                    {
                        // 这里没有 Template 可用，直接发原文
                        messagesList.Add(new { role = "user", content = inputContent });
                    }
                }
                else
                {
                    // 【方案 B】: 兜底逻辑 (固定顺序 System -> Assistant -> User)
                    
                    // System
                    if (!string.IsNullOrEmpty(currentMode.system_prompt))
                    {
                        string val = currentMode.system_prompt.Replace("${tolang}", targetLang).Replace("${fromlang}", sourceLang);
                        messagesList.Add(new { role = "system", content = val });
                    }

                    // Assistant
                    if (!string.IsNullOrEmpty(currentMode.assistant_prompt))
                    {
                        string val = currentMode.assistant_prompt.Replace("${tolang}", targetLang).Replace("${fromlang}", sourceLang);
                        messagesList.Add(new { role = "assistant", content = val });
                    }

                    // User
                    string finalUserContent = inputContent;
                    if (!string.IsNullOrEmpty(currentMode.prompt))
                    {
                        string template = currentMode.prompt.Replace("${tolang}", targetLang).Replace("${fromlang}", sourceLang);
                        finalUserContent = template + "\n\n" + inputContent;
                    }
                    messagesList.Add(new { role = "user", content = finalUserContent });
                }

                // 4. 构造请求体
                var requestBody = new
                {
                    model = modelName,
                    messages = messagesList,
                    temperature = currentMode.temperature,
           
                    thinking = currentMode.enable_thinking.HasValue
                        ? new 
                        { 
                            type = currentMode.enable_thinking.Value ? "enabled" : "disabled" 
                        }
                        : null,
                        
                    stream=false
                };

                // 5. 发送 HTTP 请求
                // 处理 Endpoint 后缀
                string endpoint = baseUrl.TrimEnd('/');
                if (!endpoint.EndsWith("/chat/completions")) endpoint += "/chat/completions";
              
                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                var content = new StringContent(JsonConvert.SerializeObject(requestBody, jsonSettings), Encoding.UTF8, "application/json");

                // 设置请求头
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // 发送并等待结果
                var response = httpClient.PostAsync(endpoint, content).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode)
                {
                    return $"翻译请求失败: {response.StatusCode}\r\n详细信息: {responseString}";
                }

                // 6. 解析结果
                JObject resultJson = JObject.Parse(responseString);
                string textResult = resultJson["choices"]?[0]?["message"]?["content"]?.ToString();

                // 7. 思考内容过滤 (如果模型输出了 <think> 标签，可选择过滤或保留，这里默认直接返回)
                return textResult ?? "未收到有效翻译内容";
            }
            catch (Exception ex)
            {
                return $"发生异常: {ex.Message}";
            }
        }
    }
}