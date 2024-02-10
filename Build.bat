@echo off

echo.
echo ### Clean up ###
echo.

pushd .\Additionals\Scripts

call Clean.bat

popd

echo.
echo ### Build application ###
echo.

dotnet publish App -p:DebugType=None -p:DebugSymbols=false -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -p:PublishSingleFile=true --self-contained --configuration Release --runtime win-x64 --output _Publish

rem dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true

PAUSE