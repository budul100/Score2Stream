@echo off
setlocal

rem Move to the parent of the script folder
pushd "%~dp0.."

echo.
echo Updating GitHub content in: "%cd%"
echo.

rem Remove all cached files from Git index
git rm -r -f --cached .

rem Re-add all files to the index
git add .

popd
endlocal

pause