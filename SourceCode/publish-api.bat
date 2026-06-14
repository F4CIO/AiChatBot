@echo off
rem Publish the backend API to ..\Published\F4CIO.AiChatBot.UiApi (self-contained win-x64).
cd /d "%~dp0"
set "OUT=..\Published\F4CIO.AiChatBot.UiApi"
echo Publishing UiApi (Release, self-contained win-x64) to %OUT% ...
dotnet publish "F4CIO.AiChatBot.UiApi\F4CIO.AiChatBot.UiApi.csproj" -c Release -r win-x64 --self-contained true -o "%OUT%"
if errorlevel 1 ( echo PUBLISH FAILED & pause & exit /b 1 )
echo.
echo Done. Output: %OUT%
pause
