VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "clsBoleto"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'==============================================================================
' SGCBoleto.clsBoleto - codigo de barras e linha digitavel do banco 999.
' Usado pelo portal do consorciado (ASP): Server.CreateObject("SGCBoleto.clsBoleto")
'
' Campo livre: agencia(4) carteira(2) nosso numero(11) conta(7) "0"
'
' 2007-03 R.A.  versao inicial
' 2014-05 J.P.  banco 999 (troca do banco cobrador)
' 2025-02 J.P.  chamado 9120: fator de vencimento reiniciou em 1000 (22/02/2025)
'==============================================================================
Option Explicit

Private Const BANCO As String = "999"
Private Const MOEDA As String = "9"

Public Function FatorVencimento(ByVal Vencimento As Date) As String
    Dim dias As Long
    dias = DateDiff("d", DateSerial(1997, 10, 7), Vencimento)
    ' chamado 9120: a partir de 22/02/2025 o fator volta para 1000
    If Vencimento >= DateSerial(2025, 2, 22) Then dias = dias - 9000
    FatorVencimento = Format$(dias, "0000")
End Function

Public Function CampoLivre(ByVal Agencia As String, ByVal Carteira As String, _
                           ByVal NossoNumero As String, ByVal Conta As String) As String
    CampoLivre = Right$("0000" & Agencia, 4) & Right$("00" & Carteira, 2) & _
                 Right$("00000000000" & NossoNumero, 11) & Right$("0000000" & Conta, 7) & "0"
End Function

Public Function CodigoBarras(ByVal Vencimento As Date, ByVal Valor As Currency, ByVal Agencia As String, _
                             ByVal Carteira As String, ByVal NossoNumero As String, ByVal Conta As String) As String
    Dim semDV As String
    Dim centavos As String
    centavos = Right$("0000000000" & CStr(CLng(Valor * 100)), 10)
    semDV = BANCO & MOEDA & FatorVencimento(Vencimento) & centavos & CampoLivre(Agencia, Carteira, NossoNumero, Conta)
    CodigoBarras = Left$(semDV, 4) & DVGeral(semDV) & Mid$(semDV, 5)
End Function

' AAAAA.AAAAA BBBBB.BBBBBB CCCCC.CCCCCC D EEEEEEEEEEEEEE
Public Function LinhaDigitavel(ByVal Codigo As String) As String
    Dim c1 As String, c2 As String, c3 As String
    c1 = Left$(Codigo, 4) & Mid$(Codigo, 20, 5)
    c2 = Mid$(Codigo, 25, 10)
    c3 = Mid$(Codigo, 35, 10)
    c1 = c1 & Modulo10(c1)
    c2 = c2 & Modulo10(c2)
    c3 = c3 & Modulo10(c3)
    LinhaDigitavel = Left$(c1, 5) & "." & Mid$(c1, 6) & " " & _
                     Left$(c2, 5) & "." & Mid$(c2, 6) & " " & _
                     Left$(c3, 5) & "." & Mid$(c3, 6) & " " & _
                     Mid$(Codigo, 5, 1) & " " & Mid$(Codigo, 6, 14)
End Function

' Interleaved 2 of 5 em HTML (barras como imagens de 1 pixel esticadas)
Public Function BarrasHTML(ByVal Codigo As String, ByVal UrlPreto As String, ByVal UrlBranco As String) As String
    Dim padroes(9) As String
    Dim i As Integer, j As Integer
    Dim pretas As String, brancas As String, seq As String, html As String

    padroes(0) = "nnwwn": padroes(1) = "wnnnw": padroes(2) = "nwnnw": padroes(3) = "wwnnn": padroes(4) = "nnwnw"
    padroes(5) = "wnwnn": padroes(6) = "nwwnn": padroes(7) = "nnnww": padroes(8) = "wnnwn": padroes(9) = "nwnwn"

    seq = "nnnn"
    For i = 1 To Len(Codigo) Step 2
        pretas = padroes(CInt(Mid$(Codigo, i, 1)))
        brancas = padroes(CInt(Mid$(Codigo, i + 1, 1)))
        For j = 1 To 5
            seq = seq & Mid$(pretas, j, 1) & Mid$(brancas, j, 1)
        Next j
    Next i
    seq = seq & "wnn"

    For i = 1 To Len(seq)
        html = html & "<img src=""" & IIf(i Mod 2 = 1, UrlPreto, UrlBranco) & """ width=""" & _
               IIf(Mid$(seq, i, 1) = "n", "1", "3") & """ height=""50"" border=""0"">"
    Next i
    BarrasHTML = html
End Function
