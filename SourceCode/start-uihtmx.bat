
@echo off
rem Start the UiHtmx project from source (development run) in a new window and open browser.
cd /d "%~dp0"
echo Starting UiHtmx (dotnet run) from SourceCode folder in a new window...

set "ASPNETCORE_ENVIRONMENT=Development"

rem Launch the server in a new console window so this script can continue and open the browser.
start "UiHtmx" cmd /c "dotnet run --project ""F4CIO.AiChatBot.UiHtmx\F4CIO.AiChatBot.UiHtmx.csproj"" --no-launch-profile --urls ""https://localhost:5103;http://localhost:5104"""

if errorlevel 1 ( echo FAILED TO LAUNCH SERVER & pause & exit /b 1 )

rem Give the server a moment to start, then open the browser to the HTTPS endpoint.
timeout /t 2 /nobreak >nul
start "" "https://localhost:5103"

echo UiHtmx launched. Server window will remain open separately.
