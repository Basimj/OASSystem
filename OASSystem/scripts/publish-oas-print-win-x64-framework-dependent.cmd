@echo off
setlocal
cd /d "%~dp0\.."
dotnet publish OAS.Print.Desktop\OAS.Print.Desktop.csproj -c Release --self-contained false -p:UseAppHost=true -o publish\OAS.Print\framework-dependent
if errorlevel 1 exit /b 1
echo.
echo EXE: %CD%\publish\OAS.Print\framework-dependent\OAS.Print.exe
echo Requires .NET 9 Desktop Runtime on the target Windows PC.
pause
