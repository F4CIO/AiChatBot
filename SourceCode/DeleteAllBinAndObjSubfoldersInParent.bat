ECHO Source: https://stackoverflow.com/questions/39597481/windows-cmd-which-removes-every-bin-and-obj-folders
cd..
for /d /r . %%d in (bin obj) do @if exist "%%d" rd /s/q "%%d"
pause