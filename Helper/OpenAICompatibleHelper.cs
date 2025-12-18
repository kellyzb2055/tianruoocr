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
using System.Drawing.Imaging; // 用于获取 StartupPath

namespace TrOCR.Helper
{
    internal class OpenAICompatibleHelper
    {
       
        private static readonly HttpClient httpClient = new HttpClient();


        /// <summary>
        /// 执行 OCR 识别
        /// </summary>
        /// <returns>识别结果文本</returns>
        public static string OCR(Image image)
        {
            // 1. 基础配置校验
            string baseUrl = IniHelper.GetValue("OpenAICompatible", "BaseUrl");
            string apiKey = IniHelper.GetValue("OpenAICompatible", "APIKey");
            string modelName = IniHelper.GetValue("OpenAICompatible", "Model");
            string configJsonPath = IniHelper.GetValue("OpenAICompatible", "Config");

            if (string.IsNullOrEmpty(baseUrl)) return "错误：未配置 BaseUrl";
            if (string.IsNullOrEmpty(apiKey)) return "错误：未配置 API Key";
            if (string.IsNullOrEmpty(modelName)) return "错误：未配置模型";
            
            // 2. 读取提示词配置 (JSON)
            // 如果没有配置文件，使用默认的 Config 对象，防止报错
            AIConfig aiConfig = null;
            if (!string.IsNullOrEmpty(configJsonPath) && File.Exists(configJsonPath))
            {
                try 
                {
                    string jsonContent = File.ReadAllText(configJsonPath, Encoding.UTF8);
                    aiConfig = JsonConvert.DeserializeObject<AIConfig>(jsonContent);
                }
                catch(Exception ex) { 
                    return "读取配置文件出错"+ex.Message;
                }
            }

            // 获取当前模式（默认第一项，或者回退到默认值）
            AIMode currentMode = (aiConfig != null && aiConfig.modes != null && aiConfig.modes.Count > 0) 
                                ? aiConfig.modes[0] 
                                : new AIMode { // 默认模式
                                    mode="默认模式",
                                    system_prompt = "You are a professional OCR engine. Recognize the text in the image and output it directly. Do not use markdown code blocks. Do not output any conversational filler. Maintain the original line breaks. If the image contains code, remember to preserve the formatting.",
                                    prompt = "OCR this image.",
                                    temperature = 0.1,
                                    enable_thinking=false,

                                };

            try
            {
                // 3. 内存图片转 Base64 (直接使用传入的 Image 对象)
                string base64Image = ImageToBase64(image);
                if (string.IsNullOrEmpty(base64Image)) return "错误：图片转换失败";

                // 4. 构造请求体
                var requestBody = new
                {
                    model = string.IsNullOrEmpty(modelName) ? "gpt-4-vision-preview" : modelName,
                    messages = new object[]
                    {
                        new { role = "system", content = currentMode.system_prompt },
                        new {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = currentMode.prompt },
                                new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                            }
                        }
                    },
                    temperature = currentMode.temperature,
                    thinking = new
                    {
                        type = currentMode.enable_thinking?"enabled":"disabled",
                    },
                    //max_tokens = 4096
                };

                // 5. 发送请求
                string endpoint = baseUrl.TrimEnd('/');
                if (!endpoint.EndsWith("/chat/completions")) endpoint += "/chat/completions";

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
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
        public double temperature { get; set; }
        public bool enable_thinking { get; set; }
    }
}