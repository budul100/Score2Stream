@echo off

pushd %~dp0..\..

ECHO.
ECHO Update GitHub content in %cd%
ECHO.

git rm -r -f --cached .
git add .

popd