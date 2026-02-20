@echo off
setlocal enabledelayedexpansion

pushd %~dp0..\..

echo ================================================
echo C# Project Cleanup Script
echo ================================================

:: Check if we're in a project directory
if not exist "*.sln" if not exist "*.csproj" if not exist "*.vbproj" if not exist "*.fsproj" (
    echo WARNING: No Visual Studio project files found!
    echo Do you want to continue anyway? [y/n]
    set /p continue=
    if /i not "!continue!"=="y" if /i not "!continue!"=="yes" (
        echo Cancelled.
        goto :end
    )
)

:: Counters for deleted folders
set /a deleted_count=0
set /a error_count=0

echo.
echo Searching for bin and obj folders...
echo.

:: Delete bin folders
echo [INFO] Deleting bin folders...
for /f "tokens=*" %%G in ('dir /b /ad /s bin 2^>nul') do (
    echo Deleting: %%G
    rmdir /s /q "%%G" 2>nul
    if !errorlevel! equ 0 (
        set /a deleted_count+=1
    ) else (
        echo [ERROR] Could not delete: %%G
        set /a error_count+=1
    )
)

:: Delete obj folders
echo [INFO] Deleting obj folders...
for /f "tokens=*" %%G in ('dir /b /ad /s obj 2^>nul') do (
    echo Deleting: %%G
    rmdir /s /q "%%G" 2>nul
    if !errorlevel! equ 0 (
        set /a deleted_count+=1
    ) else (
        echo [ERROR] Could not delete: %%G
        set /a error_count+=1
    )
)

:: Optional: Delete other temporary files
echo.
echo [INFO] Deleting additional temporary files...

:: Delete .vs folders (Visual Studio Cache)
for /f "tokens=*" %%G in ('dir /b /ad /s .vs 2^>nul') do (
    echo Deleting VS Cache: %%G
    rmdir /s /q "%%G" 2>nul
    if !errorlevel! equ 0 (
        set /a deleted_count+=1
    )
)

:: Delete packages folder if present (old NuGet structure)
if exist "packages\" (
    echo Deleting packages folder...
    rmdir /s /q "packages" 2>nul
    if !errorlevel! equ 0 (
        set /a deleted_count+=1
    )
)

:: Delete temporary files
del /s /q *.tmp 2>nul
del /s /q *.log 2>nul
del /s /q *.cache 2>nul

:: Summary
echo.
echo ================================================
echo Cleanup completed!
echo ================================================
echo Successfully deleted folders: !deleted_count!
if !error_count! gtr 0 (
    echo Errors during deletion: !error_count!
)
echo.

:end
pause