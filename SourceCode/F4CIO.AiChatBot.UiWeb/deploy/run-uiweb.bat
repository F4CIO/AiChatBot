@echo off
rem Serve the published React static files on this machine (no Node required).
rem Uses a small PowerShell static server with SPA fallback. For production, hosting
rem the folder in IIS (see README.md) is recommended; this is a simple alternative.
cd /d "%~dp0"
echo F4CIO.AiChatBot web UI - static server
echo IMPORTANT: edit config.js so window.__API_BASE__ points at your API URL.
echo Serving http://localhost:5102  (Ctrl+C to stop)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0serve-static.ps1" -Port 5102 -Root "%~dp0."
pause
