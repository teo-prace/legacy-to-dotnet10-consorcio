Imports System.Data

Partial Class Site
    Inherits System.Web.UI.MasterPage

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim usu As DataRow = CType(Session("Usuario"), DataRow)
        If usu IsNot Nothing Then
            lblUsuario.Text = usu("NM_USUARIO").ToString()
            pnlAdmin.Visible = (CInt(usu("ID_PERFIL")) = 1)
        Else
            lblUsuario.Text = "(não identificado)"
            pnlAdmin.Visible = False
        End If
        lblData.Text = Date.Now.ToString("dd/MM/yyyy")
    End Sub

    Protected Sub lnkSair_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkSair.Click
        Session.Abandon()
        Response.Redirect("~/Login.aspx")
    End Sub

End Class
