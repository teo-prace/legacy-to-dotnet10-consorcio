Imports System.Data
Imports SGC.Comum

Partial Class Login
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            txtLogin.Focus()
        End If
    End Sub

    Protected Sub btnEntrar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnEntrar.Click
        If txtLogin.Text.Trim() = "" Or txtSenha.Text = "" Then
            lblMensagem.Text = "Informe usuário e senha."
            lblMensagem.CssClass = "msgErro"
            lblMensagem.Visible = True
            Exit Sub
        End If

        Dim dt As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT * FROM TB_USUARIO WHERE DS_LOGIN = @LOGIN AND DS_SENHA = @SENHA", _
            AcessoDados.Parametro("@LOGIN", txtLogin.Text.Trim()), _
            AcessoDados.Parametro("@SENHA", Criptografia.MD5(txtSenha.Text)))

        If dt.Rows.Count = 0 Then
            lblMensagem.Text = "Usuário ou senha inválidos."
            lblMensagem.CssClass = "msgErro"
            lblMensagem.Visible = True
            Log.Grava("Login invalido: " & txtLogin.Text)
            Exit Sub
        End If

        If dt.Rows(0)("FL_ATIVO").ToString() <> "S" Then
            lblMensagem.Text = "Usuário inativo. Procure o administrador."
            lblMensagem.CssClass = "msgErro"
            lblMensagem.Visible = True
            Exit Sub
        End If

        Session("Usuario") = dt.Rows(0)

        AcessoDados.ExecutaNonQuery("UPDATE TB_USUARIO SET DT_ULTIMO_ACESSO = GETDATE() WHERE CD_USUARIO = " & dt.Rows(0)("CD_USUARIO"))
        AcessoDados.ExecutaNonQuery("EXEC SP_GRAVA_LOG " & dt.Rows(0)("CD_USUARIO") & ", 'LOGIN', NULL")

        Dim retorno As String = Request.QueryString("ReturnUrl")
        If retorno <> "" AndAlso retorno.StartsWith("/") Then
            Response.Redirect(retorno)
        Else
            Response.Redirect("~/Default.aspx")
        End If
    End Sub

End Class
