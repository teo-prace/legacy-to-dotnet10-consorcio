@echo off
REM ==========================================================================
REM  Portal do consorciado (ASP classico) no IIS Express - http://localhost:8091
REM  Requer o componente SGCBoleto registrado: vb6\SGCBoleto\compilar.ps1
REM ==========================================================================
"%ProgramFiles%\IIS Express\iisexpress.exe" /path:"%~dp0portal" /port:8091
