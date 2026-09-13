@echo off
setlocal
title FE2.IO Native Builder

echo ==========================================
echo        FE2.IO Native - Build
echo ==========================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] .NET SDK not found.
    echo.
    echo .NET 8 SDK must be installed.
    echo Check with: dotnet --version
    echo.
    pause
    exit /b 1
)

echo [1/3] Restoring project...
dotnet restore
if errorlevel 1 (
    echo.
    echo [ERROR] Failed to restore NuGet packages.
    echo Check your internet connection.
    pause
    exit /b 1
)

echo.
echo [2/3] Building x64 EXE...
dotnet publish -c Release -r win-x64 --self-contained false ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o "dist"
if errorlevel 1 (
    echo.
    echo [ERROR] Build failed.
    pause
    exit /b 1
)

echo.
echo [3/3] Done.
echo.
echo EXE:
echo %CD%\dist\FE2IO Native.exe
echo.
echo Note: since this is not self-contained, the target machine needs the
echo .NET 8 Desktop Runtime installed. If you want it to run on machines
echo without .NET installed, change --self-contained false to
echo --self-contained true in this script (bigger EXE, but no runtime needed).
echo.
pause
endlocal
