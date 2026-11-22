Imports System.Data
Imports SGC.Comum

Partial Class Cadastro_Consorciados
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            If Session("FiltroConsorciado") IsNot Nothing Then
                txtNome.Text = Session("FiltroConsorciado").ToString()
            End If
            Pesquisa()
        End If
    End Sub

    Private Sub Pesquisa()
        Dim sql As String = "SELECT C.CD_CONSORCIADO, C.NR_CPF, C.NM_CONSORCIADO, C.NM_CIDADE, C.SG_UF, C.NR_CELULAR, C.FL_ATIVO, " & _
                            "       (SELECT COUNT(*) FROM TB_COTA T WHERE T.CD_CONSORCIADO = C.CD_CONSORCIADO AND T.ID_SITUACAO <> 6) AS QT_COTAS " & _
                            "  FROM TB_CONSORCIADO C WHERE 1 = 1 "

        If txtNome.Text.Trim() <> "" Then
            sql &= " AND C.NM_CONSORCIADO LIKE '%" & txtNome.Text.Trim().ToUpper() & "%'"
        End If
        If txtCPF.Text.Trim() <> "" Then
            sql &= " AND C.NR_CPF LIKE '" & Validacao.SomenteNumeros(txtCPF.Text) & "%'"
        End If
        If txtCidade.Text.Trim() <> "" Then
            sql &= " AND C.NM_CIDADE LIKE '" & txtCidade.Text.Trim() & "%'"
        End If
        sql &= " ORDER BY C.NM_CONSORCIADO"

        Dim dt As DataTable = AcessoDados.ExecutaDataTable(sql)
        gvConsorciados.DataSource = dt
        gvConsorciados.DataBind()
        lblTotal.Text = dt.Rows.Count & " consorciado(s)"

        Session("FiltroConsorciado") = txtNome.Text
    End Sub

    Protected Sub btnPesquisar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPesquisar.Click
        gvConsorciados.PageIndex = 0
        Pesquisa()
    End Sub

    Protected Sub btnNovo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnNovo.Click
        Response.Redirect("ConsorciadoEdicao.aspx")
    End Sub

    Protected Sub gvConsorciados_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles gvConsorciados.PageIndexChanging
        gvConsorciados.PageIndex = e.NewPageIndex
        Pesquisa()
    End Sub

End Class
