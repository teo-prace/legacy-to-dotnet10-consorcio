Imports System.Data
Imports SGC.Comum

Partial Class Cobranca_Parcelas
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaCombo(ddlGrupo, "SELECT CD_GRUPO, NR_GRUPO FROM TB_GRUPO WHERE ID_SITUACAO = 2 ORDER BY NR_GRUPO", "NR_GRUPO", "CD_GRUPO", "(todos)")
            If Request.QueryString("grupo") <> "" Then ddlGrupo.SelectedValue = Request.QueryString("grupo")
            Pesquisa()
        End If
    End Sub

    Private Sub Pesquisa()
        Dim sql As String = _
            "SELECT TOP 2000 P.CD_PARCELA, P.CD_COTA, P.NR_PARCELA, P.DT_VENCIMENTO, P.NR_NOSSO_NUMERO, P.VL_PARCELA, P.DT_PAGAMENTO, P.VL_PAGO, P.ID_SITUACAO, " & _
            "       G.NR_GRUPO + '/' + RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR) AS DS_COTA, X.NM_CONSORCIADO, " & _
            "       CASE WHEN P.ID_SITUACAO = 2 AND dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) < GETDATE() - 1 THEN DATEDIFF(DAY, P.DT_VENCIMENTO, GETDATE()) " & _
            "            WHEN P.ID_SITUACAO = 3 AND P.DT_PAGAMENTO > dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) THEN DATEDIFF(DAY, P.DT_VENCIMENTO, P.DT_PAGAMENTO) END AS QT_DIAS_ATRASO " & _
            "  FROM TB_PARCELA P " & _
            " INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA " & _
            " INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO " & _
            " INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
            " WHERE P.ID_SITUACAO <> 4 "

        If ddlGrupo.SelectedValue <> "" Then sql &= " AND C.CD_GRUPO = " & ddlGrupo.SelectedValue
        Select Case ddlSituacao.SelectedValue
            Case "V"
                sql &= " AND P.ID_SITUACAO = 2 AND dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) < CONVERT(VARCHAR(8), GETDATE(), 112) "
            Case ""
            Case Else
                sql &= " AND P.ID_SITUACAO = " & ddlSituacao.SelectedValue
        End Select
        If txtDe.Text <> "" Then sql &= " AND P.DT_VENCIMENTO >= '" & Date.ParseExact(txtDe.Text, "dd/MM/yyyy", Nothing).ToString("yyyyMMdd") & "'"
        If txtAte.Text <> "" Then sql &= " AND P.DT_VENCIMENTO <= '" & Date.ParseExact(txtAte.Text, "dd/MM/yyyy", Nothing).ToString("yyyyMMdd") & "'"
        If txtNossoNumero.Text.Trim() <> "" Then sql &= " AND P.NR_NOSSO_NUMERO LIKE '%" & txtNossoNumero.Text.Trim() & "%'"
        sql &= " ORDER BY P.DT_VENCIMENTO DESC, G.NR_GRUPO, C.NR_COTA"

        Dim dt As DataTable = AcessoDados.ExecutaDataTable(sql)
        gvParcelas.DataSource = dt
        gvParcelas.DataBind()
        lblTotal.Text = dt.Rows.Count & " parcela(s)" & IIf(dt.Rows.Count = 2000, " (limitado a 2000; refine o filtro)", "")
    End Sub

    Protected Sub btnPesquisar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPesquisar.Click
        gvParcelas.PageIndex = 0
        Pesquisa()
    End Sub

    Protected Sub gvParcelas_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles gvParcelas.PageIndexChanging
        gvParcelas.PageIndex = e.NewPageIndex
        Pesquisa()
    End Sub

End Class
