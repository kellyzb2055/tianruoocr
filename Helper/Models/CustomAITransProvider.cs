using Newtonsoft.Json;
using System;

namespace TrOCR.Helper.Models 
{
    public class CustomAITransProvider : CustomAIProvider
    {

        //源语言
        // ★★★ 添加 Order 属性将它们移到最后 ★★★
        [JsonProperty(Order = 100)]
        public string Source { get; set; } = "auto detect";
        //目标语言
        // ★★★ 添加 Order 属性将它们移到最后 ★★★
        [JsonProperty(Order = 101)]
        public string Target { get; set; } = "自动判断";

      
    }
}