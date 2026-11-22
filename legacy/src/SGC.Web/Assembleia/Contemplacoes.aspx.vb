Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

Partial Class Assembleia_Contemplacoes
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaCombo(ddlGrupo, "SELECT CD_GRUPO, NR_GRUPO FROM TB_GRUPO WHERE ID_SITUACAO IN (2, 3) ORDER BY NR_GRUPO", "NR_GRUPO", "CD_GRUPO", "(todos)")
            Carrega()
        End If
    End Sub

    Private Sub Carrega()
        Dim sql As String = _
            "SELECT T.*, A.NR_ASSEMBLEIA, X.NM_CONSORCIADO, C.CD_COTA, " & _
            "       CASE T.ID_TIPO WHEN 1 THEN 'Sorteio' WHEN 3 THEN 'Devolução' ELSE 'Lance ' + CAST(CAST(T.PC_LANCE AS DECIMAL(5,2)) AS VARCHAR) + '%' END AS DS_TIPO, " & _
            "       CASE T.ID_SITUACAO WHEN 1 THEN 'Aguardando liberação' WHEN 2 THEN 'Crédito liberado' ELSE 'Cancelada' END AS DS_SITUACAO, " & _
            "       G.NR_GRUPO + '/' + RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR) AS DS_COTA " & _
            "  FROM TB_CONTEMPLACAO T INNER JOIN TB_ASSEMBLEIA A ON A.CD_ASSEMBLEIA = T.CD_ASSEMBLEIA " & _
            " INNER JOIN TB_COTA C ON C.CD_COTA = T.CD_COTA INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO " & _
            " INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO WHERE 1 = 1 "
        If ddlGrupo.SelectedValue <> "" Then sql &= " AND C.CD_GRUPO = " & ddlGrupo.SelectedValue
        If ddlSituacao.SelectedValue <> "" Then sql &= " AND T.ID_SITUACAO = " & ddlSituacao.SelectedValue
        sql &= " ORDER BY T.DT_CONTEMPLACAO DESC, T.CD_CONTEMPLACAO DESC"

        gvContemplacoes.SelectedIndex = -1
        gvContemplacoes.DataSource = AcessoDados.ExecutaDataTable(sql)
        gvContemplacoes.DataBind()
        pnlLiberar.Visible = False
    End Sub

    Protected Sub Filtro_Changed(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlGrupo.SelectedIndexChanged, ddlSituacao.SelectedIndexChanged
        Carrega()
    End Sub

    Protected Sub gvContemplacoes_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles gvContemplacoes.SelectedIndexChanged
        Dim situacao As String = gvContemplacoes.SelectedRow.Cells(7).Text
        litCota.Text = CType(gvContemplacoes.SelectedRow.Cells(3).Controls(0), HyperLink).Text
        pnlLiberar.Visible = (situacao.StartsWith("Aguardando") And PodeGravar())
        txtDataLiberacao.Text = Date.Today.ToString("dd/MM/yyyy")
        If Not pnlLiberar.Visible Then MostraMensagem(lblMensagem, "Só é possível liberar contemplações aguardando liberação.", True)
    End Sub

    Protected Sub btnLiberar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnLiberar.Click
        If Not Validacao.ValidaData(txtDataLiberacao.Text) Then
            MostraMensagem(lblMensagem, "Data inválida.", True)
            Exit Sub
        End If
        Try
            AcessoDados.ExecutaProcedureRetorno("SP_LIBERA_CREDITO", _
                AcessoDados.Parametro("@CD_CONTEMPLACAO", gvContemplacoes.SelectedDataKey.Value), _
                AcessoDados.Parametro("@DT_LIBERACAO", Date.ParseExact(txtDataLiberacao.Text, "dd/MM/yyyy", Nothing)), _
                AcessoDados.Parametro("@DS_FAVORECIDO", txtFavorecido.Text.Trim().ToUpper()), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            Carrega()
            MostraMensagem(lblMensagem, "Crédito liberado.")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

    Protected Sub btnCancelar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCancelar.Click
        Try
            AcessoDados.ExecutaProcedureRetorno("SP_CANCELA_CONTEMPLACAO", _
                AcessoDados.Parametro("@CD_CONTEMPLACAO", gvContemplacoes.SelectedDataKey.Value), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            Carrega()
            MostraMensagem(lblMensagem, "Contemplação cancelada.")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

End Class
