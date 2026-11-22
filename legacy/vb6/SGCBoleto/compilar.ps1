<#
  Compila o SGCBoleto (ActiveX DLL VB6) com o twinBASIC Community, sem a IDE do VB6.

  1. monta a pasta do twinBASIC (Settings + Sources) a partir dos fontes VB6
  2. twinBASIC_win32.exe import -> SGCBoleto.twinproj
  3. twinBASIC.exe --buildAndExit64 -> build\Build\SGCBoleto_win64.dll
     (o twinBASIC registra o COM no HKCU do usuario)

  Uso: .\compilar.ps1 [-TwinBasic C:\caminho\twinBASIC] [-Arquitetura win64|win32]
  Em erro de compilacao a IDE fica aberta; o script encerra em 3 minutos e
  pede para abrir o .twinproj no twinBASIC para ver os erros.
#>
param(
    [string]$TwinBasic = $(if ($env:TWINBASIC) { $env:TWINBASIC } else { 'C:\Projects\ferramentas\twinBASIC' }),
    [ValidateSet('win64', 'win32')][string]$Arquitetura = 'win64'
)
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$build = Join-Path $aqui 'build'
$src = Join-Path $build 'src'
$fontes = Join-Path $src 'Sources'
$twinproj = Join-Path $build 'SGCBoleto.twinproj'

if (-not (Test-Path "$TwinBasic\twinBASIC.exe")) { throw "twinBASIC nao encontrado em $TwinBasic (use -TwinBasic ou a variavel TWINBASIC)" }

if (Test-Path $build) { Remove-Item $build -Recurse -Force }
New-Item -ItemType Directory -Force $fontes | Out-Null
foreach ($f in 'clsBoleto.cls', 'clsEncargos.cls', 'modDigitos.bas') {
    # fontes VB6 sao CRLF / ANSI
    $texto = [IO.File]::ReadAllText((Join-Path $aqui $f)) -replace "(?<!`r)`n", "`r`n"
    [IO.File]::WriteAllText((Join-Path $fontes $f), $texto, [Text.Encoding]::GetEncoding(1252))
}

$settings = [ordered]@{
    'configuration.inherits'         = 'Defaults'
    'project.appTitle'               = 'SGCBoleto'
    'project.buildPath'              = '${SourcePath}\Build\${ProjectName}_${Architecture}.${FileExtension}'
    'project.buildType'              = 'ActiveX DLL'
    'project.description'            = 'SGC - boleto e encargos (portal do consorciado)'
    'project.dllRegisterLocalMachine' = $false
    'project.exportPathIsV2'         = $true
    'project.id'                     = '{5B0C3E2A-7D41-4F6B-9C8E-2A1D0F7B3C55}'
    'project.name'                   = 'SGCBoleto'
    'project.optionExplicit'         = $false
    'project.references'             = @(
        [ordered]@{ id = '{00020430-0000-0000-C000-000000000046}'; lcid = 0; name = 'OLE Automation'; path32 = 'C:\Windows\SysWOW64\stdole2.tlb'; path64 = 'C:\Windows\System32\stdole2.tlb'; versionMajor = 2; versionMinor = 0 },
        [ordered]@{ hasBeenSplit = $true; id = '{F50B82D0-DCAB-43FE-9631-11959D4A4728}'; isCompilerPackage = $true; licence = 'MIT'; name = '[COMPILER PACKAGE] WinNativeForms Package'; path32 = ''; path64 = ''; publisher = 'TWINBASIC-COMPILER'; symbolId = 'WinFormsPackage'; versionBuild = 0; versionMajor = 0; versionMinor = 0; versionRevision = 31 }
    )
    'project.settingsVersion'        = 1
    'project.warnings'               = [ordered]@{ errors = @(); ignored = @() }
    'runtime.useUnicodeStandardLibrary' = $false
}
[IO.File]::WriteAllText((Join-Path $src 'Settings'), ($settings | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))

Write-Host "Importando fontes..."
$import = & "$TwinBasic\bin\twinBASIC_win32.exe" import $twinproj $src 2>&1 | Out-String
if (-not (Test-Path $twinproj) -or $import -notmatch 'DONE') { throw "import falhou:`n$import" }

$arq = if ($Arquitetura -eq 'win64') { '64' } else { '32' }
Write-Host "Compilando ($Arquitetura)..."
$p = Start-Process -FilePath "$TwinBasic\twinBASIC.exe" -ArgumentList "`"$twinproj`" --buildAndExit$arq --no-update-recent-files --no-save-ide-settings" -WorkingDirectory $build -PassThru
if (-not $p.WaitForExit(180000)) {
    $p.Kill()
    throw "twinBASIC nao terminou em 3 minutos: provavel erro de compilacao. Abra $twinproj no twinBASIC para ver."
}

$dll = Get-ChildItem (Join-Path $build 'Build') -Filter "SGCBoleto_$Arquitetura.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $dll) { throw "DLL nao gerada (codigo $($p.ExitCode))" }
Write-Host "OK: $($dll.FullName)"
