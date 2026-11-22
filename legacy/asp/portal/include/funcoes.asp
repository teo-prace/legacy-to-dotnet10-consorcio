<%
'==============================================================================
' Portal do Consorciado - funcoes gerais
' 2007-08 R.A.
'==============================================================================

Function SomenteNumeros(texto)
    Dim i, c, r
    r = ""
    For i = 1 To Len(texto)
        c = Mid(texto, i, 1)
        If c >= "0" And c <= "9" Then r = r & c
    Next
    SomenteNumeros = r
End Function

Function FormataMoeda(valor)
    If IsNull(valor) Then
        FormataMoeda = ""
    Else
        FormataMoeda = "R$ " & FormatNumber(valor, 2)
    End If
End Function

Function FormataData(valor)
    If IsNull(valor) Or valor = "" Then
        FormataData = ""
    Else
        FormataData = Right("0" & Day(valor), 2) & "/" & Right("0" & Month(valor), 2) & "/" & Year(valor)
    End If
End Function

Function FormataCPF(cpf)
    FormataCPF = Left(cpf, 3) & "." & Mid(cpf, 4, 3) & "." & Mid(cpf, 7, 3) & "-" & Right(cpf, 2)
End Function

Function FormataCota(grupo, numero, versao)
    FormataCota = grupo & "/" & Right("000" & numero, 3) & "-" & versao
End Function

Function SituacaoCota(id)
    Select Case CInt(id)
        Case 1 SituacaoCota = "Ativa"
        Case 2 SituacaoCota = "Contemplada"
        Case 3 SituacaoCota = "Exclu&iacute;da"
        Case 4 SituacaoCota = "Desistente"
        Case 5 SituacaoCota = "Quitada"
        Case 6 SituacaoCota = "Cancelada"
    End Select
End Function

Function SituacaoParcela(id)
    Select Case CInt(id)
        Case 1 SituacaoParcela = "A emitir"
        Case 2 SituacaoParcela = "Em aberto"
        Case 3 SituacaoParcela = "Paga"
        Case 4 SituacaoParcela = "Cancelada"
    End Select
End Function

Function Parametro(nome)
    Dim rs
    Set rs = AbreRS("SELECT DS_VALOR FROM TB_PARAMETRO WHERE NM_PARAMETRO = '" & nome & "'")
    If Not rs.EOF Then Parametro = rs("DS_VALOR")
    rs.Close
End Function

Sub VerificaLogin()
    If Session("CD_CONSORCIADO") = "" Then
        Response.Redirect "default.asp"
    End If
End Sub

Sub Cabecalho(titulo)
%>
<html>
<head>
<title>CeCons Cons&oacute;rcios - Portal do Consorciado</title>
<meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1">
<link rel="stylesheet" href="estilo.css" type="text/css">
</head>
<body>
<table width="760" border="0" cellpadding="0" cellspacing="0" align="center">
  <tr>
    <td class="topo">CeCons Cons&oacute;rcios<br><span>Portal do Consorciado</span></td>
    <td class="topo" align="right">
      <% If Session("CD_CONSORCIADO") <> "" Then %>
        Ol&aacute;, <%= Session("NM_CONSORCIADO") %> | <a href="cotas.asp">Minhas cotas</a> | <a href="sair.asp">Sair</a>
      <% End If %>
    </td>
  </tr>
  <tr>
    <td colspan="2" class="conteudo">
      <h1><%= titulo %></h1>
<%
End Sub

Sub Rodape()
%>
    </td>
  </tr>
  <tr><td colspan="2" class="rodape">CeCons Administradora de Cons&oacute;rcios Ltda - Central de atendimento 0800 000 0000</td></tr>
</table>
</body>
</html>
<%
End Sub
%>
