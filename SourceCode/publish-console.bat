@echo off
rem Publish the test console to ..\Published\F4CIO.AiChatBot.UiConsole (self-contained win-x64).
cd /d "%~dp0"
set "OUT=..\Published\F4CIO.AiChatBot.UiConsole"
echo Publishing UiConsole (Release, self-contained win-x64) to %OUT% ...
dotnet publish "F4CIO.AiChatBot.UiConsole\F4CIO.AiChatBot.UiConsole.csproj" -c Release -r win-x64 --self-contained true -o "%OUT%"
if errorlevel 1 ( echo PUBLISH FAILED & pause & exit /b 1 )
echo.
echo Done. Output: %OUT%
pause
