@echo off
REM ==========================================================================
REM  SGC - compila a solution (bibliotecas + validacao das paginas do site)
REM  Requer Visual Studio com a carga de trabalho ASP.NET / .NET Framework 4.8
REM ==========================================================================
setlocal
cd /d "%~dp0"

set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
for /f "usebackq tokens=*" %%i in (`%VSWHERE% -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set MSBUILD=%%i

if "%MSBUILD%"=="" (
    echo MSBuild nao encontrado. Instale o Visual Studio.
    exit /b 1
)

"%MSBUILD%" SGC.sln /nologo /v:m /p:Configuration=Debug
exit /b %errorlevel%
