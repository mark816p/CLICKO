@echo off
setlocal
echo [CLICKO] Building standalone CLICKO.exe...

set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC_PATH%" (
    set CSC_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if not exist "%CSC_PATH%" (
    echo [ERROR] csc.exe compiler not found in standard .NET Framework locations.
    exit /b 1
)

"%CSC_PATH%" /nologo /target:winexe /optimize+ /win32icon:"%~dp0icon.ico" /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Web.Extensions.dll /out:"%~dp0CLICKO.exe" "%~dp0src\Win32.cs" "%~dp0src\DeviceDetector.cs" "%~dp0src\ClickStats.cs" "%~dp0src\StartupManager.cs" "%~dp0src\DashboardForm.cs" "%~dp0src\TrayContext.cs" "%~dp0src\Program.cs" "%~dp0src\IconGenerator.cs"

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] CLICKO.exe built successfully!
    echo Output: %~dp0CLICKO.exe
) else (
    echo [ERROR] Build failed with exit code %ERRORLEVEL%
)

endlocal
