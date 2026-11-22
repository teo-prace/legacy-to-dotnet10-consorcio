<%@ LANGUAGE="VBSCRIPT" CODEPAGE="1252" LCID="1046" %>
<%
'==============================================================================
' Extrato da cota
' 2007-08 R.A.
'==============================================================================
Option Explicit
Response.Buffer = True
Response.Charset = "ISO-8859-1"
%>
<!--#include file="include/conexao.asp"-->
<!--#include file="include/funcoes.asp"-->
<%
VerificaLogin

Dim rs, rsCota, cota
cota = Request.QueryString("cota")

AbreConexao
Set rsCota = AbreRS("SELECT C.NR_COTA, C.NR_VERSAO, G.NR_GRUPO, C.ID_SITUACAO FROM TB_COTA C INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO WHERE C.CD_COTA = " & cota)
If rsCota.EOF Then
    rsCota.Close
    FechaConexao
    Response.Redirect "cotas.asp"
End If

Set rs = AbreRS("SELECT CD_PARCELA, NR_PARCELA, DT_VENCIMENTO, VL_PARCELA, DT_PAGAMENTO, VL_PAGO, ID_SITUACAO " & _
                "  FROM TB_PARCELA WHERE CD_COTA = " & cota & " AND ID_SITUACAO IN (2, 3) ORDER BY NR_PARCELA")

Cabecalho "Extrato da cota " & FormataCota(rsCota("NR_GRUPO"), rsCota("NR_COTA"), rsCota("NR_VERSAO"))
%>
<p>Situa&ccedil;&atilde;o da cota: <b><%= SituacaoCota(rsCota("ID_SITUACAO")) %></b></p>
<table class="tabela" width="100%">
  <tr><th>Parcela</th><th>Vencimento</th><th>Valor</th><th>Pago em</th><th>Valor pago</th><th>Situa&ccedil;&atilde;o</th><th></th></tr>
<% Do While Not rs.EOF %>
  <tr>
    <td class="direita"><%= rs("NR_PARCELA") %></td>
    <td><%= FormataData(rs("DT_VENCIMENTO")) %></td>
    <td class="direita"><%= FormataMoeda(rs("VL_PARCELA")) %></td>
    <td><%= FormataData(rs("DT_PAGAMENTO")) %></td>
    <td class="direita"><%= FormataMoeda(rs("VL_PAGO")) %></td>
    <td><%= SituacaoParcela(rs("ID_SITUACAO")) %><% If rs("ID_SITUACAO") = 2 And rs("DT_VENCIMENTO") < Date Then %> <span class="erro">(vencida)</span><% End If %></td>
    <td><% If rs("ID_SITUACAO") = 2 Then %><a href="segundavia.asp?parcela=<%= rs("CD_PARCELA") %>" target="_blank">2&ordf; via</a><% End If %></td>
  </tr>
<%
    rs.MoveNext
Loop
rs.Close
rsCota.Close
FechaConexao
%>
</table>
<p><a href="cotas.asp">Voltar</a></p>
<% Rodape %>
