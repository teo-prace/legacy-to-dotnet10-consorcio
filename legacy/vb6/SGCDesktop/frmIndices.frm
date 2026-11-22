VERSION 5.00
Object = "{5E9E78A0-531B-11CF-91F6-C2863C385E30}#1.0#0"; "MSFLXGRD.OCX"
Begin VB.Form frmIndices
   Caption         =   "Indices de reajuste"
   ClientHeight    =   5400
   ClientLeft      =   60
   ClientTop       =   345
   ClientWidth     =   6600
   LinkTopic       =   "Form1"
   MDIChild        =   -1  'True
   ScaleHeight     =   5400
   ScaleWidth      =   6600
   Begin MSFlexGridLib.MSFlexGrid grdValores
      Height          =   3735
      Left            =   120
      TabIndex        =   9
      Top             =   1500
      Width           =   6360
      _ExtentX        =   11218
      _ExtentY        =   6588
      _Version        =   393216
      Cols            =   3
      FixedCols       =   0
      SelectionMode   =   1
      AllowUserResizing=   1
   End
   Begin VB.CommandButton cmdExcluir
      Caption         =   "Excluir"
      Height          =   375
      Left            =   5280
      TabIndex        =   8
      Top             =   960
      Width           =   1095
   End
   Begin VB.CommandButton cmdIncluir
      Caption         =   "Incluir"
      Height          =   375
      Left            =   4080
      TabIndex        =   7
      Top             =   960
      Width           =   1095
   End
   Begin VB.TextBox txtVariacao
      Height          =   315
      Left            =   2880
      TabIndex        =   6
      Top             =   990
      Width           =   975
   End
   Begin VB.TextBox txtMes
      Height          =   315
      Left            =   1920
      MaxLength       =   2
      TabIndex        =   4
      Top             =   990
      Width           =   495
   End
   Begin VB.TextBox txtAno
      Height          =   315
      Left            =   960
      MaxLength       =   4
      TabIndex        =   2
      Top             =   990
      Width           =   735
   End
   Begin VB.ComboBox cboIndice
      Height          =   315
      Left            =   960
      Style           =   2  'Dropdown List
      TabIndex        =   0
      Top             =   360
      Width           =   3015
   End
   Begin VB.Label Label4
      Caption         =   "Var. %:"
      Height          =   255
      Left            =   2520
      TabIndex        =   5
      Top             =   1020
      Width           =   375
   End
   Begin VB.Label Label3
      Caption         =   "Mes:"
      Height          =   255
      Left            =   1740
      TabIndex        =   3
      Top             =   1020
      Width           =   375
   End
   Begin VB.Label Label2
      Caption         =   "Ano:"
      Height          =   255
      Left            =   240
      TabIndex        =   1
      Top             =   1020
      Width           =   615
   End
   Begin VB.Label Label1
      Caption         =   "Indice:"
      Height          =   255
      Left            =   240
      TabIndex        =   10
      Top             =   420
      Width           =   615
   End
End
Attribute VB_Name = "frmIndices"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
'==============================================================================
' Cadastro de indices de reajuste (INCC, IPCA).
' Usado no reajuste anual dos bens.
' 2005-06 R.A.
' 2011-04 M.S. "migrar para a web na fase 2"
'==============================================================================
Option Explicit

Private Sub Form_Load()
    Dim rs As ADODB.Recordset
    Set rs = AbreRecordset("SELECT CD_INDICE, SG_INDICE FROM TB_INDICE ORDER BY CD_INDICE")
    Do While Not rs.EOF
        cboIndice.AddItem rs!SG_INDICE
        cboIndice.ItemData(cboIndice.NewIndex) = rs!CD_INDICE
        rs.MoveNext
    Loop
    rs.Close
    If cboIndice.ListCount > 0 Then cboIndice.ListIndex = 0

    txtAno.Text = Year(Date)
    txtMes.Text = Month(DateAdd("m", -1, Date))
End Sub

Private Sub cboIndice_Click()
    CarregaGrid
End Sub

Private Sub CarregaGrid()
    Dim rs As ADODB.Recordset
    Dim linha As Long

    grdValores.Rows = 1
    grdValores.TextMatrix(0, 0) = "Ano/Mes"
    grdValores.TextMatrix(0, 1) = "Variacao %"
    grdValores.TextMatrix(0, 2) = "Cadastro"
    grdValores.ColWidth(0) = 1200
    grdValores.ColWidth(1) = 1500
    grdValores.ColWidth(2) = 1800

    If cboIndice.ListIndex < 0 Then Exit Sub

    Set rs = AbreRecordset("SELECT NR_ANO_MES, PC_VARIACAO, DT_CADASTRO FROM TB_INDICE_VALOR " & _
                           "WHERE CD_INDICE = " & cboIndice.ItemData(cboIndice.ListIndex) & " ORDER BY NR_ANO_MES DESC")
    Do While Not rs.EOF
        linha = grdValores.Rows
        grdValores.Rows = linha + 1
        grdValores.TextMatrix(linha, 0) = Right$(rs!NR_ANO_MES, 2) & "/" & Left$(rs!NR_ANO_MES, 4)
        grdValores.TextMatrix(linha, 1) = Format$(rs!PC_VARIACAO, "0.0000")
        grdValores.TextMatrix(linha, 2) = Format$(rs!DT_CADASTRO, "dd/mm/yyyy")
        rs.MoveNext
    Loop
    rs.Close
End Sub

Private Sub cmdIncluir_Click()
    Dim anoMes As String

    If Val(txtAno.Text) < 2000 Or Val(txtMes.Text) < 1 Or Val(txtMes.Text) > 12 Then
        MsgBox "Ano/mes invalido.", vbExclamation, "SGC"
        Exit Sub
    End If
    If Not IsNumeric(txtVariacao.Text) Then
        MsgBox "Informe a variacao.", vbExclamation, "SGC"
        Exit Sub
    End If

    anoMes = txtAno.Text & Format$(Val(txtMes.Text), "00")

    On Error GoTo Erro
    Executa "INSERT INTO TB_INDICE_VALOR (CD_INDICE, NR_ANO_MES, PC_VARIACAO) VALUES (" & _
            cboIndice.ItemData(cboIndice.ListIndex) & ", " & anoMes & ", " & SqlDecimal(CDbl(txtVariacao.Text)) & ")"
    GravaLog "INDICE_INCLUIDO", cboIndice.Text & " " & anoMes & " " & txtVariacao.Text
    txtVariacao.Text = ""
    CarregaGrid
    Exit Sub

Erro:
    MsgBox "Erro ao incluir. Verifique se o mes ja foi cadastrado." & vbCrLf & Err.Description, vbCritical, "SGC"
End Sub

Private Sub cmdExcluir_Click()
    Dim anoMes As String
    If grdValores.Row < 1 Then Exit Sub
    anoMes = Right$(grdValores.TextMatrix(grdValores.Row, 0), 4) & Left$(grdValores.TextMatrix(grdValores.Row, 0), 2)
    If MsgBox("Excluir o indice de " & grdValores.TextMatrix(grdValores.Row, 0) & "?", vbQuestion + vbYesNo, "SGC") = vbNo Then Exit Sub
    Executa "DELETE FROM TB_INDICE_VALOR WHERE CD_INDICE = " & cboIndice.ItemData(cboIndice.ListIndex) & " AND NR_ANO_MES = " & anoMes
    GravaLog "INDICE_EXCLUIDO", cboIndice.Text & " " & anoMes
    CarregaGrid
End Sub
