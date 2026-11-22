<%@ LANGUAGE="VBSCRIPT" CODEPAGE="1252" LCID="1046" %>
<%
'==============================================================================
' 2a via do boleto. Parcela vencida: valor atualizado ate hoje, vencimento hoje.
' Boleto e encargos pelo componente SGCBoleto.dll (VB6).
' 2007-08 R.A.
' 2014-05 J.P. banco 999
'==============================================================================
Option Explicit
Response.Buffer = True
Response.Charset = "ISO-8859-1"
%>
<!--#include file="include/conexao.asp"-->
<!--#include file="include/funcoes.asp"-->
<%
VerificaLogin

Dim rs, objBoleto, objEncargos, codigo, linha, vencimento, valor, valorOriginal, multa, juros
Dim agencia, conta, carteira, cedente, nosso

AbreConexao
Set rs = AbreRS("SELECT P.*, C.NR_COTA, C.NR_VERSAO, G.NR_GRUPO, X.NM_CONSORCIADO, X.NR_CPF " & _
                "  FROM TB_PARCELA P INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO " & _
                " INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
                " WHERE P.CD_PARCELA = " & Request.QueryString("parcela") & " AND P.ID_SITUACAO = 2")

If rs.EOF Then
    Response.Write "Parcela n&atilde;o dispon&iacute;vel para 2&ordf; via."
    Response.End
End If

agencia = Parametro("NR_AGENCIA")
conta = Parametro("NR_CONTA_CEDENTE")
carteira = Parametro("NR_CARTEIRA")
cedente = Parametro("NM_ADMINISTRADORA")
nosso = rs("NR_NOSSO_NUMERO")
valorOriginal = CCur(rs("VL_PARCELA"))
vencimento = CDate(rs("DT_VENCIMENTO"))
valor = valorOriginal
multa = 0
juros = 0

Set objBoleto = Server.CreateObject("SGCBoleto.clsBoleto")
Set objEncargos = Server.CreateObject("SGCBoleto.clsEncargos")

If vencimento < Date Then
    multa = objEncargos.Multa(valorOriginal, vencimento, Date)
    juros = objEncargos.Juros(valorOriginal, vencimento, Date)
    valor = objEncargos.ValorAtualizado(valorOriginal, vencimento, Date)
    vencimento = Date
End If

codigo = objBoleto.CodigoBarras(vencimento, valor, agencia, carteira, nosso, conta)
linha = objBoleto.LinhaDigitavel(codigo)
%>
<html>
<head>
<title>2a via - boleto</title>
<meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1">
<style>
  body { font-family: Arial; font-size: 10px; margin: 20px; }
  table.bol { border-collapse: collapse; width: 666px; }
  table.bol td { border: 1px solid #000; padding: 2px 4px; vertical-align: top; }
  .rot { font-size: 8px; display: block; } .val { font-size: 11px; font-weight: bold; }
  .linha { font-size: 13px; font-weight: bold; text-align: right; }
</style>
</head>
<body>
<p><a href="javascript:window.print()">Imprimir</a></p>
<table class="bol">
  <tr><td style="font-size:18px;font-weight:bold;width:150px">CeCons</td><td style="font-size:18px;font-weight:bold;width:60px">999-9</td><td class="linha" colspan="3" id="linha"><%= linha %></td></tr>
  <tr>
    <td colspan="3"><span class="rot">Cedente</span><span class="val"><%= cedente %></span></td>
    <td><span class="rot">Vencimento</span><span class="val" id="vencimento"><%= FormataData(vencimento) %></span></td>
    <td><span class="rot">Valor</span><span class="val" id="valor"><%= FormatNumber(valor, 2) %></span></td>
  </tr>
  <tr>
    <td colspan="3"><span class="rot">Sacado</span><span class="val"><%= rs("NM_CONSORCIADO") %> - CPF <%= FormataCPF(rs("NR_CPF")) %></span></td>
    <td><span class="rot">Nosso n&uacute;mero</span><span class="val"><%= carteira %>/<%= Left(nosso, 10) %>-<%= Right(nosso, 1) %></span></td>
    <td><span class="rot">Grupo/cota/parcela</span><span class="val"><%= FormataCota(rs("NR_GRUPO"), rs("NR_COTA"), rs("NR_VERSAO")) %> - <%= rs("NR_PARCELA") %></span></td>
  </tr>
  <tr>
    <td colspan="5"><span class="rot">Instru&ccedil;&otilde;es</span>
      <% If multa > 0 Or juros > 0 Then %>
        2&ordf; via com valor atualizado: parcela <%= FormatNumber(valorOriginal, 2) %> + multa <%= FormatNumber(multa, 2) %> + juros <%= FormatNumber(juros, 2) %>.
        Vencimento original <%= FormataData(rs("DT_VENCIMENTO")) %>.<br>
      <% End If %>
      N&atilde;o receber ap&oacute;s o vencimento.
    </td>
  </tr>
</table>
<div style="margin-top:8px; line-height:0"><%= objBoleto.BarrasHTML(codigo, "img/p.gif", "img/b.gif") %></div>
<%
Set objBoleto = Nothing
Set objEncargos = Nothing
rs.Close
FechaConexao
%>
</body>
</html>
