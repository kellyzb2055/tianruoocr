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
    internal class OpenAICompatibleHelper
    {
       
        private static readonly HttpClient httpClient = new HttpClient();


        /// <summary>
        /// 执行 OCR 识别
        /// </summary>
        /// <returns>识别结果文本</returns>
        public static string OCR(Image image, AIMode manualMode = null)
        {
            Debug.WriteLine("传入AI接口的模式是" + JsonConvert.SerializeObject(manualMode, Formatting.Indented));
            // 1. 基础配置校验
            string baseUrl = IniHelper.GetValue("OpenAICompatible", "BaseUrl");
            string apiKey = IniHelper.GetValue("OpenAICompatible", "APIKey");
            string modelName = IniHelper.GetValue("OpenAICompatible", "Model");
            string configJsonPath = IniHelper.GetValue("OpenAICompatible", "Config");

            if (string.IsNullOrEmpty(baseUrl)) return "错误：未配置 BaseUrl";
            if (string.IsNullOrEmpty(apiKey)) return "错误：未配置 API Key";
            if (string.IsNullOrEmpty(modelName)) return "错误：未配置模型";
            

          
            // 2. 确定使用的模式
            AIMode currentMode = null;
            if (manualMode != null)
            {
                // 情况A：从菜单选中了特定模式
                currentMode = manualMode;
            }
            else
            {   // 情况B：没有特定模式（默认行为），尝试读取 Config 文件
                // 如果没有配置文件，使用默认的 Config 对象，防止报错
                AIConfig aiConfig = null;
                if (!string.IsNullOrEmpty(configJsonPath) && File.Exists(configJsonPath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(configJsonPath, Encoding.UTF8);
                        aiConfig = JsonConvert.DeserializeObject<AIConfig>(jsonContent);
                        // === 【新增功能 1】检查配置文件类型是否为 ocr ===
                        // 如果 type 字段存在但不是 "ocr" (忽略大小写)，则报错
                        if (aiConfig != null && !string.Equals(aiConfig.type, "ocr", StringComparison.OrdinalIgnoreCase))
                        {
                            return "配置错误：所选配置文件类型不匹配。\r\n当前文件 type 为: " + (aiConfig.type ?? "null") + "，OCR 功能仅支持 type: \"ocr\"。";
                        }
                    }
                    catch (Exception ex)
                    {
                        return "读取配置文件出错" + ex.Message;
                    }
                }

                // 获取当前模式（默认第一项，或者回退到默认值）
                 currentMode = (aiConfig != null && aiConfig.modes != null && aiConfig.modes.Count > 0)
                                    ? aiConfig.modes[0]
                                    : new AIMode
                                    { // 默认模式
                                        mode = "默认模式",
                                        system_prompt = "You are a professional OCR engine. Recognize the text in the image and output it directly. Do not use markdown code blocks. Do not output any conversational filler. Maintain the original line breaks. If the image contains code, remember to preserve the formatting.",
                                        prompt = "OCR this image.",
                                        temperature = 0.1,
                                        enable_thinking = false,

                                    };
            }
            System.Diagnostics.Debug.WriteLine($"[Helper] 最终使用的模式名称: {currentMode.mode}");
            System.Diagnostics.Debug.WriteLine($"[Helper] 最终使用的模式详情: {JsonConvert.SerializeObject(currentMode, Formatting.Indented)}");
            try
            {
                // 3. 内存图片转 Base64 (直接使用传入的 Image 对象)
                string base64Image = ImageToBase64(image);
                if (string.IsNullOrEmpty(base64Image)) return "错误：图片转换失败";

                // === 4. 动态组装请求体 ===

                // 4.1 动态构建 messages 列表
                var messagesList = new List<object>();

                // (A) System Message: 有才加
                if (!string.IsNullOrEmpty(currentMode.system_prompt))
                {
                    messagesList.Add(new { role = "system", content = currentMode.system_prompt });
                }
                // (B) User Message Content: 动态构建
                var userContentList = new List<object>();

                // 只有当 prompt 不为空时，才添加 text 类型的节点
                if (!string.IsNullOrEmpty(currentMode.prompt))
                {
                    userContentList.Add(new { type = "text", text = currentMode.prompt });
                }

                // 图片是必须添加的
                userContentList.Add(new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } });

                // 将构建好的 content 放入 user message
                messagesList.Add(new
                {
                    role = "user",
                    content = userContentList
                });

                //var requestBody = new
                //{
                //    model =  modelName,
                //    messages = new object[]
                //    {
                //        new { role = "system", content = currentMode.system_prompt },
                //        new {
                //            role = "user",
                //            content = new object[]
                //            {
                //                new { type = "text", text = currentMode.prompt },
                //                new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                //            }
                //        }
                //    },
                //    temperature = currentMode.temperature,
                //    thinking = new
                //    {
                //        type = currentMode.enable_thinking?"enabled":"disabled",
                //    },
                //    //max_tokens = 4096
                //};
                // 4.2 构造请求体
                var requestBody = new
                {
                    // 1. 必填项：直接赋值
                    model = modelName,
                    messages = messagesList,

                    // 利用可空类型 + Ignore Null
                    // 2. 温度 (Temperature) - 处理可空数值
                    // currentMode.temperature 是 double? 类型 (可空)
                    // ---------------------------------------------------------
                    // 情况 A：如果配置文件里没写 temperature，它是 null。
                    //         -> requestBody.temperature = null
                    //         -> 序列化时被忽略，JSON 里完全没有 "temperature" 字段。
                    //         -> 接口收到请求后，会使用它自己的默认值 (比如 1.0)。
                    //
                    // 情况 B：如果配置文件写了 0.5。
                    //         -> requestBody.temperature = 0.5
                    //         -> 序列化结果： "temperature": 0.5
                    temperature = currentMode.temperature,

                    // 3. 思考模式 (Thinking) - 处理复杂的嵌套对象逻辑
                    // currentMode.enable_thinking 是 bool? 类型 (可空)
                    // ---------------------------------------------------------
                    thinking = currentMode.enable_thinking.HasValue
                    // [分支 1] 如果 HasValue 为 true (即配置文件里写了 true 或 false)
                    ? new
                    {
                        // 再进行一次判断：如果是 true -> "enabled", 如果是 false -> "disabled"
                        type = currentMode.enable_thinking.Value ? "enabled" : "disabled"
                    }
                    // [分支 2] 如果 HasValue 为 false (即配置文件里没写，或者删了)
                    : null
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
        public string description { get; set; }
        public string system_prompt { get; set; }
        public string prompt { get; set; }
        // 改为可空类型 (double?)，如果 json 里没填，值为 null
        public double? temperature { get; set; }

        // 改为可空类型 (bool?)，如果 json 里没填，值为 null
        public bool? enable_thinking { get; set; }
    }
}