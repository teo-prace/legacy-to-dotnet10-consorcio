VERSION 5.00
Begin VB.Form frmLogin
   BorderStyle     =   3  'Fixed Dialog
   Caption         =   "SGC - Acesso"
   ClientHeight    =   1800
   ClientLeft      =   45
   ClientTop       =   330
   ClientWidth     =   4200
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   1800
   ScaleWidth      =   4200
   StartUpPosition =   2  'CenterScreen
   Begin VB.CommandButton cmdCancelar
      Cancel          =   -1  'True
      Caption         =   "Cancelar"
      Height          =   375
      Left            =   2880
      TabIndex        =   5
      Top             =   1260
      Width           =   1095
   End
   Begin VB.CommandButton cmdEntrar
      Caption         =   "Entrar"
      Default         =   -1  'True
      Height          =   375
      Left            =   1680
      TabIndex        =   4
      Top             =   1260
      Width           =   1095
   End
   Begin VB.TextBox txtSenha
      Height          =   315
      IMEMode         =   3  'DISABLE
      Left            =   1200
      PasswordChar    =   "*"
      TabIndex        =   3
      Top             =   720
      Width           =   2775
   End
   Begin VB.TextBox txtLogin
      Height          =   315
      Left            =   1200
      TabIndex        =   1
      Top             =   240
      Width           =   2775
   End
   Begin VB.Label Label2
      Caption         =   "Senha:"
      Height          =   255
      Left            =   240
      TabIndex        =   2
      Top             =   780
      Width           =   855
   End
   Begin VB.Label Label1
      Caption         =   "Usuario:"
      Height          =   255
      Left            =   240
      TabIndex        =   0
      Top             =   300
      Width           =   855
   End
End
Attribute VB_Name = "frmLogin"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Private Sub cmdEntrar_Click()
    Dim rs As ADODB.Recordset

    ' senha comparada no servidor (mesmo MD5 da retaguarda web)
    Set rs = AbreRecordset("SELECT CD_USUARIO, NM_USUARIO, ID_PERFIL, FL_ATIVO FROM TB_USUARIO " & _
                           "WHERE DS_LOGIN = '" & txtLogin.Text & "' " & _
                           "AND DS_SENHA = CONVERT(VARCHAR(32), HASHBYTES('MD5', '" & txtSenha.Text & "'), 2)")

    If rs.EOF Then
        MsgBox "Usuario ou senha invalidos.", vbExclamation, "SGC"
        txtSenha.Text = ""
        txtSenha.SetFocus
        Exit Sub
    End If

    If rs!FL_ATIVO <> "S" Then
        MsgBox "Usuario inativo.", vbExclamation, "SGC"
        Exit Sub
    End If

    gCodigoUsuario = rs!CD_USUARIO
    gNomeUsuario = rs!NM_USUARIO
    gPerfil = rs!ID_PERFIL
    rs.Close

    GravaLog "LOGIN_DESKTOP", ""
    Unload Me
End Sub

Private Sub cmdCancelar_Click()
    gCodigoUsuario = 0
    Unload Me
End Sub
