@echo off
setlocal
cd /d "%~dp0\.."
dotnet publish OAS.Print.Desktop\OAS.Print.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\OAS.Print\win-x64
if errorlevel 1 exit /b 1
echo.
echo EXE: %CD%\publish\OAS.Print\win-x64\OAS.Print.exe
pause
