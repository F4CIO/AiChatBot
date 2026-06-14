@echo off
rem Build the React frontend and copy the static site to ..\Published\F4CIO.AiChatBot.UiWeb.
rem Copy that folder to the server. It includes run-uiweb.bat to serve it without Node.
cd /d "%~dp0"
set "OUT=..\Published\F4CIO.AiChatBot.UiWeb"
if not exist "F4CIO.AiChatBot.UiWeb\node_modules" call npm install --prefix "F4CIO.AiChatBot.UiWeb"
echo Building React frontend (Release) ...
call npm --prefix "F4CIO.AiChatBot.UiWeb" run build
if errorlevel 1 ( echo BUILD FAILED & pause & exit /b 1 )
echo Copying static files to %OUT% ...
if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%"
xcopy /e /i /y "F4CIO.AiChatBot.UiWeb\dist\*" "%OUT%\" >nul
copy /y "F4CIO.AiChatBot.UiWeb\deploy\run-uiweb.bat" "%OUT%\run-uiweb.bat" >nul
copy /y "F4CIO.AiChatBot.UiWeb\deploy\serve-static.ps1" "%OUT%\serve-static.ps1" >nul
echo.
echo Done. Output: %OUT%
echo Copy this folder to the server. Edit config.js (window.__API_BASE__) to your API URL,
echo then run run-uiweb.bat to serve it (or host the folder in IIS - see README.md).
pause
