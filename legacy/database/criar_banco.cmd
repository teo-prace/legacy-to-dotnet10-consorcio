@echo off
REM ==========================================================================
REM  SGC - recria o banco do zero
REM  Uso: criar_banco.cmd [servidor]
REM       padrao: (localdb)\MSSQLLocalDB
REM  ATENCAO: apaga o banco SGC existente no servidor informado.
REM ==========================================================================
setlocal
set SERVIDOR=(localdb)\MSSQLLocalDB
set BANCO=SGC
if not "%~1"=="" set SERVIDOR=%~1

cd /d "%~dp0"

echo Recriando banco %BANCO% em %SERVIDOR%...
sqlcmd -S "%SERVIDOR%" -E -b -Q "IF DB_ID('%BANCO%') IS NOT NULL BEGIN ALTER DATABASE [%BANCO%] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [%BANCO%]; END; CREATE DATABASE [%BANCO%] COLLATE Latin1_General_CI_AI;"
if errorlevel 1 goto erro

call :executa 01_schema.sql || goto erro
for %%f in (procs\FN_*.sql) do call :executa %%f || goto erro
for %%f in (procs\SP_*.sql) do call :executa %%f || goto erro
for %%f in (procs\TR_*.sql) do call :executa %%f || goto erro
call :executa 02_dominios.sql || goto erro
call :executa 03_dados_iniciais.sql || goto erro
call :executa 04_carga_vendas.sql || goto erro
call :executa 05_carga_historico.sql || goto erro

echo.
echo Banco %BANCO% criado com sucesso.
exit /b 0

:executa
echo   %1
sqlcmd -S "%SERVIDOR%" -E -b -d %BANCO% -f 65001 -i "%1"
exit /b %errorlevel%

:erro
echo.
echo *** ERRO na criacao do banco ***
exit /b 1
