@echo off
REM ==========================================================================
REM  SGC - sobe a retaguarda web no IIS Express
REM  http://localhost:8090   (usuario admin / senha cecons123)
REM ==========================================================================
cd /d "%~dp0"
"%ProgramFiles%\IIS Express\iisexpress.exe" /path:"%~dp0SGC.Web" /port:8090 /clr:v4.0
