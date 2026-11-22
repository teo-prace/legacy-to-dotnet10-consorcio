VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "clsSorteio"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
'==============================================================================
' Sorteio da assembleia (regra do SGCApuracao, 2005).
' Desde 2014 a apuracao oficial e a SP_APURA_ASSEMBLEIA; esta classe e usada
' pelo backoffice para CONFERIR o resultado (frmConferencia).
'
' Regra:
'   - numero = tres ultimos algarismos do 1o premio ("000" = 1000)
'   - acima do numero de cotas: subtrai o numero de cotas ate caber
'   - cota sorteada nao apta: proxima cota apta acima (circular)
'
' 2005-09 R.A.
'==============================================================================
Option Explicit

Private mQtCotas As Integer
Private mAptas() As Boolean

Public Sub Inicializa(ByVal QtCotas As Integer)
    mQtCotas = QtCotas
    ReDim mAptas(1 To QtCotas)
End Sub

Public Sub MarcaApta(ByVal NrCota As Integer)
    If NrCota >= 1 And NrCota <= mQtCotas Then mAptas(NrCota) = True
End Sub

Public Function NumeroBase(ByVal Premio As String) As Integer
    Dim n As Long
    n = CLng(Right$(Premio, 3))
    If n = 0 Then n = 1000
    Do While n > mQtCotas
        n = n - mQtCotas
    Loop
    NumeroBase = n
End Function

' Devolve o numero da cota contemplada (0 = nenhuma apta)
Public Function CotaSorteada(ByVal Premio1 As String) As Integer
    Dim n As Integer, i As Integer
    n = NumeroBase(Premio1)
    For i = 0 To mQtCotas - 1
        If mAptas(((n - 1 + i) Mod mQtCotas) + 1) Then
            CotaSorteada = ((n - 1 + i) Mod mQtCotas) + 1
            Exit Function
        End If
    Next i
    CotaSorteada = 0
End Function
