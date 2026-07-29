@echo off
rem Publish the HTMX server to ..\Published\F4CIO.AiChatBot.UiHtmx for IIS (framework-dependent, not single-file).
cd /d "%~dp0"
set "OUT=..\Published\F4CIO.AiChatBot.UiHtmx"
echo Publishing UiHtmx (Release, framework-dependent, not single-file) to %OUT% ...
rem Framework-dependent publish (suitable for IIS with .NET Hosting Bundle installed)
dotnet publish "F4CIO.AiChatBot.UiHtmx\F4CIO.AiChatBot.UiHtmx.csproj" -c Release -r win-x64 --self-contained true -o "%OUT%"
if errorlevel 1 ( echo PUBLISH FAILED & pause & exit /b 1 )
echo.
echo Done. Output: %OUT%
pause
