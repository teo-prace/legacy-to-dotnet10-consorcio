# SGC · Sistema de Gestão de Consórcios

Sistema da **CeCons Administradora de Consórcios**: cadastros, planos, grupos, venda de cotas, cobrança
(boleto, baixa, retorno bancário), assembleias (sorteio e lance), contemplação, exclusão, desistência,
devolução, reajuste e caixa segregado por grupo. Mais o portal do consorciado.

> **Sistema fictício.** Construído para demonstrar diagnóstico e modernização de legado Microsoft.
> As regras seguem a legislação e práticas públicas do mercado de consórcios
> ([Lei 11.795/2008](https://www.planalto.gov.br/ccivil_03/_ato2007-2010/2008/Lei/L11795.htm),
> Resolução BCB 285/2023). Empresa, pessoas e dados são inventados.

## Peças

| Peça | Tecnologia | Desde |
|---|---|---|
| Banco `SGC` | SQL Server: 25 tabelas, 26 procedures, funções e trigger | 2005 |
| SGC Desktop | VB6 + ADO: índices de reajuste, conferência da apuração | 2005 |
| Portal do consorciado | ASP clássico + ADO: extrato e 2ª via | 2007 |
| SGCBoleto.dll | VB6 ActiveX DLL: boleto e encargos do portal | 2007 |
| Retaguarda web | ASP.NET WebForms, VB.NET, *Web Site project*, .NET Framework 4.8 | 2011 |
| SGC.Comum / SGC.Negocio | Bibliotecas VB.NET | 2011 |
| SGC.Batch | Console VB.NET agendado | 2016 |

## Rodar localmente

Requisitos: Windows, Visual Studio com ASP.NET e .NET Framework 4.8, SQL Server LocalDB, IIS Express,
ODBC Driver 17 for SQL Server (portal) e twinBASIC Community (para compilar o componente VB6).

```bat
database\criar_banco.cmd                  :: recria o banco SGC com dados de exemplo (~15 s)
src\build.cmd                             :: compila bibliotecas, batch e valida as páginas
src\executar.cmd                          :: retaguarda em http://localhost:8090

powershell vb6\SGCBoleto\compilar.ps1     :: compila e registra o SGCBoleto (twinBASIC, 64 bits)
asp\executar_portal.cmd                   :: portal em http://localhost:8091
```

Retaguarda (senha `cecons123`): `admin` e `jpereira` (administradores), `msouza` (operador), `consulta`.
Portal: CPF + data de nascimento de qualquer consorciado (`TB_CONSORCIADO`).

Batch (`src\SGC.Batch\bin\Debug\SGC.Batch.exe`):

```bat
SGC.Batch EMISSAO 202612          :: emite a cobrança dos vencimentos de 12/2026
SGC.Batch RETORNO C:\pasta        :: processa arquivos .RET (exemplo em docs\exemplos\retorno)
SGC.Batch INADIMPLENCIA           :: exclui cotas com 3+ parcelas vencidas
SGC.Batch REAJUSTE 202701         :: reajuste anual dos bens pelo índice
```

Conferência de integridade (suporte): `sqlcmd -S (localdb)\MSSQLLocalDB -d SGC -i database\suporte\verifica_integridade.sql`

O desktop VB6 (`vb6/SGCDesktop`) tem formulários e MSFlexGrid; precisa da IDE do VB6 e não é compilado
pelos scripts.

## Dados de exemplo

| Grupo | Plano | Situação |
|---|---|---|
| 002071 | Veículo leve 60 meses (2024) | Em andamento desde 06/2024, 80 cotas: cobrança de 07/2024 a 11/2026 com atrasos, 29 assembleias apuradas (sorteio, lance, devolução a desistentes), 3 cotas excluídas, 2 contempladas inadimplentes, e a AGO de 25/11/2026 agendada com lances |
| 001041 | Imóvel 150 meses | Constituído, 160 cotas, 1ª assembleia em 18/12/2026 |
| 002087 | Veículo leve 60 meses | Em formação, 64 cotas (mínimo 72). A cota 064 tem versão 0 cancelada por arrependimento e versão 1 ativa |
| 003012 | Pesados 80 meses | Em formação, 5 cotas |

Data de referência dos dados: 20/11/2026.

## Estrutura

```
database/          schema, procedures (uma por arquivo), domínios, dados iniciais, cargas, suporte
src/SGC.Comum      acesso a dados, validação, formatação, log
src/SGC.Negocio    cálculo de parcelas, boleto (código de barras, linha digitável)
src/SGC.Web        retaguarda web
src/SGC.Batch      processos agendados
asp/portal         portal do consorciado
vb6/SGCBoleto      componente de boleto do portal
vb6/SGCDesktop     desktop VB6
docs/exemplos      arquivos de retorno bancário de exemplo
```

## Problemas conhecidos do ambiente

- **Smart App Control (Windows 11)** pode bloquear, de forma intermitente, o `App_Code.dll` que o compilador do
  ASP.NET gera na pasta temporária ("Uma política de Controle de Aplicativo bloqueou este arquivo"). Apague
  `%LOCALAPPDATA%\Temp\Temporary ASP.NET Files\sgc.web` e compile de novo.
- O log da web e do batch grava em `C:\SGC\Logs` (caminho fixo no código).
