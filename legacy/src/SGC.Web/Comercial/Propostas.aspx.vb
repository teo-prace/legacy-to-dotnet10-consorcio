Imports System.Data
Imports SGC.Comum

Partial Class Comercial_Propostas
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaCombo(ddlGrupo, "SELECT CD_GRUPO, NR_GRUPO FROM TB_GRUPO WHERE ID_SITUACAO IN (1, 2) ORDER BY NR_GRUPO", "NR_GRUPO", "CD_GRUPO", "(todos)")
            Pesquisa()
        End If
    End Sub

    Private Sub Pesquisa()
        Dim sql As String = "SELECT P.CD_PROPOSTA, P.NR_PROPOSTA, P.DT_PROPOSTA, P.VL_PRIMEIRA_PARCELA, P.ID_SITUACAO, " & _
                            "       C.NM_CONSORCIADO, G.NR_GRUPO, B.DS_BEM, PV.NM_PONTO_VENDA " & _
                            "  FROM TB_PROPOSTA P " & _
                            " INNER JOIN TB_CONSORCIADO C ON C.CD_CONSORCIADO = P.CD_CONSORCIADO " & _
                            " INNER JOIN TB_GRUPO G ON G.CD_GRUPO = P.CD_GRUPO " & _
                            " INNER JOIN TB_BEM B ON B.CD_BEM = P.CD_BEM " & _
                            " INNER JOIN TB_PONTO_VENDA PV ON PV.CD_PONTO_VENDA = P.CD_PONTO_VENDA " & _
                            " WHERE 1 = 1 "
        Dim parametros As New List(Of SqlClient.SqlParameter)

        If txtNumero.Text.Trim() <> "" Then
            sql &= " AND P.NR_PROPOSTA LIKE @NR"
            parametros.Add(AcessoDados.Parametro("@NR", "%" & txtNumero.Text.Trim() & "%"))
        End If
        If txtCPF.Text.Trim() <> "" Then
            sql &= " AND C.NR_CPF = @CPF"
            parametros.Add(AcessoDados.Parametro("@CPF", Validacao.SomenteNumeros(txtCPF.Text)))
        End If
        If ddlGrupo.SelectedValue <> "" Then
            sql &= " AND P.CD_GRUPO = " & ddlGrupo.SelectedValue
        End If
        If ddlSituacao.SelectedValue <> "" Then
            sql &= " AND P.ID_SITUACAO = " & ddlSituacao.SelectedValue
        End If
        sql &= " ORDER BY P.DT_PROPOSTA DESC, P.CD_PROPOSTA DESC"

        gvPropostas.DataSource = AcessoDados.ExecutaDataTable(sql, parametros.ToArray())
        gvPropostas.DataBind()
    End Sub

    Protected Sub btnPesquisar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPesquisar.Click
        gvPropostas.PageIndex = 0
        Pesquisa()
    End Sub

    Protected Sub btnNova_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnNova.Click
        Response.Redirect("PropostaEdicao.aspx")
    End Sub

    Protected Sub gvPropostas_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles gvPropostas.PageIndexChanging
        gvPropostas.PageIndex = e.NewPageIndex
        Pesquisa()
    End Sub

End Class
