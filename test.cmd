@echo off
setlocal

SET CONFIGURATION=Debug
SET PUBLIC=""

:loop
IF NOT "%1"=="" (
    IF "%1"=="-r" (
        SET CONFIGURATION=Release
    )
    SHIFT
    GOTO :loop
)

dotnet restore
dotnet test --no-build --no-restore -c %CONFIGURATION%
