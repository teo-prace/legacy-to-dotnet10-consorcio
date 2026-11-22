Imports SGC.Comum

Partial Class Cobranca_Retornos
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaArquivos()
        End If
    End Sub

    Private Sub CarregaArquivos()
        gvArquivos.DataSource = AcessoDados.ExecutaDataTable("SELECT * FROM TB_RETORNO_ARQUIVO ORDER BY DT_PROCESSAMENTO DESC")
        gvArquivos.DataBind()
    End Sub

    Protected Sub gvArquivos_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles gvArquivos.SelectedIndexChanged
        Dim cd As Integer = CInt(gvArquivos.SelectedDataKey.Value)
        litArquivo.Text = gvArquivos.SelectedRow.Cells(1).Text
        gvDetalhe.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT D.*, C.CD_COTA, G.NR_GRUPO + '/' + RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR) AS DS_COTA " & _
            "  FROM TB_RETORNO_DETALHE D " & _
            "  LEFT JOIN TB_PARCELA P ON P.CD_PARCELA = D.CD_PARCELA " & _
            "  LEFT JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA " & _
            "  LEFT JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO " & _
            " WHERE D.CD_RETORNO_ARQUIVO = " & cd & " ORDER BY D.NR_LINHA")
        gvDetalhe.DataBind()
        pnlDetalhe.Visible = True
    End Sub

End Class
