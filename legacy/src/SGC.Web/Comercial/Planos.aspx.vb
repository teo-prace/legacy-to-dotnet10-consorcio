Imports SGC.Comum

Partial Class Comercial_Planos
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Carrega()
    End Sub

    Private Sub Carrega()
        Dim sql As String = "SELECT P.*, I.SG_INDICE, (SELECT COUNT(*) FROM TB_GRUPO G WHERE G.CD_PLANO = P.CD_PLANO) AS QT_GRUPOS " & _
                            "  FROM TB_PLANO P INNER JOIN TB_INDICE I ON I.CD_INDICE = P.CD_INDICE "
        If Not chkInativos.Checked Then sql &= " WHERE P.FL_ATIVO = 'S' "
        sql &= " ORDER BY P.FL_ATIVO DESC, P.ID_TIPO_BEM, P.NM_PLANO"
        gvPlanos.DataSource = AcessoDados.ExecutaDataTable(sql)
        gvPlanos.DataBind()
    End Sub

    Protected Sub btnNovo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnNovo.Click
        Response.Redirect("PlanoEdicao.aspx")
    End Sub

End Class
