Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

Partial Class Assembleia_Assembleias
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaCombo(ddlGrupo, "SELECT CD_GRUPO, NR_GRUPO FROM TB_GRUPO WHERE ID_SITUACAO = 2 ORDER BY NR_GRUPO", "NR_GRUPO", "CD_GRUPO")
            If Request.QueryString("grupo") <> "" Then ddlGrupo.SelectedValue = Request.QueryString("grupo")
            btnAgendar.Enabled = PodeGravar()
            Carrega()
        End If
    End Sub

    Private Sub Carrega()
        If ddlGrupo.SelectedValue = "" Then Exit Sub
        gvAssembleias.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT A.*, (SELECT COUNT(*) FROM TB_LANCE L WHERE L.CD_ASSEMBLEIA = A.CD_ASSEMBLEIA AND L.ID_SITUACAO <> 5) AS QT_LANCES " & _
            "  FROM TB_ASSEMBLEIA A WHERE A.CD_GRUPO = " & ddlGrupo.SelectedValue & " ORDER BY A.NR_ASSEMBLEIA DESC")
        gvAssembleias.DataBind()
    End Sub

    Protected Sub ddlGrupo_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlGrupo.SelectedIndexChanged
        Carrega()
    End Sub

    Protected Sub btnAgendar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAgendar.Click
        Try
            Dim pId As SqlParameter = AcessoDados.ParametroSaida("@CD_ASSEMBLEIA", SqlDbType.Int)
            AcessoDados.ExecutaProcedureRetorno("SP_AGENDA_ASSEMBLEIA", _
                AcessoDados.Parametro("@CD_GRUPO", CInt(ddlGrupo.SelectedValue)), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario), pId)
            Response.Redirect("AssembleiaEdicao.aspx?id=" & pId.Value, False)
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

End Class
