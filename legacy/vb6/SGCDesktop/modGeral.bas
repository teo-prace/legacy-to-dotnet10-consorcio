Attribute VB_Name = "modGeral"
'==============================================================================
' SGC Desktop - rotinas gerais
' 2005-03 R.A.
'==============================================================================
Option Explicit

Private Declare Function GetPrivateProfileString Lib "kernel32" Alias "GetPrivateProfileStringA" _
    (ByVal lpApplicationName As String, ByVal lpKeyName As Any, ByVal lpDefault As String, _
     ByVal lpReturnedString As String, ByVal nSize As Long, ByVal lpFileName As String) As Long

Public gCodigoUsuario As Long
Public gNomeUsuario As String
Public gPerfil As Integer

Sub Main()
    If Not Conecta() Then End
    frmLogin.Show vbModal
    If gCodigoUsuario = 0 Then End
    frmPrincipal.Show
End Sub

Public Function LeIni(ByVal secao As String, ByVal chave As String, ByVal padrao As String) As String
    Dim buffer As String * 255
    Dim tam As Long
    tam = GetPrivateProfileString(secao, chave, padrao, buffer, 255, App.Path & "\SGC.INI")
    LeIni = Left$(buffer, tam)
End Function

' valores decimais no SQL sempre com ponto (estacao em pt-BR usa virgula)
Public Function SqlDecimal(ByVal valor As Double) As String
    SqlDecimal = Replace(CStr(valor), ",", ".")
End Function

Public Function SqlTexto(ByVal texto As String) As String
    SqlTexto = "'" & texto & "'"
End Function

Public Sub GravaLog(ByVal acao As String, ByVal detalhe As String)
    On Error Resume Next
    Executa "EXEC SP_GRAVA_LOG " & gCodigoUsuario & ", '" & acao & "', '" & detalhe & "'"
End Sub
