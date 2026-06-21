@echo off
echo ==================================================
echo         天若 OCR 微信组件一键部署工具
echo ==================================================

set "DEST_DIR=%APPDATA%\Tencent\WeChat\XPlugin\Plugins\WeChatOCR\9999\extracted"
set "SRC_DIR="

if exist "wco_data\WeChatOCR.exe" set "SRC_DIR=wco_data"
if exist "WeChatOCR.exe" set "SRC_DIR=."

if not "%SRC_DIR%"=="" goto SRC_OK
echo [错误] 未能在当前位置找到微信 OCR 组件！
echo.
echo 请将本批处理脚本 (.bat) 放置于以下位置之一后再运行：
echo   1. 软件安装根目录下（即含有 wco_data 文件夹的目录）
echo   2. wco_data 文件夹内部（即含有 WeChatOCR.exe 的目录）
echo.
pause
exit /b

:SRC_OK
echo 目标路径: %DEST_DIR%
echo 源组件路径: %SRC_DIR%
echo.
echo 正在创建目标文件夹...
if not exist "%DEST_DIR%" mkdir "%DEST_DIR%"

echo 正在复制组件文件，请稍候...
xcopy "%SRC_DIR%\Model" "%DEST_DIR%\Model" /E /I /Y
xcopy "%SRC_DIR%\mmmojo.dll" "%DEST_DIR%\" /Y
xcopy "%SRC_DIR%\mmmojo_64.dll" "%DEST_DIR%\" /Y
xcopy "%SRC_DIR%\WeChatOCR.exe" "%DEST_DIR%\" /Y
xcopy "%SRC_DIR%\x64.config" "%DEST_DIR%\" /Y

echo.
if exist "%DEST_DIR%\WeChatOCR.exe" goto DEPLOY_OK
echo [错误] 部署失败，请确认是否具有 C 盘 AppData 的写入权限，或源文件是否完整！
goto END

:DEPLOY_OK
echo [成功] 微信 OCR 组件部署完成！
echo 目标路径已就绪，现在您可以重新开启软件并使用微信接口进行识别。

:END
echo.
pause
