using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics; // 用于获取 StartupPath
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrOCR.Helper
{
    internal class OpenAICompatibleHelper
    {
        

        /// <summary>
        /// 新版 OCR 接口 (V3)，支持多接口切换和高级参数
        /// </summary>
        /// <param name="image">截图</param>
        /// <param name="apiUrl">API 地址</param>
        /// <param name="apiKey">API 密钥</param>
        /// <param name="modelName">模型名称</param>
        /// <param name="systemPrompt">系统提示词</param>
        /// <param name="userPrompt">用户提示词</param>
        /// <param name="assistantPrompt">助手提示词(可选)</param>
        /// <param name="temperature">温度(可选)</param>
        /// <param name="enableThinking">开启思考(可选)</param>
        /// <returns>识别结果文本</returns>
        public static string OCR_V3(
            Image image,
            string apiUrl,
            string apiKey,
            string modelName,
            AIMode mode)
        {
            // 1. 基础校验
            if (image == null) return "错误：图片为空";
            if (string.IsNullOrEmpty(apiUrl)) return "错误：API 地址未配置";
            if (string.IsNullOrEmpty(apiKey)) return "错误：API Key 未配置";
            if (string.IsNullOrEmpty(modelName)) return "错误：模型未配置";
            if (mode == null) return "错误：模式配置为空";


            try
            {
                string systemPrompt = mode.system_prompt;
                string userPrompt = mode.prompt;
                string assistantPrompt = mode.assistant_prompt;

                // 3. 图片转 Base64
                string base64Image = ImageToBase64(image);

                // 4. 构造 Messages 数组
                var messages = new List<object>();

                // (1) System Prompt
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }

                // (2) User Prompt (混合图文)
                // 标准 OpenAI Vision 格式
                var userContent = new List<object>();
                if (!string.IsNullOrEmpty(userPrompt))
                {
                    userContent.Add(new
                    { 
                        type = "text", 
                        text = userPrompt 
                    });
                }

                // 添加图片
                userContent.Add(new
                {
                    type = "image_url",
                    image_url = new { url = $"data:image/jpeg;base64,{base64Image}" }
                });

                messages.Add(new { role = "user", content = userContent });

                // (3) Assistant Prompt (可选，用于引导输出开头)
                if (!string.IsNullOrEmpty(assistantPrompt))
                {
                    messages.Add(new { role = "assistant", content = assistantPrompt });
                }

                // 5. 构造请求体 (Request Body)
                // 5.1. 直接定义包含所有字段的匿名对象 (哪怕是 null 也没关系)
                var requestBody = new
                {
                    model = modelName,
                    messages = messages,         
                    temperature = mode.temperature,
                    thinking = (mode.enable_thinking == true)
                    ? (object)new { type = "enabled" }//如果加上budget_tokens = 4096这个参数，设置思考最大token花费值，可限制思考花费的 Token 数（防止它想太久太贵）
                    : (object)new { type = "disabled" }, // <--- 显式发送 disabled
                    stream = mode.stream
                };

                // 序列化 JSON
                // 5.2配置序列化设置：忽略 Null 值，过滤掉所有 value 为 null 的字段
                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonPostData = JsonConvert.SerializeObject(requestBody,jsonSettings);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonPostData);

                // 6. 发起 HTTP 请求
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", "Bearer " + apiKey);
                request.ContentLength = byteArray.Length;
                request.Timeout = 60000; // 60秒超时

                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }

                // 7. ★★★ 智能响应处理 ★★★
                StringBuilder sb = new StringBuilder();

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) // 强转为 HttpWebResponse 以方便获取 Headers
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    // A. 获取 Content-Type
                    string contentType = response.ContentType?.ToLower() ?? "";

                    // B. 判断是否为流式响应 (SSE)
                    bool isEventStream = contentType.Contains("text/event-stream");

                    // === 分支 1：处理流式响应 ===
                    if (isEventStream)
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (string.IsNullOrEmpty(line)) continue;
                            if (line == "data: [DONE]") break;

                            if (line.StartsWith("data: "))
                            {
                                string json = line.Substring(6);
                                try
                                {
                                    JObject obj = JObject.Parse(json);
                                    // 流式格式：choices[0].delta.content
                                    var content = obj["choices"]?[0]?["delta"]?["content"]?.ToString();
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        sb.Append(content);
                                    }
                                }
                                catch { /* 忽略流式解析中的单行错误 */ }
                            }
                        }
                    }
                    // === 分支 2：处理普通 JSON 响应 ===
                    else
                    {
                        // 一次性读取所有内容
                        string jsonResponse = reader.ReadToEnd();
                        try
                        {
                            JObject obj = JObject.Parse(jsonResponse);

                            // 1. 检查是否有错误信息
                            if (obj["error"] != null)
                            {
                                return $"API 报错: {obj["error"]["message"]}";
                            }

                            // 2. 普通格式：choices[0].message.content
                            // 注意：这里是 message，不是 delta
                            var content = obj["choices"]?[0]?["message"]?["content"]?.ToString();

                            if (!string.IsNullOrEmpty(content))
                            {
                                sb.Append(content);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 如果解析失败，返回原始文本方便调试
                            return $"解析响应失败: {ex.Message} \n原始内容: {jsonResponse}";
                        }
                    }
                }

                return sb.ToString().Trim();
            }
            catch (WebException webEx)
            {
                // 增加对 WebException 的详细处理，读取服务器返回的错误文本
                if (webEx.Response != null)
                {
                    using (var errStream = webEx.Response.GetResponseStream())
                    using (var reader = new StreamReader(errStream))
                    {
                        return $"请求被拒绝: {reader.ReadToEnd()}";
                    }
                }
                return $"网络错误: {webEx.Message}";
            }
            catch (Exception ex)
            {
                return $"API 请求失败: {ex.Message}";
            }
        }

        // --- 辅助方法：计算图片哈希 ---
        // private static string GetImageHash(Bitmap bmp)
        // {
        //     try
        //     {
        //         using (MemoryStream ms = new MemoryStream())
        //         {
        //             bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        //             byte[] bytes = ms.ToArray();
        //             using (MD5 md5 = MD5.Create())
        //             {
        //                 byte[] hash = md5.ComputeHash(bytes);
        //                 return BitConverter.ToString(hash).Replace("-", "").ToLower();
        //             }
        //         }
        //     }
        //     catch
        //     {
        //         return DateTime.Now.Ticks.ToString(); // 降级处理
        //     }
        // }
        private static readonly HttpClient httpClient = new HttpClient();

        // ===  缓存相关的静态变量 ===
        // 线程锁，防止并发读写文件冲突
        private static readonly object _configLock = new object();
        // 缓存的配置对象（内存变量）
        private static AIConfig _cachedConfig = null;
        // 上次读取配置文件的时间
        private static DateTime _lastConfigWriteTime = DateTime.MinValue;
        //用于保留顺序的 JObject
        private static JObject _cachedJsonRoot = null; 
        // 是否已经显示过通知
        private static bool hasnotified = false; 
        //通知相关代码移到fmmain.ai.cs里比较好，暂时不移

        /// <summary>
        /// 执行 OCR 识别
        /// </summary>
        /// <returns>识别结果文本</returns>
        public static string OCR(Image image, AIMode manualMode = null)
        {
            Debug.WriteLine("传入AI接口的模式是" + JsonConvert.SerializeObject(manualMode, Formatting.Indented));
            // 1. 基础配置校验
            string baseUrl = StaticValue.OpenAICompatible_OCR_BASE_URL;
            string apiKey = StaticValue.OpenAICompatible_OCR_API_KEY;
            string modelName = StaticValue.OpenAICompatible_OCR_MODEL;
            string configJsonPath = StaticValue.OpenAICompatible_OCR_CONFIG_PATH;

            if (string.IsNullOrEmpty(baseUrl)) return "错误：未配置 BaseUrl";
            if (string.IsNullOrEmpty(apiKey)) return "错误：未配置 API Key";
            if (string.IsNullOrEmpty(modelName)) return "错误：未配置模型";
            

            // === 智能刷新配置逻辑 (检测文件变动，热重载) ===
            AIConfig freshConfig = null;
            JObject freshJsonRoot = null;
            
            // 加锁确保安全
            lock (_configLock)
            {
                if (!string.IsNullOrEmpty(configJsonPath) && File.Exists(configJsonPath))
                {
                    try
                    {
                        // 获取配置文件当前的修改时间 (不读取内容，速度极快)
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

                            // 检查配置文件类型是否为 ocr
                            if (loadedConfig != null && !string.Equals(loadedConfig.type, "ocr", StringComparison.OrdinalIgnoreCase))
                            {
                                return "配置错误：所选配置文件类型不匹配。\r\n当前文件 type 为: " + (loadedConfig.type ?? "null") + "，OCR 功能仅支持 type: \"ocr\"。";

                            }
                            else
                            {
                                // 更新缓存和时间戳
                                _cachedConfig = loadedConfig;
                                _lastConfigWriteTime = currentWriteTime;
                                _cachedJsonRoot = loadedJsonRoot; // 更新 JObject 缓存
                            }
                        }
                        
                        // 拿到最新的配置（不管是刚读的，还是缓存的）
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
            AIMode defaultSafeMode = new AIMode
            {
                mode = "默认模式 (内置)",
                system_prompt = "You are a professional OCR engine. Recognize the text in the image and output it directly. Do not use markdown code blocks. Do not output any conversational filler. Maintain the original line breaks. If the image contains code, remember to preserve the formatting.",
                prompt = "OCR this image.",
                temperature = 0.1,
                enable_thinking = false,
            };

            // 2. 确定使用的模式
            AIMode currentMode = null;
            JToken currentModeJToken = null; // 对应模式的 JToken 节点

            if (manualMode != null)
            {
                // 情况A：从菜单选中了特定模式
                // === 【优化】即使传入了 manualMode，也尝试从 freshConfig 里找同名的最新版 ===
                // 这样用户修改了配置文件后，不需要切换菜单就能生效
                if (freshConfig != null && freshConfig.modes != null)
                {
                    var foundFreshMode = freshConfig.modes.FirstOrDefault(m => m.mode == manualMode.mode);
                    // 找到了就用新的（热更新生效），找不到就用传入的旧的
                    currentMode = foundFreshMode ?? manualMode;
                    // 同时找到对应的 JToken 节点 (为了顺序)
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
                // 情况B：没有特定模式（默认行为），尝试从 freshConfig 读取
                if (freshConfig != null && freshConfig.modes != null && freshConfig.modes.Count > 0)
                {
                    // 2. 尝试读取配置文件中保存的“模式名称”
                    string savedModeName = TrOCRUtils.LoadSetting("OpenAICompatible", "SelectedMode","");
                    AIMode foundMode = null;

                    if (!string.IsNullOrEmpty(savedModeName))
                    {
                        // 查找名称匹配的模式
                        foundMode = freshConfig.modes.FirstOrDefault(m => m.mode == savedModeName);
                        if (foundMode == null)
                        {
                            // === 【直接报错】 ===
                            return $"配置错误：无法找到模式“{savedModeName}”。\r\n原因：该模式可能已被从配置文件中删除或重命名。\r\n解决方法：请点击菜单重新选择一个有效的模式。";
                        }
                        // === 【新增优化】 ===
                        // 既然成功找到了有效模式，说明配置是好的。
                        // 我们重置通知标记，以便下次如果用户又改坏了，能再次收到通知。
                        hasnotified = false;
                        currentMode = foundMode;
                        // 只有找到了对应的 Mode，才有必要去 JSON 里找它的顺序
                        if (freshJsonRoot != null && freshJsonRoot["modes"] is JArray modesArray)
                        {
                            currentModeJToken = modesArray.FirstOrDefault(m => m["mode"]?.ToString() == savedModeName);
                        }
                    }else
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
                          
                            IniHelper.SetValue("OpenAICompatible", "SelectedMode", currentMode.mode);

                           
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
                    // Debug.WriteLine("配置文件不存在或无效，将使用程序内置的默认模式");
                    // 4. 连配置文件都读不到 -> 直接用内置默认
                    currentMode = defaultSafeMode;
                    if (!hasnotified)
                    {
                        CommonHelper.ShowHelpMsg("配置文件不存在，将使用程序内置的默认模式");
                    }
                    hasnotified=true;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[Helper] 最终使用的模式名称: {currentMode.mode}");
            System.Diagnostics.Debug.WriteLine($"[Helper] 最终使用的模式详情: {JsonConvert.SerializeObject(currentMode, Formatting.Indented)}");
            
            try
            {
                // 3. 内存图片转 Base64 (直接使用传入的 Image 对象)
                string base64Image = ImageToBase64(image);
                if (string.IsNullOrEmpty(base64Image)) return "错误：图片转换失败";

                // === 4. 动态组装请求体 ===
                var messagesList = new List<object>();
                if (currentModeJToken != null && currentModeJToken is JObject modeObj)
                {
                    // 1. 定义一个标记，记录循环里是否处理过 User 消息
                    bool hasUserMessage = false;
                    // 【方案 A】: 如果能读到 JObject，就按照 JSON 文件里的顺序遍历属性
                    foreach (var property in modeObj.Properties())
                    {
                        string key = property.Name;
                        
                        // 1. 处理 System Prompt
                        if (key == "system_prompt")
                        {
                            string val = currentMode.system_prompt; // 从强类型取值，确保数据准确
                            if (!string.IsNullOrEmpty(val))
                                messagesList.Add(new { role = "system", content = val });
                        }
                        // 2. 处理 Assistant Prompt
                        else if (key == "assistant_prompt")
                        {
                            string val = currentMode.assistant_prompt;
                            if (!string.IsNullOrEmpty(val))
                                messagesList.Add(new { role = "assistant", content = val });
                        }
                        // 3. 处理 User Prompt (prompt) -> 必须附带图片
                        else if (key == "prompt")
                        {
                            // 标记：我们在循环里找到了 prompt 键
                            hasUserMessage = true;
                            string val = currentMode.prompt;
                            var userContentList = new List<object>();
                            if (!string.IsNullOrEmpty(val)) // 即使为空也需要发图片
                            {
                                userContentList.Add(new { type = "text", text = val });                              
                            }
                            // 立刻把图片加进去，并立刻把消息加入列表
                            // 这样才能保证它出现在 JSON 指定的位置
                            userContentList.Add(new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } });

                            messagesList.Add(new { role = "user", content = userContentList });
                        }
                    }
                    // === 【兜底逻辑】 ===
                    // 循环跑完了，如果发现 hasUserMessage 依然是 false
                    // 说明 JSON 文件里根本没有 "prompt" 这个键
                    if (!hasUserMessage)
                    {
                        // 手动补发一个只包含图片的 User 消息
                        var userContentList = new List<object>();
                        userContentList.Add(new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } });
                        
                        messagesList.Add(new { role = "user", content = userContentList });
                    }
                }
                else
                {
                    // 【方案 B】: 如果读不到文件 (比如用的内置默认模式)，则回退到固定顺序 (System -> Assistant -> User)
                    if (!string.IsNullOrEmpty(currentMode.system_prompt))
                        messagesList.Add(new { role = "system", content = currentMode.system_prompt });

                    if (!string.IsNullOrEmpty(currentMode.assistant_prompt))
                        messagesList.Add(new { role = "assistant", content = currentMode.assistant_prompt });

                    // User 总是放在最后
                    var userContentList = new List<object>();
                    if (!string.IsNullOrEmpty(currentMode.prompt))
                    {   
                        userContentList.Add(new { type = "text", text = currentMode.prompt });
                    }

                    // 图片是必须添加的
                    userContentList.Add(new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } });

                    
                    messagesList.Add(new { role = "user", content = userContentList });
                }

                
                // 4.2 构造请求体
                var requestBody = new
                {
                    // 1. 必填项：直接赋值
                    model = modelName,
                    messages = messagesList,

                    // 2. 温度 (Temperature) - 处理可空数值
                    temperature = currentMode.temperature,

                    // 3. 思考模式 (Thinking) - 处理复杂的嵌套对象逻辑
                    thinking = currentMode.enable_thinking.HasValue
                    // [分支 1] 如果 HasValue 为 true
                    ? new
                    {
                        type = currentMode.enable_thinking.Value ? "enabled" : "disabled"
                    }
                    // [分支 2] 如果 HasValue 为 false
                    : null,

                    stream=false
                };
            

                // 5. 发送请求
                string endpoint = baseUrl.TrimEnd('/');
                if (!endpoint.EndsWith("/chat/completions")) endpoint += "/chat/completions";
                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody, jsonSettings), Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = httpClient.PostAsync(endpoint, content).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode) return $"API 请求失败: {response.StatusCode}\n{responseString}";

                JObject resultJson = JObject.Parse(responseString);
                string textResult = resultJson["choices"]?[0]?["message"]?["content"]?.ToString();

                return textResult ?? "未收到有效内容";
            }
            catch (Exception ex)
            {
                return $"发生异常: {ex.Message}";
            }
        }

        /// <summary>
        /// 将图片转换为 Base64 字符串
        /// </summary>
        private static string ImageToBase64(Image image)
        {
            try
            {
                if (image == null) return null;
                using (MemoryStream m = new MemoryStream())
                {
                    // 统一保存为 Jpeg 格式以减小体积，或者使用 image.RawFormat
                    image.Save(m, ImageFormat.Jpeg); 
                    byte[] imageBytes = m.ToArray();
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                return null;
            }
        }

        //public static void ResetCache()
        //{
        //    lock (_configLock)
        //    {
        //        _cachedConfig = null;           // 清除强类型对象缓存
        //        _cachedJsonRoot = null;         // 清除 JSON 树缓存
        //        _lastConfigWriteTime = DateTime.MinValue; // 重置时间戳
        //        hasnotified = false;            // 允许重新发送通知
        //        Debug.WriteLine("AI 配置缓存已手动重置");
        //        Debug.WriteLine("[缓存重置] 已强制清除配置缓存，下次调用将重新读取文件。");
        //    }
        //}
    }

    // 实体类定义
    public class AIConfig
    {
        public string type { get; set; }
        public List<AIMode> modes { get; set; }
    }

    public class AIMode
    {
        public string mode { get; set; }
        public string? description { get; set; }
        public string? system_prompt { get; set; }
        //user_prompt
        public string? prompt { get; set; }
        public string? assistant_prompt { get; set; }
        // 改为可空类型 (double?)，如果 json 里没填，值为 null
        public double? temperature { get; set; }

        // 改为可空类型 (bool?)，如果 json 里没填，值为 null
        public bool? enable_thinking { get; set; }
        public bool? stream { get;set; }
    }
}