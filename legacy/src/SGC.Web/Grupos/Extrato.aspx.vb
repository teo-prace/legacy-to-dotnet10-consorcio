Imports System.Data
Imports SGC.Comum

'==============================================================================
' Extrato de movimentacao do grupo.
' 2016-09 J.P. (copiada da tela de saldos para o financeiro conferir)
'==============================================================================
Partial Class Grupos_Extrato
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Dim cdGrupo As Integer = CInt(Request.QueryString("id"))
            litGrupo.Text = AcessoDados.ExecutaEscalar("SELECT NR_GRUPO FROM TB_GRUPO WHERE CD_GRUPO = " & cdGrupo).ToString()
            lnkGrupo.NavigateUrl = "GrupoEdicao.aspx?id=" & cdGrupo
            gvSaldos.DataSource = AcessoDados.ExecutaProcedure("SP_SALDO_GRUPO", AcessoDados.Parametro("@CD_GRUPO", cdGrupo))
            gvSaldos.DataBind()
            Carrega()
        End If
    End Sub

    Private Sub Carrega()
        Dim de As Object = Nothing
        Dim ate As Object = Nothing
        If txtDe.Text <> "" Then de = Date.ParseExact(txtDe.Text, "dd/MM/yyyy", Nothing)
        If txtAte.Text <> "" Then ate = Date.ParseExact(txtAte.Text, "dd/MM/yyyy", Nothing)

        gvExtrato.DataSource = AcessoDados.ExecutaProcedure("SP_EXTRATO_GRUPO", _
            AcessoDados.Parametro("@CD_GRUPO", CInt(Request.QueryString("id"))), _
            AcessoDados.Parametro("@SG_FUNDO", NuloSeVazio(ddlFundo.SelectedValue)), _
            AcessoDados.Parametro("@DT_INI", de), _
            AcessoDados.Parametro("@DT_FIM", ate))
        gvExtrato.DataBind()
    End Sub

    Protected Sub btnFiltrar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnFiltrar.Click
        gvExtrato.PageIndex = 0
        Carrega()
    End Sub

    Protected Sub gvExtrato_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles gvExtrato.PageIndexChanging
        gvExtrato.PageIndex = e.NewPageIndex
        Carrega()
    End Sub

End Class
