<#
  POC-02a · monta uma CÓPIA do legado com os System.Web Adapters oficiais (lado .NET Framework).
  Não altera src\SGC.Web. Saída: %TEMP%\sgc-web-adaptadores (rodar com IIS Express na porta 8094).

  Tudo o que o legado precisa receber para os adaptadores funcionarem está aqui, passo a passo:
    1. 49 DLLs no Bin (pacote Microsoft.AspNetCore.SystemWebAdapters.FrameworkServices 2.3.0 e dependências)
    2. web.config: chave RemoteAppApiKey, módulo SystemWebAdapterModule, referência a netstandard
       (Web Site project não referencia sozinho) e 8 redirecionamentos de assembly
    3. Global.asax: registro do host (VB, chamadas estáticas: os métodos de extensão não resolvem no Web Site)
    4. Login: gravar chaves serializáveis (CD_USUARIO, NM_USUARIO, ID_PERFIL); o DataRow "Usuario" não serializa
#>
$ErrorActionPreference = 'Stop'
$raiz = Resolve-Path "$PSScriptRoot\..\.."
$destino = Join-Path $env:TEMP 'sgc-web-adaptadores'
$pacote = Join-Path $env:TEMP 'sgc-adaptadores-fx'

# 1) DLLs do lado Framework (restaura por um projeto net48 descartável)
if (-not (Test-Path "$pacote\bin\Release\net48\Microsoft.AspNetCore.SystemWebAdapters.FrameworkServices.dll")) {
    New-Item -ItemType Directory -Force $pacote | Out-Null
    Set-Content "$pacote\Fx.csproj" @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Exe</OutputType>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
    <GenerateBindingRedirectsOutputType>true</GenerateBindingRedirectsOutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SystemWebAdapters.FrameworkServices" Version="2.3.0" />
    <Reference Include="System.Web" />
  </ItemGroup>
</Project>
'@
    Set-Content "$pacote\P.cs" 'class P { static void Main() {} }'
    Push-Location $pacote; dotnet build -c Release | Out-Null; Pop-Location
}
$fxBin = "$pacote\bin\Release\net48"

if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }
Copy-Item (Join-Path $raiz 'legacy\src\SGC.Web') $destino -Recurse
Copy-Item "$fxBin\*.dll" "$destino\Bin\" -Force

# 2) web.config
[xml]$wc = Get-Content "$destino\web.config"
[xml]$fx = Get-Content "$fxBin\Fx.exe.config"
$k = $wc.CreateElement('add'); $k.SetAttribute('key', 'RemoteAppApiKey'); $k.SetAttribute('value', '0d4b3c1e-5a2f-4e7b-9c8d-1f2e3a4b5c6d')
$wc.configuration.appSettings.AppendChild($k) | Out-Null
$ns = $wc.CreateElement('add'); $ns.SetAttribute('assembly', 'netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51')
$wc.configuration.'system.web'.compilation.assemblies.AppendChild($ns) | Out-Null
$mods = $wc.CreateElement('modules')
$rm = $wc.CreateElement('remove'); $rm.SetAttribute('name', 'SystemWebAdapterModule'); $mods.AppendChild($rm) | Out-Null
$ad = $wc.CreateElement('add'); $ad.SetAttribute('name', 'SystemWebAdapterModule')
$ad.SetAttribute('type', 'Microsoft.AspNetCore.SystemWebAdapters.SystemWebAdapterModule, Microsoft.AspNetCore.SystemWebAdapters.FrameworkServices')
$ad.SetAttribute('preCondition', 'managedHandler'); $mods.AppendChild($ad) | Out-Null
$wc.configuration.'system.webServer'.AppendChild($mods) | Out-Null
$wc.configuration.AppendChild($wc.ImportNode($fx.configuration.runtime, $true)) | Out-Null
$wc.Save("$destino\web.config")

# 3) Global.asax
Set-Content "$destino\Global.asax" -Encoding utf8 @'
<%@ Application Language="VB" %>
<%@ Import Namespace="SGC.Comum" %>
<%@ Import Namespace="Microsoft.AspNetCore.SystemWebAdapters" %>
<%@ Import Namespace="Microsoft.AspNetCore.SystemWebAdapters.Hosting" %>
<%@ Import Namespace="Microsoft.AspNetCore.SystemWebAdapters.SessionState.Serialization" %>

<script runat="server">

    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        HttpApplicationHost.RegisterHost(Sub(b)
            Dim a As ISystemWebAdapterBuilder = System.Web.SystemWebAdapterConfiguration.AddSystemWebAdapters(b.Services)
            System.Web.JsonSessionSerializerExtensions.AddJsonSessionSerializer(a, Sub(o As JsonSessionSerializerOptions)
                o.RegisterKey(Of Integer)("CD_USUARIO")
                o.RegisterKey(Of String)("NM_USUARIO")
                o.RegisterKey(Of Integer)("ID_PERFIL")
            End Sub)
            Dim r = System.Web.RemoteAppServerExtensions.AddRemoteAppServer(a, Sub(o) o.ApiKey = ConfigurationManager.AppSettings("RemoteAppApiKey"))
            System.Web.RemoteAppSessionStateExtensions.AddSessionServer(r)
        End Sub)
        Log.Grava("Aplicacao iniciada")
    End Sub

    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
        Dim ex As Exception = Server.GetLastError()
        If ex IsNot Nothing Then
            Log.Grava(Request.RawUrl & " - " & ex.ToString())
        End If
    End Sub

    Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
    End Sub

</script>
'@

# 4) Login: chaves serializáveis
$login = Get-Content "$destino\Login.aspx.vb" -Raw
$login = $login.Replace("        Session(""Usuario"") = dt.Rows(0)`r`n", @"
        Session("Usuario") = dt.Rows(0)
        ' migracao: chaves serializaveis para o SGC.Novo (System.Web Adapters)
        Session("CD_USUARIO") = CInt(dt.Rows(0)("CD_USUARIO"))
        Session("NM_USUARIO") = dt.Rows(0)("NM_USUARIO").ToString()
        Session("ID_PERFIL") = CInt(dt.Rows(0)("ID_PERFIL"))

"@)
Set-Content "$destino\Login.aspx.vb" $login -Encoding utf8
if ($login -notmatch 'NM_USUARIO"\) =') { throw 'não consegui alterar o Login.aspx.vb' }

"Cópia pronta em $destino ($((Get-ChildItem "$destino\Bin" -Filter *.dll).Count) DLLs no Bin)"
