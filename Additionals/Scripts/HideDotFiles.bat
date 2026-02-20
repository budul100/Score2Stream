@echo off
setlocal

rem Start in the parent of the script folder
pushd "%~dp0.."

echo.
echo Hiding dot-files and dot-folders under: "%cd%"
echo.

rem /b  = bare names
rem /s  = recurse
rem /a  = include all (including hidden/system)
rem ".*" pattern includes names that start with a dot

for /f "delims=" %%F in ('dir /b /s /a .*') do (

    rem Skip the special entries . and ..
    if /i not "%%~nxF"=="." if /i not "%%~nxF"==".." (
        attrib +h "%%~fF"
    )

)

popd
endlocal

pause