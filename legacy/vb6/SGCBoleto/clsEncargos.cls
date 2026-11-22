VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "clsEncargos"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'==============================================================================
' SGCBoleto.clsEncargos - valor atualizado da 2a via (portal do consorciado).
' Multa 2% e juros 1% ao mes pro rata apos o vencimento.
' 2007-03 R.A.
'==============================================================================
Option Explicit

Public Function Multa(ByVal Valor As Currency, ByVal Vencimento As Date, ByVal Pagamento As Date) As Currency
    If Pagamento <= Vencimento Then
        Multa = 0
        Exit Function
    End If
    Multa = Round(Valor * 0.02, 2)
End Function

Public Function Juros(ByVal Valor As Currency, ByVal Vencimento As Date, ByVal Pagamento As Date) As Currency
    Dim dias As Long
    If Pagamento <= Vencimento Then
        Juros = 0
        Exit Function
    End If
    dias = DateDiff("d", Vencimento, Pagamento)
    Juros = Round(Valor * 0.01 / 30 * dias, 2)
End Function

Public Function ValorAtualizado(ByVal Valor As Currency, ByVal Vencimento As Date, ByVal Pagamento As Date) As Currency
    ValorAtualizado = Valor + Multa(Valor, Vencimento, Pagamento) + Juros(Valor, Vencimento, Pagamento)
End Function
