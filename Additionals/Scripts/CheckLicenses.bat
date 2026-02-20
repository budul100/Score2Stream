@echo off
setlocal

rem Start in the parent of the script folder
pushd "%~dp0.."

echo.
echo License check based on: https://github.com/tomchavakis/nuget-license
echo The tool can be installed with:
echo     dotnet tool install --global dotnet-project-licenses
echo.
echo The analysis may take a moment...
echo.

rem Run license analysis with URLs included
dotnet-project-licenses -i "%cd%" -u

popd
endlocal

pause