@echo off
setlocal

if "%~1"=="" (
    echo Usage: add-migration ^<MigrationName^>
    echo Example: add-migration AddBuildingContactPersonsJson
    exit /b 1
)

pushd "%~dp0"
dotnet ef migrations add %1 ^
    --project Umea.se.EstateService.DataStore ^
    --startup-project Umea.se.EstateService.API
set EXITCODE=%ERRORLEVEL%
popd
exit /b %EXITCODE%
