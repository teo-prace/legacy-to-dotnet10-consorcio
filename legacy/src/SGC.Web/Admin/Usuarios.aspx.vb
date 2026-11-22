Imports System.Data
Imports SGC.Comum

Partial Class Admin_Usuarios
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If PerfilUsuario <> 1 Then
            Response.Redirect("~/Default.aspx")
        End If
        If Not IsPostBack Then
            Carrega()
        End If
    End Sub

    Private Sub Carrega()
        gvUsuarios.DataSource = AcessoDados.ExecutaDataTable("SELECT * FROM TB_USUARIO ORDER BY FL_ATIVO DESC, DS_LOGIN")
        gvUsuarios.DataBind()
    End Sub

    Private Sub Limpa()
        ViewState("CD_USUARIO") = Nothing
        litForm.Text = "Novo usuário"
        txtLogin.Text = ""
        txtLogin.ReadOnly = False
        txtNome.Text = ""
        txtEmail.Text = ""
        ddlPerfil.SelectedValue = "2"
        chkAtivo.Checked = True
        chkResetSenha.Checked = False
    End Sub

    Protected Sub gvUsuarios_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles gvUsuarios.RowCommand
        If e.CommandName <> "Editar" Then Exit Sub
        Dim cd As Integer = CInt(gvUsuarios.DataKeys(CInt(e.CommandArgument)).Value)
        Dim r As DataRow = AcessoDados.ExecutaDataTable("SELECT * FROM TB_USUARIO WHERE CD_USUARIO = " & cd).Rows(0)
        ViewState("CD_USUARIO") = cd
        litForm.Text = "Editar usuário"
        txtLogin.Text = r("DS_LOGIN").ToString()
        txtLogin.ReadOnly = True
        txtNome.Text = r("NM_USUARIO").ToString()
        txtEmail.Text = r("DS_EMAIL").ToString()
        ddlPerfil.SelectedValue = r("ID_PERFIL").ToString()
        chkAtivo.Checked = (r("FL_ATIVO").ToString() = "S")
        chkResetSenha.Checked = False
    End Sub

    Protected Sub btnGravar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnGravar.Click
        If txtLogin.Text.Trim() = "" Or txtNome.Text.Trim() = "" Then
            MostraMensagem(lblMensagem, "Informe login e nome.", True)
            Exit Sub
        End If

        Try
            If ViewState("CD_USUARIO") Is Nothing Then
                AcessoDados.ExecutaNonQuery( _
                    "INSERT INTO TB_USUARIO (DS_LOGIN, DS_SENHA, NM_USUARIO, DS_EMAIL, ID_PERFIL, FL_ATIVO) VALUES (@LOGIN, @SENHA, @NOME, @EMAIL, @PERFIL, @ATIVO)", _
                    AcessoDados.Parametro("@LOGIN", txtLogin.Text.Trim().ToLower()), _
                    AcessoDados.Parametro("@SENHA", Criptografia.MD5(Constantes.SENHA_PADRAO)), _
                    AcessoDados.Parametro("@NOME", txtNome.Text.Trim()), _
                    AcessoDados.Parametro("@EMAIL", NuloSeVazio(txtEmail.Text)), _
                    AcessoDados.Parametro("@PERFIL", ddlPerfil.SelectedValue), _
                    AcessoDados.Parametro("@ATIVO", IIf(chkAtivo.Checked, "S", "N")))
                MostraMensagem(lblMensagem, "Usuário incluído com a senha padrão.")
            Else
                AcessoDados.ExecutaNonQuery( _
                    "UPDATE TB_USUARIO SET NM_USUARIO = @NOME, DS_EMAIL = @EMAIL, ID_PERFIL = @PERFIL, FL_ATIVO = @ATIVO WHERE CD_USUARIO = @CD", _
                    AcessoDados.Parametro("@NOME", txtNome.Text.Trim()), _
                    AcessoDados.Parametro("@EMAIL", NuloSeVazio(txtEmail.Text)), _
                    AcessoDados.Parametro("@PERFIL", ddlPerfil.SelectedValue), _
                    AcessoDados.Parametro("@ATIVO", IIf(chkAtivo.Checked, "S", "N")), _
                    AcessoDados.Parametro("@CD", ViewState("CD_USUARIO")))
                If chkResetSenha.Checked Then
                    AcessoDados.ExecutaNonQuery("UPDATE TB_USUARIO SET DS_SENHA = '" & Criptografia.MD5(Constantes.SENHA_PADRAO) & "' WHERE CD_USUARIO = " & ViewState("CD_USUARIO"))
                End If
                MostraMensagem(lblMensagem, "Usuário alterado.")
            End If
            Limpa()
            Carrega()
        Catch ex As Exception
            MostraMensagem(lblMensagem, "Erro ao gravar. Verifique se o login já existe.", True)
        End Try
    End Sub

    Protected Sub btnLimpar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLimpar.Click
        Limpa()
    End Sub

End Class
