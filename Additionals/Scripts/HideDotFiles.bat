@ECHO off

PUSHD %1

call :treeProcess
goto :eof

:treeProcess

FOR %%X IN (.*.*) DO ATTRIB +H %%X
FOR /D %%X IN (.*.*) DO ATTRIB +H %%X

for /D %%d in (*) do (
    cd %%d
    call :treeProcess
    cd ..
)

POPD

exit /b