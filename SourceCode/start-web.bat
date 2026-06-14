@echo off
rem Run the React UI dev server (http://localhost:5101)
cd /d "%~dp0"
if not exist "F4CIO.AiChatBot.UiWeb\node_modules" echo Installing dependencies (first run, one moment) ...
if not exist "F4CIO.AiChatBot.UiWeb\node_modules" call npm install --prefix "F4CIO.AiChatBot.UiWeb"
echo Starting React dev server: http://localhost:5101
call npm --prefix "F4CIO.AiChatBot.UiWeb" run dev
