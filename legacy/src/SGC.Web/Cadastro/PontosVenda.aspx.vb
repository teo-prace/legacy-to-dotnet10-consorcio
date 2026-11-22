Imports System.Data
Imports SGC.Comum

Partial Class Cadastro_PontosVenda
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaUF(ddlUFPonto)
            ddlUFPonto.SelectedValue = "RS"
            CarregaPontos()
        End If
    End Sub

    Private Sub CarregaPontos()
        gvPontos.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT P.CD_PONTO_VENDA, P.NM_PONTO_VENDA, P.NM_CIDADE, P.SG_UF, P.FL_ATIVO, " & _
            "       (SELECT COUNT(*) FROM TB_VENDEDOR V WHERE V.CD_PONTO_VENDA = P.CD_PONTO_VENDA AND V.FL_ATIVO = 'S') AS QT_VENDEDORES " & _
            "  FROM TB_PONTO_VENDA P ORDER BY P.CD_PONTO_VENDA")
        gvPontos.DataBind()
    End Sub

    Private Sub CarregaVendedores()
        Dim cd As Integer = CInt(gvPontos.SelectedDataKey.Value)
        litPonto.Text = gvPontos.SelectedRow.Cells(1).Text
        gvVendedores.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT V.CD_VENDEDOR, V.NM_VENDEDOR, V.FL_ATIVO, " & _
            "       (SELECT COUNT(*) FROM TB_PROPOSTA P WHERE P.CD_VENDEDOR = V.CD_VENDEDOR) AS QT_PROPOSTAS " & _
            "  FROM TB_VENDEDOR V WHERE V.CD_PONTO_VENDA = " & cd & " ORDER BY V.NM_VENDEDOR")
        gvVendedores.DataBind()
        pnlVendedores.Visible = True
    End Sub

    Protected Sub gvPontos_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles gvPontos.SelectedIndexChanged
        CarregaVendedores()
    End Sub

    Protected Sub btnIncluirPonto_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnIncluirPonto.Click
        If Not PodeGravar() Then Exit Sub
        If txtNomePonto.Text.Trim() = "" Or txtCidadePonto.Text.Trim() = "" Then
            MostraMensagem(lblMensagem, "Informe nome e cidade do ponto de venda.", True)
            Exit Sub
        End If
        AcessoDados.ExecutaNonQuery("INSERT INTO TB_PONTO_VENDA (NM_PONTO_VENDA, NM_CIDADE, SG_UF) VALUES (@NM, @CID, @UF)", _
            AcessoDados.Parametro("@NM", txtNomePonto.Text.Trim().ToUpper()), _
            AcessoDados.Parametro("@CID", txtCidadePonto.Text.Trim()), _
            AcessoDados.Parametro("@UF", ddlUFPonto.SelectedValue))
        txtNomePonto.Text = ""
        txtCidadePonto.Text = ""
        CarregaPontos()
        MostraMensagem(lblMensagem, "Ponto de venda incluído.")
    End Sub

    Protected Sub btnIncluirVendedor_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnIncluirVendedor.Click
        If Not PodeGravar() Or gvPontos.SelectedDataKey Is Nothing Then Exit Sub
        If txtNomeVendedor.Text.Trim() = "" Then Exit Sub
        AcessoDados.ExecutaNonQuery("INSERT INTO TB_VENDEDOR (CD_PONTO_VENDA, NM_VENDEDOR) VALUES (@PV, @NM)", _
            AcessoDados.Parametro("@PV", gvPontos.SelectedDataKey.Value), _
            AcessoDados.Parametro("@NM", txtNomeVendedor.Text.Trim().ToUpper()))
        txtNomeVendedor.Text = ""
        CarregaVendedores()
        CarregaPontos()
    End Sub

    Protected Sub gvVendedores_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles gvVendedores.RowCommand
        If e.CommandName = "AtivaInativa" AndAlso PodeGravar() Then
            Dim cd As Integer = CInt(gvVendedores.DataKeys(CInt(e.CommandArgument)).Value)
            AcessoDados.ExecutaNonQuery("UPDATE TB_VENDEDOR SET FL_ATIVO = CASE FL_ATIVO WHEN 'S' THEN 'N' ELSE 'S' END WHERE CD_VENDEDOR = " & cd)
            CarregaVendedores()
        End If
    End Sub

End Class
