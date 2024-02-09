@echo off

echo.
echo License check based on https://github.com/tomchavakis/nuget-license
echo The tool can be installed using dotnet tool install --global dotnet-project-licenses
echo.
echo The analyse can take a moment ...

dotnet-project-licenses -i %~dp0..\.. -u

PAUSE