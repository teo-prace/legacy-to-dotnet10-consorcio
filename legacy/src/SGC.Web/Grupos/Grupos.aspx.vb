Imports SGC.Comum

Partial Class Grupos_Grupos
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim sql As String = "SELECT G.*, P.NM_PLANO, " & _
                            "       (SELECT COUNT(*) FROM TB_COTA C WHERE C.CD_GRUPO = G.CD_GRUPO AND C.ID_SITUACAO IN (1, 2)) AS QT_ATIVAS " & _
                            "  FROM TB_GRUPO G INNER JOIN TB_PLANO P ON P.CD_PLANO = G.CD_PLANO "
        If ddlSituacao.SelectedValue <> "" Then sql &= " WHERE G.ID_SITUACAO = " & ddlSituacao.SelectedValue
        sql &= " ORDER BY G.NR_GRUPO"
        gvGrupos.DataSource = AcessoDados.ExecutaDataTable(sql)
        gvGrupos.DataBind()
    End Sub

    Protected Sub btnNovo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnNovo.Click
        Response.Redirect("GrupoEdicao.aspx")
    End Sub

End Class
