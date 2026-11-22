Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

Partial Class Cobranca_Inadimplencia
    Inherits PaginaBase

    Private _limite As Integer = 3

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        _limite = CInt(Parametros.Le("QT_PARCELAS_EXCLUSAO"))
        litQt.Text = _limite.ToString()
        If Not IsPostBack Then
            CarregaCombo(ddlGrupo, "SELECT CD_GRUPO, NR_GRUPO FROM TB_GRUPO WHERE ID_SITUACAO = 2 ORDER BY NR_GRUPO", "NR_GRUPO", "CD_GRUPO", "(todos)")
            btnExcluir.Enabled = (PerfilUsuario = 1)
            Carrega()
        End If
    End Sub

    Private Sub Carrega()
        Dim sql As String = _
            "SELECT C.CD_COTA, C.ID_SITUACAO, X.NM_CONSORCIADO, X.NR_CELULAR, COUNT(*) AS QT_VENCIDAS, MIN(P.DT_VENCIMENTO) AS DT_PRIMEIRA, SUM(P.VL_PARCELA) AS VL_ABERTO, " & _
            "       G.NR_GRUPO + '/' + RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR) AS DS_COTA " & _
            "  FROM TB_PARCELA P INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA " & _
            " INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
            " WHERE C.ID_SITUACAO IN (1, 2) AND P.ID_SITUACAO = 2 " & _
            "   AND dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) < CONVERT(VARCHAR(8), GETDATE(), 112) "
        If ddlGrupo.SelectedValue <> "" Then sql &= " AND C.CD_GRUPO = " & ddlGrupo.SelectedValue
        sql &= " GROUP BY C.CD_COTA, C.ID_SITUACAO, X.NM_CONSORCIADO, X.NR_CELULAR, G.NR_GRUPO, C.NR_COTA, C.NR_VERSAO " & _
               " ORDER BY COUNT(*) DESC, G.NR_GRUPO, C.NR_COTA"
        gvAtraso.DataSource = AcessoDados.ExecutaDataTable(sql)
        gvAtraso.DataBind()
    End Sub

    Protected Function Encaminhamento(ByVal situacao As Object, ByVal vencidas As Object) As String
        If CInt(situacao) = 2 Then Return "<b style='color:#8b0000'>Cobrança jurídica (contemplada)</b>"
        If CInt(vencidas) >= _limite Then Return "<b style='color:#8b0000'>Exclusão</b>"
        If CInt(vencidas) = _limite - 1 Then Return "Aviso de exclusão"
        Return "Cobrança amigável"
    End Function

    Protected Sub ddlGrupo_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlGrupo.SelectedIndexChanged
        Carrega()
    End Sub

    Protected Sub btnExcluir_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExcluir.Click
        Try
            Dim ds As DataSet = AcessoDados.ExecutaProcedureDataSet("SP_EXCLUI_INADIMPLENTES", _
                AcessoDados.Parametro("@DT_REFERENCIA", Date.Now), AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            Carrega()
            MostraMensagem(lblMensagem, ds.Tables(0).Rows.Count & " cota(s) excluída(s).")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

End Class
