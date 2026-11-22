Imports System.Data

'==============================================================================
' Toda pagina da retaguarda deve herdar desta classe (confere a sessao).
' 2011-04 M.S.
'==============================================================================
Public Class PaginaBase
    Inherits System.Web.UI.Page

    Protected ReadOnly Property UsuarioLogado() As DataRow
        Get
            Return CType(Session("Usuario"), DataRow)
        End Get
    End Property

    Protected ReadOnly Property CodigoUsuario() As Integer
        Get
            Return CInt(UsuarioLogado("CD_USUARIO"))
        End Get
    End Property

    Protected ReadOnly Property PerfilUsuario() As Integer
        Get
            Return CInt(UsuarioLogado("ID_PERFIL"))
        End Get
    End Property

    Protected Overrides Sub OnInit(ByVal e As EventArgs)
        If Session("Usuario") Is Nothing Then
            Response.Redirect("~/Login.aspx?ReturnUrl=" & Server.UrlEncode(Request.RawUrl), True)
        End If
        MyBase.OnInit(e)
    End Sub

    Protected Sub MostraMensagem(ByVal lbl As System.Web.UI.WebControls.Label, ByVal texto As String, Optional ByVal erro As Boolean = False)
        lbl.Text = texto
        lbl.CssClass = IIf(erro, "msgErro", "msgOk")
        lbl.Visible = True
    End Sub

    ' perfil 3 (consulta) nao grava
    Protected Function PodeGravar() As Boolean
        Return PerfilUsuario <> 3
    End Function

End Class
