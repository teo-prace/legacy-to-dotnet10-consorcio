<%@ LANGUAGE="VBSCRIPT" CODEPAGE="1252" LCID="1046" %>
<%
'==============================================================================
' Portal do Consorciado - acesso por CPF + data de nascimento
' 2007-08 R.A.
'==============================================================================
Option Explicit
Response.Buffer = True
Response.Charset = "ISO-8859-1"
%>
<!--#include file="include/conexao.asp"-->
<!--#include file="include/funcoes.asp"-->
<%
Dim msg, cpf, nasc, rs, partes

If Request.Form("acao") = "entrar" Then
    cpf = SomenteNumeros(Request.Form("cpf"))
    nasc = Request.Form("nascimento")
    partes = Split(nasc, "/")

    If Len(cpf) <> 11 Or UBound(partes) <> 2 Then
        msg = "Informe CPF e data de nascimento (dd/mm/aaaa)."
    Else
        AbreConexao
        Set rs = AbreRS("SELECT CD_CONSORCIADO, NM_CONSORCIADO FROM TB_CONSORCIADO " & _
                        "WHERE NR_CPF = '" & cpf & "' AND CONVERT(CHAR(8), DT_NASCIMENTO, 112) = '" & _
                        partes(2) & partes(1) & partes(0) & "' AND FL_ATIVO = 'S'")
        If rs.EOF Then
            msg = "Dados n&atilde;o conferem."
        Else
            Session("CD_CONSORCIADO") = rs("CD_CONSORCIADO")
            Session("NM_CONSORCIADO") = rs("NM_CONSORCIADO")
            conn.Execute "EXEC SP_GRAVA_LOG NULL, 'LOGIN_PORTAL', '" & cpf & "'"
            rs.Close
            FechaConexao
            Response.Redirect "cotas.asp"
        End If
        rs.Close
        FechaConexao
    End If
End If

Cabecalho "Acesso ao portal"
%>
<p>Consulte suas cotas, o extrato de pagamentos e emita a 2&ordf; via do boleto.</p>
<% If msg <> "" Then %><p class="erro"><%= msg %></p><% End If %>
<form method="post" action="default.asp">
<input type="hidden" name="acao" value="entrar">
<table>
  <tr><td>CPF:</td><td><input type="text" name="cpf" size="16" maxlength="14" value="<%= Request.Form("cpf") %>"></td></tr>
  <tr><td>Data de nascimento:</td><td><input type="text" name="nascimento" size="12" maxlength="10"> (dd/mm/aaaa)</td></tr>
  <tr><td></td><td><input type="submit" value="Entrar"></td></tr>
</table>
</form>
<% Rodape %>
