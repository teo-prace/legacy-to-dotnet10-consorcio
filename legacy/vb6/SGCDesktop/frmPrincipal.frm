VERSION 5.00
Begin VB.MDIForm frmPrincipal
   BackColor       =   &H8000000C&
   Caption         =   "SGC Desktop - CeCons Consorcios"
   ClientHeight    =   6900
   ClientLeft      =   165
   ClientTop       =   855
   ClientWidth     =   10200
   LinkTopic       =   "MDIForm1"
   StartUpPosition =   2  'CenterScreen
   WindowState     =   2  'Maximized
   Begin VB.Menu mnuTabelas
      Caption         =   "&Tabelas"
      Begin VB.Menu mnuIndices
         Caption         =   "&Indices de reajuste"
      End
   End
   Begin VB.Menu mnuAssembleia
      Caption         =   "&Assembleia"
      Begin VB.Menu mnuApuracao
         Caption         =   "&Conferencia de apuracao"
      End
   End
   Begin VB.Menu mnuSair
      Caption         =   "&Sair"
   End
End
Attribute VB_Name = "frmPrincipal"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
'==============================================================================
' SGC Desktop
' Desde 2011 os cadastros e a venda estao na retaguarda web.
' Ficaram aqui: indices de reajuste e conferencia da apuracao.
'==============================================================================
Option Explicit

Private Sub MDIForm_Load()
    Me.Caption = "SGC Desktop - CeCons Consorcios - " & gNomeUsuario
End Sub

Private Sub mnuIndices_Click()
    frmIndices.Show
End Sub

Private Sub mnuApuracao_Click()
    frmConferencia.Show
End Sub

Private Sub mnuSair_Click()
    Unload Me
End Sub

Private Sub MDIForm_Unload(Cancel As Integer)
    On Error Resume Next
    cn.Close
    End
End Sub
