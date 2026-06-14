@echo off
rem Regenerate the TypeScript API client from the running API's OpenAPI doc (NSwag).
rem The API must be running first (start-api.bat -> http://localhost:5100).
cd /d "%~dp0"
echo Regenerating the TypeScript API client (NSwag) ...
call npm --prefix "F4CIO.AiChatBot.UiWeb" run gen:api
echo.
pause
