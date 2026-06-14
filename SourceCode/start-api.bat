@echo off
rem Run the backend API (Swagger UI at http://localhost:5100/swagger)
cd /d "%~dp0"
echo Starting F4CIO.AiChatBot API ...
echo Swagger UI: http://localhost:5100/swagger
echo (Set your ClaudeApiKey in F4CIO.AiChatBot.UiApi\appsettings.json first.)
dotnet run --project "F4CIO.AiChatBot.UiApi"
