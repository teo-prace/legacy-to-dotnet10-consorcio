<%@ LANGUAGE="VBSCRIPT" CODEPAGE="1252" LCID="1046" %>
<%
Option Explicit
Response.Buffer = True
Response.Charset = "ISO-8859-1"
%>
<!--#include file="include/conexao.asp"-->
<!--#include file="include/funcoes.asp"-->
<%
VerificaLogin

Dim rs
AbreConexao
Set rs = AbreRS("SELECT C.CD_COTA, C.NR_COTA, C.NR_VERSAO, C.ID_SITUACAO, C.PC_AMORTIZADO, G.NR_GRUPO, B.DS_BEM, B.VL_BEM, " & _
                "       (SELECT COUNT(*) FROM TB_PARCELA P WHERE P.CD_COTA = C.CD_COTA AND P.ID_SITUACAO = 2) AS QT_ABERTO " & _
                "  FROM TB_COTA C INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO INNER JOIN TB_BEM B ON B.CD_BEM = C.CD_BEM " & _
                " WHERE C.CD_CONSORCIADO = " & Session("CD_CONSORCIADO") & " AND C.ID_SITUACAO <> 6 ORDER BY G.NR_GRUPO, C.NR_COTA")

Cabecalho "Minhas cotas"
%>
<table class="tabela" width="100%">
  <tr><th>Grupo/cota</th><th>Cr&eacute;dito</th><th>Valor atual</th><th>% pago</th><th>Situa&ccedil;&atilde;o</th><th>Boletos em aberto</th><th></th></tr>
<% Do While Not rs.EOF %>
  <tr>
    <td><%= FormataCota(rs("NR_GRUPO"), rs("NR_COTA"), rs("NR_VERSAO")) %></td>
    <td><%= rs("DS_BEM") %></td>
    <td class="direita"><%= FormataMoeda(rs("VL_BEM")) %></td>
    <td class="direita"><%= FormatNumber(rs("PC_AMORTIZADO"), 2) %>%</td>
    <td><%= SituacaoCota(rs("ID_SITUACAO")) %></td>
    <td class="direita"><%= rs("QT_ABERTO") %></td>
    <td><a href="extrato.asp?cota=<%= rs("CD_COTA") %>">Extrato</a></td>
  </tr>
<%
    rs.MoveNext
Loop
rs.Close
FechaConexao
%>
</table>
<% Rodape %>
