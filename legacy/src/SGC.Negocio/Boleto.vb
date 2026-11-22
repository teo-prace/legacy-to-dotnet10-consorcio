Imports System.Text

'==============================================================================
' Boleto bancario - banco 999 (padrao FEBRABAN).
'
' Codigo de barras (44):
'   banco(3) moeda(1) DV(1) fator(4) valor(10) campo livre(25)
' Campo livre do banco 999:
'   agencia(4) carteira(2) nosso numero(11) conta(7) zero(1)
'
' Portado do componente SGCBoleto.dll (VB6, 2007) em 2011 - M.S.
' 2025-02 J.P. fator de vencimento reinicia em 1000 a partir de 22/02/2025
'==============================================================================
Public Class Boleto

    Public Const BANCO As String = "999"
    Public Const MOEDA As String = "9"
    Private Shared ReadOnly DATA_BASE As New Date(1997, 10, 7)

    Public Shared Function FatorVencimento(ByVal vencimento As Date) As String
        Dim dias As Integer = CInt((vencimento.Date - DATA_BASE).TotalDays)
        If dias >= 10000 Then dias = ((dias - 1000) Mod 9000) + 1000
        Return dias.ToString("0000")
    End Function

    Public Shared Function CampoLivre(ByVal agencia As String, ByVal carteira As String, _
                                      ByVal nossoNumero As String, ByVal conta As String) As String
        Return agencia.PadLeft(4, "0"c) & carteira.PadLeft(2, "0"c) & nossoNumero.PadLeft(11, "0"c) & _
               conta.PadLeft(7, "0"c) & "0"
    End Function

    Public Shared Function CodigoBarras(ByVal vencimento As Date, ByVal valor As Decimal, ByVal agencia As String, _
                                        ByVal carteira As String, ByVal nossoNumero As String, ByVal conta As String) As String
        Dim centavos As String = CLng(Math.Round(valor * 100, 0)).ToString("0000000000")
        Dim semDV As String = BANCO & MOEDA & FatorVencimento(vencimento) & centavos & _
                              CampoLivre(agencia, carteira, nossoNumero, conta)
        Return semDV.Substring(0, 4) & DVCodigoBarras(semDV) & semDV.Substring(4)
    End Function

    ' AAAAA.AAAAA BBBBB.BBBBBB CCCCC.CCCCCC D EEEEEEEEEEEEEE
    Public Shared Function LinhaDigitavel(ByVal codigoBarras As String) As String
        Dim c1 As String = codigoBarras.Substring(0, 4) & codigoBarras.Substring(19, 5)
        Dim c2 As String = codigoBarras.Substring(24, 10)
        Dim c3 As String = codigoBarras.Substring(34, 10)
        c1 = c1 & Modulo10(c1)
        c2 = c2 & Modulo10(c2)
        c3 = c3 & Modulo10(c3)
        Return c1.Substring(0, 5) & "." & c1.Substring(5) & " " & _
               c2.Substring(0, 5) & "." & c2.Substring(5) & " " & _
               c3.Substring(0, 5) & "." & c3.Substring(5) & " " & _
               codigoBarras.Substring(4, 1) & " " & codigoBarras.Substring(5, 14)
    End Function

    Public Shared Function Modulo10(ByVal numero As String) As String
        Dim soma As Integer = 0
        Dim peso As Integer = 2
        For i As Integer = numero.Length - 1 To 0 Step -1
            Dim produto As Integer = CInt(numero(i).ToString()) * peso
            If produto > 9 Then produto = (produto \ 10) + (produto Mod 10)
            soma += produto
            peso = IIf(peso = 2, 1, 2)
        Next
        Dim dv As Integer = (10 - (soma Mod 10)) Mod 10
        Return dv.ToString()
    End Function

    ' DV geral do codigo de barras: 0, 10 e 11 viram 1
    Public Shared Function DVCodigoBarras(ByVal numero As String) As String
        Dim soma As Integer = 0
        Dim peso As Integer = 2
        For i As Integer = numero.Length - 1 To 0 Step -1
            soma += CInt(numero(i).ToString()) * peso
            peso = IIf(peso = 9, 2, peso + 1)
        Next
        Dim dv As Integer = 11 - (soma Mod 11)
        If dv = 0 Or dv = 10 Or dv = 11 Then dv = 1
        Return dv.ToString()
    End Function

    ' Mesmo calculo da FN_DV_MODULO11 (10 e 11 viram 0)
    Public Shared Function DVNossoNumero(ByVal numero As String) As String
        Dim soma As Integer = 0
        Dim peso As Integer = 2
        For i As Integer = numero.Length - 1 To 0 Step -1
            soma += CInt(numero(i).ToString()) * peso
            peso = IIf(peso = 9, 2, peso + 1)
        Next
        Dim dv As Integer = 11 - (soma Mod 11)
        If dv >= 10 Then dv = 0
        Return dv.ToString()
    End Function

    ' Interleaved 2 of 5: devolve a sequencia de barras alternando preta/branca,
    ' "f" = fina, "g" = grossa. Comeca com preta.
    Public Shared Function BarrasI25(ByVal codigo As String) As String
        Dim padroes() As String = {"ffggf", "gfffg", "fgffg", "ggfff", "ffgfg", "gfgff", "fggff", "fffgg", "gffgf", "fgfgf"}
        Dim sb As New StringBuilder("ffff")
        For i As Integer = 0 To codigo.Length - 1 Step 2
            Dim pretas As String = padroes(CInt(codigo(i).ToString()))
            Dim brancas As String = padroes(CInt(codigo(i + 1).ToString()))
            For j As Integer = 0 To 4
                sb.Append(pretas(j))
                sb.Append(brancas(j))
            Next
        Next
        sb.Append("gff")
        Return sb.ToString()
    End Function

End Class
