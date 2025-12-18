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

            // 1. 从 StaticValue 缓存读取配置
            string baseUrl = StaticValue.OpenAICompatible_Trans_BASE_URL;
            string apiKey = StaticValue.OpenAICompatible_Trans_API_KEY;
            string modelName = StaticValue.OpenAICompatible_Trans_MODEL;
            string configJsonPath = StaticValue.OpenAICompatible_Trans_CONFIG_PATH;

            // 基础校验
            if (string.IsNullOrEmpty(baseUrl)) return "错误：未配置 AI 翻译接口地址 (Base URL)";
            if (string.IsNullOrEmpty(apiKey)) return "错误：未配置 AI 翻译 API Key";
            if (string.IsNullOrEmpty(modelName)) return "错误：未配置 AI 翻译模型";
            AIMode currentMode = null;
            if (manualMode != null)
            {
                // 情况A：从菜单选中了特定模式
                currentMode = manualMode;
            }
            else
            {
                // 情况B：没有特定模式（默认行为），尝试读取 Config 文件
                // 如果没有配置文件，使用默认的 Config 对象，防止报错
                AIConfig aiConfig = null;
                if (!string.IsNullOrEmpty(configJsonPath) && File.Exists(configJsonPath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(configJsonPath, Encoding.UTF8);
                        aiConfig = JsonConvert.DeserializeObject<AIConfig>(jsonContent);
                        // === 检查配置文件类型是否为 translate ===
                        // 如果 type 字段存在但不是 "translate" (忽略大小写)，则报错
                        if (aiConfig != null && !string.Equals(aiConfig.type, "translate", StringComparison.OrdinalIgnoreCase))
                        {
                            return "配置错误：所选配置文件类型不匹配。\r\n当前文件 type 为: " + (aiConfig.type ?? "null") + "，翻译 功能仅支持 type: \"translate\"。";
                        }
                    }
                    catch (Exception ex)
                    {
                        return "读取配置文件出错" + ex.Message;
                    }
                }
               
                // 1. 先准备一个“兜底”的内置安全模式 (Hardcoded Default)
                // 作用：当配置文件不存在、或者配置文件里找不到对应的模式时，使用这个模式。
                AIMode defaultSafeMode = new AIMode
                {
                    mode = "默认模式翻译 (内置)",
                    system_prompt = " You are a professional translator. Translate the user input directly, without any explanations",
                    prompt = "Translate the following text. If it is in Chinese, translate to English. Otherwise, translate to Simplified Chinese. Do not explain:",
                    temperature = 0.7,
                    enable_thinking = false,
                };

                if (aiConfig != null && aiConfig.modes != null && aiConfig.modes.Count > 0)
                {
                    // 2. 尝试读取配置文件中保存的“模式名称”
                    string savedModeName = TrOCRUtils.LoadSetting("OpenAICompatibleTrans", "SelectedMode","");
                    AIMode foundMode = null;

                    if (!string.IsNullOrEmpty(savedModeName))
                    {
                        // 查找名称匹配的模式
                        foundMode = aiConfig.modes.FirstOrDefault(m => m.mode == savedModeName);
                    }

                    // 3. 赋值逻辑：
                    // 如果找到了保存的模式 -> 使用找到的 (foundMode)
                    // 如果【没找到】(foundMode is null) -> 使用 defaultSafeMode (内置默认)
                    //currentMode = foundMode ?? defaultSafeMode;
                    if (foundMode == null)
                    {
                        // === 【直接报错】 ===
                        return $"配置错误：无法找到模式“{savedModeName}”。\r\n原因：该模式可能已被从配置文件中删除或重命名。\r\n解决方法：请点击菜单重新选择一个有效的模式。";
                    }

                    currentMode = foundMode;
                }
                else
                {
                    //CommonHelper.ShowHelpMsg("配置文件不存在，将使用程序内置的默认模式");
                    Debug.WriteLine("配置文件不存在，将使用程序内置的默认模式翻译");
                    // 4. 连配置文件都读不到 -> 直接用内置默认
                    currentMode = defaultSafeMode;
                    
                }

            }
            System.Diagnostics.Debug.WriteLine($"[TranslateHelper] 最终使用的模式名称: {currentMode.mode}");
            System.Diagnostics.Debug.WriteLine($"[TranslateHelper] 最终使用的模式详情: {JsonConvert.SerializeObject(currentMode, Formatting.Indented)}");          

            try
            {
                // 2. 处理 Prompt 中的占位符
                // 如果 system_prompt 里写了 {target_lang}，就替换成 UI 传进来的值
                // 如果没写（比如润色模式），Replace 不会生效，保持原样，不会报错
                string finalSystemPrompt = (currentMode.system_prompt ?? "")
                    .Replace("{target_lang}", targetLang)
                    .Replace("{source_lang}", sourceLang);
                string finalUserPrompt = (currentMode.prompt ?? "")
                    .Replace("{target_lang}", targetLang)
                    .Replace("{source_lang}", sourceLang);

                // 可选优化：如果 UI 选的是 "自动检测"，把 Prompt 里的 "from Auto Detect" 稍微润色一下
                //if (sourceLang == "Auto Detect" || sourceLang == "Auto")
                //{
                //    finalSystemPrompt = finalSystemPrompt.Replace("from Auto Detect", "by detecting the source language automatically");
                //    finalUserPrompt = finalUserPrompt.Replace("from Auto Detect", "by detecting the source language automatically");
                //}

                // === 动态组装请求体 ===

                // 1. 动态构建 messages 列表
                var messagesList = new List<object>();

                // (A) System Message: 有才加
                if (!string.IsNullOrEmpty(currentMode.system_prompt))
                {
                    messagesList.Add(new { role = "system", content = finalSystemPrompt });
                }
           
                // (B) 添加 User Message (包含 inputContent)
                // 逻辑：将 Config 里的 Prompt 和 用户输入的 inputContent 拼接，或者作为 User 消息
                // 对于翻译，最简单直接的方式是：System给指令，User给原文

                string finalUserContent = inputContent;

                // 如果配置里还有额外的 prompt (例如 "Translate this code:")，拼接在前面
                if (!string.IsNullOrEmpty(currentMode.prompt))
                {
                    finalUserContent = finalUserPrompt + "\n\n" + inputContent;
                }

                // 必须把输入文本加进去！
                messagesList.Add(new { role = "user", content = finalUserContent });
                
                // 调试日志 
                System.Diagnostics.Debug.WriteLine($"[Translate] System Prompt: {finalSystemPrompt}");
                System.Diagnostics.Debug.WriteLine($"[Translate] User Prompt: {finalUserContent}");

                // 3. 构造请求体 (翻译不需要 image_url，只需要纯文本)
                //var messagesList = new List<object>
                //{
                //    new { role = "system", content = finalSystemPrompt },
                //    new { role = "user", content = inputContent } // 直接放入用户文本
                //};

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
                        : null
                };

                // 4. 发送 HTTP 请求
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

                // 5. 解析结果
                JObject resultJson = JObject.Parse(responseString);
                string textResult = resultJson["choices"]?[0]?["message"]?["content"]?.ToString();

                // 6. 思考内容过滤 (如果模型输出了 <think> 标签，可选择过滤或保留，这里默认直接返回)
                return textResult ?? "未收到有效翻译内容";
            }
            catch (Exception ex)
            {
                return $"发生异常: {ex.Message}";
            }
        }
    }
}