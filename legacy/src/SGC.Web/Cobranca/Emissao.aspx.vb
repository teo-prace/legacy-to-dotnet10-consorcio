Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

Partial Class Cobranca_Emissao
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Dim d As Date = Date.Today.AddMonths(-1)
            For i As Integer = 0 To 3
                ddlMes.Items.Add(New ListItem(d.ToString("MM/yyyy"), (d.Year * 100 + d.Month).ToString()))
                d = d.AddMonths(1)
            Next
            ddlMes.SelectedIndex = 1
            btnEmitir.Enabled = PodeGravar()
            CarregaMeses()
        End If
    End Sub

    Private Sub CarregaMeses()
        gvMeses.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT TOP 12 RIGHT('0' + CAST(MONTH(DT_VENCIMENTO) AS VARCHAR), 2) + '/' + CAST(YEAR(DT_VENCIMENTO) AS VARCHAR) AS DS_MES, " & _
            "       SUM(CASE WHEN ID_SITUACAO = 1 THEN 1 ELSE 0 END) AS QT_ABERTAS, " & _
            "       SUM(CASE WHEN ID_SITUACAO = 2 THEN 1 ELSE 0 END) AS QT_EMITIDAS, " & _
            "       SUM(CASE WHEN ID_SITUACAO = 3 THEN 1 ELSE 0 END) AS QT_PAGAS, " & _
            "       SUM(CASE WHEN ID_SITUACAO IN (2, 3) THEN VL_PARCELA ELSE 0 END) AS VL_EMITIDO, " & _
            "       SUM(CASE WHEN ID_SITUACAO = 3 THEN VL_PAGO ELSE 0 END) AS VL_RECEBIDO " & _
            "  FROM TB_PARCELA " & _
            " WHERE DT_VENCIMENTO < DATEADD(MONTH, 3, GETDATE()) AND NR_PARCELA > 1 AND ID_SITUACAO <> 4 " & _
            " GROUP BY YEAR(DT_VENCIMENTO), MONTH(DT_VENCIMENTO) " & _
            " ORDER BY YEAR(DT_VENCIMENTO) DESC, MONTH(DT_VENCIMENTO) DESC")
        gvMeses.DataBind()
    End Sub

    Protected Sub btnEmitir_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnEmitir.Click
        Try
            gvResultado.DataSource = AcessoDados.ExecutaProcedure("SP_EMITE_COBRANCA", _
                AcessoDados.Parametro("@NR_ANO_MES", CInt(ddlMes.SelectedValue)), _
                AcessoDados.Parametro("@DT_EMISSAO", Date.Now), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            gvResultado.DataBind()
            gvResultado.Visible = True
            MostraMensagem(lblMensagem, "Emissão concluída.")
            CarregaMeses()
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

End Class
