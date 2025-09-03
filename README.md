## 天若 OCR 文字识别 v6

**天若 OCR 文字识别 `v6`** ：

**全新版本，全新出发！**

基于 https://github.com/AnyListen/tianruoocr 项目进行完善制作而成。

如果能帮到你，欢迎给个 **`star`**


## 开发内容
1. 更新版本号

2. 升级框架至 `.net framework 4.8.1`

3. 增强 **搜狗 ocr** 接口 稳定性

4. 修复失效 `ocr` 接口:

   - **百度 ocr**
   - **腾讯 ocr**

   修复失效翻译接口：

   - **百度翻译**
   - **腾讯翻译**

   增加 新的 `ocr` 接口：

   - **微信 ocr**
     - 目前只支持 x64 版，x86 版暂时无法使用
   - 更多接口查看 **Release** 版本日志

   增加 新的翻译接口：

   - **bing 翻译**
   - **Microsoft 翻译**
   - **Yandex 翻译**
   - 更多接口查看 **Release** 版本日志

5. 部分 **ocr 接口** 支持用户手动设置识别语言

6. **翻译接口** 支持用户手动设置源语言和目标语言，默认自动检测源语言和自动判断目标语言

7. 增加新功能：**输入翻译**

8. 增强 **查找替换** 功能：添加 **忽略大小写** 选项

9. 允许用户在设置里 **自定义右键菜单显示的 ocr 识别及翻译接口**

#### 已知问题

- **快速翻译功能** 会自动复制选中文本到粘贴板, 暂不修复
- **任务管理器** 里天若的进程名在识别文本后会变为耗时时间
- ......

#### 注意事项

- 进入 **设置** 页面，修改设置后，只有关闭掉设置页面，才会保存设置
- **设置 - 常规 - 音效 - 复制到剪贴板**：这个设置选项指的是识别结果自动复制到粘贴板，和音效无关，未来会改一下说明文本
- **google 翻译** 依旧需要正确的网络环境才能使用，如果网络环境不满足，翻译结果会提示 `Translation failed: 发送请求时出错。`
- 本人是 `win10` 系统，功能测试正常，在 其他系统下可能会有一些不符合预期的表现

欢迎 `issue` 反馈及 `PR` 贡献！

##  特别鸣谢
本项目的开发离不开以下优秀开源项目的支持，感谢以下优秀开源项目的支持：

<https://github.com/AnyListen/tianruoocr>：提供了基础代码

<https://github.com/wangfreexx/wangfreexx-tianruoocr-cl-paddle>：参考修复百度和腾讯翻译接口

<https://github.com/ZGGSONG/STranslate>：参考增加更多接口

<https://github.com/d4n3436/GTranslate>：基于它增加 Microsoft 翻译，Yandex 翻译，重构了 Google 翻译

<https://github.com/ZGGSONG/WeChatOcr>：基于此项目增加微信 ocr

<https://github.com/pot-app/pot-desktop>：参考增加更多接口





