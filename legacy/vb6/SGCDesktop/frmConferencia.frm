VERSION 5.00
Object = "{5E9E78A0-531B-11CF-91F6-C2863C385E30}#1.0#0"; "MSFLXGRD.OCX"
Begin VB.Form frmConferencia
   Caption         =   "Conferencia de apuracao"
   ClientHeight    =   5700
   ClientLeft      =   60
   ClientTop       =   345
   ClientWidth     =   7800
   LinkTopic       =   "Form1"
   MDIChild        =   -1  'True
   ScaleHeight     =   5700
   ScaleWidth      =   7800
   Begin MSFlexGridLib.MSFlexGrid grdAgos
      Height          =   4335
      Left            =   120
      TabIndex        =   4
      Top             =   1200
      Width           =   7560
      _ExtentX        =   13335
      _ExtentY        =   7646
      _Version        =   393216
      Cols            =   6
      FixedCols       =   0
      SelectionMode   =   1
      AllowUserResizing=   1
   End
   Begin VB.CommandButton cmdConferir
      Caption         =   "Conferir"
      Height          =   375
      Left            =   4200
      TabIndex        =   2
      Top             =   300
      Width           =   1215
   End
   Begin VB.ComboBox cboGrupo
      Height          =   315
      Left            =   960
      Style           =   2  'Dropdown List
      TabIndex        =   1
      Top             =   330
      Width           =   3015
   End
   Begin VB.Label lblResumo
      Height          =   255
      Left            =   120
      TabIndex        =   3
      Top             =   840
      Width           =   7455
   End
   Begin VB.Label Label1
      Caption         =   "Grupo:"
      Height          =   255
      Left            =   240
      TabIndex        =   0
      Top             =   390
      Width           =   615
   End
End
Attribute VB_Name = "frmConferencia"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
'==============================================================================
' Conferencia da apuracao: refaz o sorteio de cada AGO apurada com a regra
' do desktop (clsSorteio) e compara com a cota contemplada por sorteio
' gravada pela procedure. Divergencia = "verificar com o suporte".
'
' Aptas aqui = cotas ativas na data (situacao), sem olhar inadimplencia:
' o desktop nunca teve essa informacao confiavel.
'
' 2014-09 J.P.
'==============================================================================
Option Explicit

Private Sub Form_Load()
    Dim rs As ADODB.Recordset
    Set rs = AbreRecordset("SELECT CD_GRUPO, NR_GRUPO, QT_COTAS_MAXIMO FROM TB_GRUPO WHERE ID_SITUACAO = 2 ORDER BY NR_GRUPO")
    Do While Not rs.EOF
        cboGrupo.AddItem rs!NR_GRUPO & " (" & rs!QT_COTAS_MAXIMO & " cotas)"
        cboGrupo.ItemData(cboGrupo.NewIndex) = rs!CD_GRUPO
        rs.MoveNext
    Loop
    rs.Close
    If cboGrupo.ListCount > 0 Then cboGrupo.ListIndex = 0
End Sub

Private Sub cmdConferir_Click()
    Dim rsAgo As ADODB.Recordset, rsCotas As ADODB.Recordset, rsSort As ADODB.Recordset
    Dim sorteio As clsSorteio
    Dim qtCotas As Integer, linha As Long, cotaVb6 As Integer, cotaSgc As Integer
    Dim divergentes As Integer, total As Integer

    Screen.MousePointer = vbHourglass

    qtCotas = CInt(Mid$(cboGrupo.Text, InStr(cboGrupo.Text, "(") + 1, InStr(cboGrupo.Text, " cotas") - InStr(cboGrupo.Text, "(") - 1))

    grdAgos.Rows = 1
    grdAgos.TextMatrix(0, 0) = "AGO"
    grdAgos.TextMatrix(0, 1) = "Data"
    grdAgos.TextMatrix(0, 2) = "1o premio"
    grdAgos.TextMatrix(0, 3) = "Desktop"
    grdAgos.TextMatrix(0, 4) = "SGC"
    grdAgos.TextMatrix(0, 5) = "Conferencia"

    Set rsAgo = AbreRecordset("SELECT CD_ASSEMBLEIA, NR_ASSEMBLEIA, DT_ASSEMBLEIA, NR_PREMIO_1 FROM TB_ASSEMBLEIA " & _
                              "WHERE CD_GRUPO = " & cboGrupo.ItemData(cboGrupo.ListIndex) & " AND ID_SITUACAO = 2 ORDER BY NR_ASSEMBLEIA")
    Do While Not rsAgo.EOF
        Set sorteio = New clsSorteio
        sorteio.Inicializa qtCotas

        ' cotas ativas na data da AGO (nao contempladas antes dela)
        Set rsCotas = AbreRecordset("SELECT C.NR_COTA FROM TB_COTA C WHERE C.CD_GRUPO = " & cboGrupo.ItemData(cboGrupo.ListIndex) & _
                                    " AND C.DT_ADESAO <= '" & Format$(rsAgo!DT_ASSEMBLEIA, "yyyymmdd") & "'" & _
                                    " AND C.ID_SITUACAO IN (1, 2, 5) AND NOT EXISTS (SELECT 1 FROM TB_CONTEMPLACAO T " & _
                                    "   WHERE T.CD_COTA = C.CD_COTA AND T.DT_CONTEMPLACAO < '" & Format$(rsAgo!DT_ASSEMBLEIA, "yyyymmdd") & "')")
        Do While Not rsCotas.EOF
            sorteio.MarcaApta rsCotas!NR_COTA
            rsCotas.MoveNext
        Loop
        rsCotas.Close

        cotaVb6 = sorteio.CotaSorteada(rsAgo!NR_PREMIO_1)

        Set rsSort = AbreRecordset("SELECT C.NR_COTA FROM TB_CONTEMPLACAO T INNER JOIN TB_COTA C ON C.CD_COTA = T.CD_COTA " & _
                                   "WHERE T.CD_ASSEMBLEIA = " & rsAgo!CD_ASSEMBLEIA & " AND T.ID_TIPO = 1")
        If rsSort.EOF Then cotaSgc = 0 Else cotaSgc = rsSort!NR_COTA
        rsSort.Close

        linha = grdAgos.Rows
        grdAgos.Rows = linha + 1
        grdAgos.TextMatrix(linha, 0) = rsAgo!NR_ASSEMBLEIA
        grdAgos.TextMatrix(linha, 1) = Format$(rsAgo!DT_ASSEMBLEIA, "dd/mm/yyyy")
        grdAgos.TextMatrix(linha, 2) = rsAgo!NR_PREMIO_1
        grdAgos.TextMatrix(linha, 3) = Format$(cotaVb6, "000")
        grdAgos.TextMatrix(linha, 4) = IIf(cotaSgc = 0, "-", Format$(cotaSgc, "000"))
        If cotaVb6 = cotaSgc Then
            grdAgos.TextMatrix(linha, 5) = "OK"
        Else
            grdAgos.TextMatrix(linha, 5) = "DIVERGENTE - verificar"
            divergentes = divergentes + 1
        End If
        total = total + 1
        rsAgo.MoveNext
    Loop
    rsAgo.Close

    lblResumo.Caption = total & " assembleia(s) conferida(s), " & divergentes & " divergente(s)."
    Screen.MousePointer = vbDefault
End Sub
