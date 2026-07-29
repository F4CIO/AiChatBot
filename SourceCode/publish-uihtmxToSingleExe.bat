@echo off
rem Publish the HTMX server to ..\Published\F4CIO.AiChatBot.UiHtmx (self-contained win-x64, single-file).
cd /d "%~dp0"
set "OUT=..\Published\F4CIO.AiChatBot.UiHtmxAsSingleExe"
echo Publishing UiHtmx (Release, self-contained win-x64, single-file) to %OUT% ...
dotnet publish "F4CIO.AiChatBot.UiHtmx\F4CIO.AiChatBot.UiHtmx.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o "%OUT%"
if errorlevel 1 ( echo PUBLISH FAILED & pause & exit /b 1 )
echo.
echo Done. Output: %OUT%
pause
