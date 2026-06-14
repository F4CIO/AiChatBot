@echo off
rem Publish API + Console + Web in one go, all under ..\Published\F4CIO.AiChatbot.
cd /d "%~dp0"
echo === Publishing API ===
dotnet publish "F4CIO.AiChatBot.UiApi\F4CIO.AiChatBot.UiApi.csproj" -c Release -r win-x64 --self-contained true -o "..\Published\F4CIO.AiChatBot.UiApi"
if errorlevel 1 ( echo API PUBLISH FAILED & pause & exit /b 1 )
echo === Publishing Console ===
dotnet publish "F4CIO.AiChatBot.UiConsole\F4CIO.AiChatBot.UiConsole.csproj" -c Release -r win-x64 --self-contained true -o "..\Published\F4CIO.AiChatBot.UiConsole"
if errorlevel 1 ( echo CONSOLE PUBLISH FAILED & pause & exit /b 1 )
echo === Publishing Web ===
if not exist "F4CIO.AiChatBot.UiWeb\node_modules" call npm install --prefix "F4CIO.AiChatBot.UiWeb"
call npm --prefix "F4CIO.AiChatBot.UiWeb" run build
if errorlevel 1 ( echo WEB BUILD FAILED & pause & exit /b 1 )
if exist "..\Published\F4CIO.AiChatBot.UiWeb" rmdir /s /q "..\Published\F4CIO.AiChatBot.UiWeb"
mkdir "..\Published\F4CIO.AiChatBot.UiWeb"
xcopy /e /i /y "F4CIO.AiChatBot.UiWeb\dist\*" "..\Published\F4CIO.AiChatBot.UiWeb\" >nul
copy /y "F4CIO.AiChatBot.UiWeb\deploy\run-uiweb.bat" "..\Published\F4CIO.AiChatBot.UiWeb\run-uiweb.bat" >nul
copy /y "F4CIO.AiChatBot.UiWeb\deploy\serve-static.ps1" "..\Published\F4CIO.AiChatBot.UiWeb\serve-static.ps1" >nul
echo.
echo All done. Outputs under ..\Published\: F4CIO.AiChatBot.UiApi, F4CIO.AiChatBot.UiConsole, F4CIO.AiChatBot.UiWeb
pause
