@echo off
cd %~dp0

SETLOCAL
SET CACHED_NUGET="%LocalAppData%\NuGet\NuGet.exe"

IF EXIST %CACHED_NUGET% goto copynuget
echo Downloading latest version of NuGet.exe...
IF NOT EXIST "%LocalAppData%\NuGet" md "%LocalAppData%\NuGet"
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%CACHED_NUGET%'"

:copynuget
IF EXIST .nuget\nuget.exe goto restore
md .nuget
copy %CACHED_NUGET% .nuget\nuget.exe > nul

:restore
IF EXIST packages\Sake goto run
.nuget\nuget.exe install Sake -version 0.2 -o packages -ExcludeVersion

:run
.nuget\nuget.exe restore
packages\Sake\tools\Sake.exe -f makefile.shade %*