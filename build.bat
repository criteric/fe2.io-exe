@echo off
setlocal
title FE2.IO Desktop Builder

echo ==========================================
echo        FE2.IO Desktop - Build
echo ==========================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [HATA] .NET SDK bulunamadi.
    echo.
    echo .NET 8 SDK kurulu olmali.
    echo Kontrol: dotnet --version
    echo.
    pause
    exit /b 1
)

echo [1/3] Proje restore ediliyor...
dotnet restore
if errorlevel 1 (
    echo.
    echo [HATA] NuGet paketleri indirilemedi.
    echo Internet baglantini kontrol et.
    pause
    exit /b 1
)

echo.
echo [2/3] x64 EXE olusturuluyor...
dotnet publish -c Release -r win-x64 --self-contained false ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o "dist"
if errorlevel 1 (
    echo.
    echo [HATA] Build basarisiz.
    pause
    exit /b 1
)

echo.
echo [3/3] Tamamlandi.
echo.
echo EXE:
echo %CD%\dist\FE2IO Desktop.exe
echo.
echo Not: Hedef bilgisayarda Microsoft Edge WebView2 Runtime gerekir.
echo.
pause
endlocal
