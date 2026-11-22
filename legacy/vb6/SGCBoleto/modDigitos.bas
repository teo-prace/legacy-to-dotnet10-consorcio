Attribute VB_Name = "modDigitos"
'==============================================================================
' SGCBoleto - digitos verificadores (padrao FEBRABAN)
' 2007-03 R.A.
'==============================================================================
Option Explicit

' DV modulo 10 dos campos da linha digitavel (pesos 2,1,2,1... da direita)
Public Function Modulo10(ByVal Numero As String) As String
    Dim i As Integer, soma As Integer, peso As Integer, produto As Integer
    peso = 2
    For i = Len(Numero) To 1 Step -1
        produto = CInt(Mid$(Numero, i, 1)) * peso
        If produto > 9 Then produto = produto - 9
        soma = soma + produto
        If peso = 2 Then peso = 1 Else peso = 2
    Next i
    Modulo10 = CStr((10 - (soma Mod 10)) Mod 10)
End Function

' DV geral do codigo de barras (modulo 11, pesos 2 a 9; 0, 10 e 11 viram 1)
Public Function DVGeral(ByVal Numero As String) As String
    Dim i As Integer, soma As Long, peso As Integer, dv As Integer
    peso = 2
    For i = Len(Numero) To 1 Step -1
        soma = soma + CInt(Mid$(Numero, i, 1)) * peso
        peso = peso + 1
        If peso > 9 Then peso = 2
    Next i
    dv = 11 - (soma Mod 11)
    If dv = 0 Or dv = 10 Or dv = 11 Then dv = 1
    DVGeral = CStr(dv)
End Function
