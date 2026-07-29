@echo off
rem Run the published UiHtmx single-file executable from the Published folder.
cd /d "%~dp0"
echo Starting UiHtmx from the published folder (if present)...
if exist "..\..\Published\F4CIO.AiChatBot.UiHtmx\F4CIO.AiChatBot.UiHtmx.exe" (
  pushd "..\..\Published\F4CIO.AiChatBot.UiHtmx"
  echo Running F4CIO.AiChatBot.UiHtmx.exe
  F4CIO.AiChatBot.UiHtmx.exe
  popd
) else (
  echo Published executable not found. Run publish-uihtmx.bat first.
)
pause
