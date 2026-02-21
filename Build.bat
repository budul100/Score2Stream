@echo off

echo.
echo ########################################
echo #          Build Score2Stream          #
echo ########################################
echo.

rem ----------------------------------------
rem Step 1: Clean up build artifacts
rem ----------------------------------------
echo [1/3] Cleaning up previous build artifacts...
echo.

call .\Additionals\Scripts\CleanContents.bat

echo.
echo Clean up completed successfully.
echo.

rem ----------------------------------------
rem Step 2: Read version from csproj
rem ----------------------------------------
for /f "delims=" %%i in ('powershell -Command "(Select-Xml -Path App\App.csproj -XPath '//VersionMajor').Node.InnerText + '.' + (Select-Xml -Path App\App.csproj -XPath '//VersionMinor').Node.InnerText + '.' + (Select-Xml -Path App\App.csproj -XPath '//VersionBuild').Node.InnerText"') do set APP_VERSION=%%i

echo Version: %APP_VERSION%
echo.

rem ----------------------------------------
rem Step 3: Build and publish Score2Stream
rem ----------------------------------------
echo [2/3] Building and publishing Score2Stream...
echo.

dotnet publish App\App.csproj ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -p:PublishReadyToRun=true ^
    --self-contained ^
    --configuration Release ^
    --runtime win-x64

echo.
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed with error code %ERRORLEVEL%.
    echo.
    echo ########################################
    echo #       Score2Stream Build Done        #
    echo ########################################
    echo.
    PAUSE
    exit /b %ERRORLEVEL%
)

echo Build and publish completed successfully.
echo Output directory: App\bin\Release\net8.0\win-x64\publish
echo.

rem ----------------------------------------
rem Step 4: Build Inno Setup installer
rem ----------------------------------------
echo [3/3] Building installer...
echo.

set INNO_COMPILER="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if not exist %INNO_COMPILER% (
    echo [ERROR] Inno Setup compiler not found at %INNO_COMPILER%.
    echo         Please install Inno Setup 6 from https://jrsoftware.org/isinfo.php
    echo         or adjust the path in this script.
    echo.
    echo ########################################
    echo #       Score2Stream Build Done        #
    echo ########################################
    echo.
    PAUSE
    exit /b 1
)

%INNO_COMPILER% "Additionals\Scripts\Setup.iss" /DAppVersion=%APP_VERSION%

echo.
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Installer build failed with error code %ERRORLEVEL%.
) else (
    echo Installer built successfully.
    echo Output directory: _Publish
)

echo.
echo ########################################
echo #       Score2Stream Build Done        #
echo ########################################
echo.

PAUSE