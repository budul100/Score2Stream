@echo off

echo.
echo ########################################
echo #          Build Score2Stream          #
echo ########################################
echo.

rem ----------------------------------------
rem Step 1: Clean up build artifacts
rem ----------------------------------------
echo [1/2] Cleaning up previous build artifacts...
echo.

call .\Additionals\Scripts\CleanContents.bat

echo.
echo Clean up completed successfully.
echo.

rem ----------------------------------------
rem Step 2: Build and publish Score2Stream
rem ----------------------------------------
echo [2/2] Building and publishing Score2Stream...
echo.

dotnet publish App\App.csproj ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishReadyToRun=true ^
    -p:PublishSingleFile=true ^
    --self-contained ^
    --configuration Release ^
    --runtime win-x64 ^
    --output _Publish

rem Uncomment the line below to publish for Linux x64 instead:
rem dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true

echo.
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed with error code %ERRORLEVEL%.
) else (
    echo Build and publish completed successfully.
    echo Output directory: _Publish
)

echo.
echo ########################################
echo #       Score2Stream Build Done        #
echo ########################################
echo.

PAUSE