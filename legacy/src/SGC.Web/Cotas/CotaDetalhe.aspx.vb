Imports System.Data
Imports SGC.Comum

Partial Class Cotas_CotaDetalhe
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Carrega(CInt(Request.QueryString("id")))
        End If
    End Sub

    Private Sub Carrega(ByVal cdCota As Integer)
        Dim dt As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT C.*, G.NR_GRUPO, X.NM_CONSORCIADO, B.DS_BEM, B.VL_BEM AS VL_BEM_ATUAL, P.NR_PROPOSTA " & _
            "  FROM TB_COTA C " & _
            " INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO " & _
            " INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
            " INNER JOIN TB_BEM B ON B.CD_BEM = C.CD_BEM " & _
            "  LEFT JOIN TB_PROPOSTA P ON P.CD_PROPOSTA = C.CD_PROPOSTA " & _
            " WHERE C.CD_COTA = " & cdCota)
        If dt.Rows.Count = 0 Then Exit Sub
        Dim c As DataRow = dt.Rows(0)
        Dim valorAtual As Decimal = CDec(c("VL_BEM_ATUAL"))

        litCota.Text = Formatacao.FormataGrupoCota(c("NR_GRUPO"), c("NR_COTA"), c("NR_VERSAO"))
        lnkConsorciado.Text = c("NM_CONSORCIADO").ToString()
        lnkConsorciado.NavigateUrl = "~/Cadastro/ConsorciadoEdicao.aspx?id=" & c("CD_CONSORCIADO")
        litSituacao.Text = DescricaoSituacaoCota(c("ID_SITUACAO"))
        lnkGrupo.Text = c("NR_GRUPO").ToString()
        lnkGrupo.NavigateUrl = "~/Grupos/GrupoEdicao.aspx?id=" & c("CD_GRUPO")
        lnkProposta.Text = c("NR_PROPOSTA").ToString()
        lnkProposta.NavigateUrl = "~/Comercial/PropostaEdicao.aspx?id=" & c("CD_PROPOSTA").ToString()
        litBem.Text = c("DS_BEM").ToString()
        litAdesao.Text = Formatacao.FormataData(c("DT_ADESAO"))
        litCreditoAdesao.Text = Formatacao.FormataMoeda(c("VL_CREDITO_ADESAO"))
        litCreditoAtual.Text = Formatacao.FormataMoeda(valorAtual)
        litPrazo.Text = c("QT_PRAZO_COTA") & " meses"
        litAmortizado.Text = Formatacao.FormataPercentual(c("PC_AMORTIZADO"), 6)

        ' desistencia: so ativa nao contemplada
        Dim bruto As Decimal = Math.Round(valorAtual * CDec(c("PC_AMORTIZADO")) / 100, 2)
        Dim multa As Decimal = Math.Round(bruto * Parametros.LeDecimal("PC_MULTA_RESCISORIA") / 100, 2)
        litEstimativa.Text = Formatacao.FormataMoeda(bruto - multa)
        pnlDesistencia.Visible = (CInt(c("ID_SITUACAO")) = 1 And PodeGravar())
        ViewState("CD_COTA") = cdCota

        Dim parcelas As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT P.*, P.PC_FUNDO_COMUM + P.PC_TAXA_ADM + P.PC_FUNDO_RESERVA AS PC_TOTAL, " & _
            "       CAST(NULL AS MONEY) AS VL_ESTIMADO " & _
            "  FROM TB_PARCELA P WHERE P.CD_COTA = " & cdCota & " ORDER BY P.NR_PARCELA")

        Dim totFC As Decimal = 0, totTA As Decimal = 0, totFR As Decimal = 0
        Dim pagas As Integer = 0
        Dim saldoPc As Decimal = 0
        For Each p As DataRow In parcelas.Rows
            p("VL_ESTIMADO") = Math.Round(valorAtual * CDec(p("PC_TOTAL")) / 100, 2)
            totFC += CDec(p("PC_FUNDO_COMUM"))
            totTA += CDec(p("PC_TAXA_ADM"))
            totFR += CDec(p("PC_FUNDO_RESERVA"))
            If CInt(p("ID_SITUACAO")) = 3 Then
                pagas += 1
            ElseIf CInt(p("ID_SITUACAO")) <> 4 Then
                saldoPc += CDec(p("PC_TOTAL"))
            End If
        Next

        litPagas.Text = pagas & " de " & parcelas.Rows.Count
        litSaldoDevedor.Text = Formatacao.FormataPercentual(saldoPc, 6) & " = " & Formatacao.FormataMoeda(Math.Round(valorAtual * saldoPc / 100, 2))

        gvParcelas.DataSource = parcelas
        gvParcelas.DataBind()

        ' contemplacao
        Dim cont As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT T.*, A.NR_ASSEMBLEIA FROM TB_CONTEMPLACAO T INNER JOIN TB_ASSEMBLEIA A ON A.CD_ASSEMBLEIA = T.CD_ASSEMBLEIA " & _
            " WHERE T.CD_COTA = " & cdCota & " ORDER BY T.CD_CONTEMPLACAO DESC")
        If cont.Rows.Count > 0 Then
            Dim t As DataRow = cont.Rows(0)
            Dim forma As String = "sorteio"
            If CInt(t("ID_TIPO")) = 2 Then forma = "lance de " & Formatacao.FormataPercentual(t("PC_LANCE"), 2)
            If CInt(t("ID_TIPO")) = 3 Then forma = "sorteio para devolução (multa rescisória " & Formatacao.FormataMoeda(t("VL_DESCONTO")) & ")"
            Dim texto As String = "Contemplada por <b>" & forma & _
                                  "</b> na AGO " & t("NR_ASSEMBLEIA") & " de " & Formatacao.FormataData(t("DT_CONTEMPLACAO")) & ". " & _
                                  "Crédito " & Formatacao.FormataMoeda(t("VL_CREDITO"))
            If CDec(t("VL_EMBUTIDO")) > 0 Then texto &= ", embutido " & Formatacao.FormataMoeda(t("VL_EMBUTIDO"))
            texto &= ", crédito líquido <b>" & Formatacao.FormataMoeda(t("VL_CREDITO_LIQUIDO")) & "</b>. "
            Select Case CInt(t("ID_SITUACAO"))
                Case 1 : texto &= "Aguardando liberação."
                Case 2 : texto &= "Liberado em " & Formatacao.FormataData(t("DT_LIBERACAO")) & " para " & Server.HtmlEncode(t("DS_FAVORECIDO").ToString()) & "."
                Case 3 : texto &= "Cancelada."
            End Select
            litContemplacao.Text = texto
            pnlContemplacao.Visible = True
        End If

        Dim lances As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT L.*, A.NR_ASSEMBLEIA, A.DT_ASSEMBLEIA, " & _
            "       CASE L.ID_SITUACAO WHEN 1 THEN 'Registrado' WHEN 2 THEN 'Vencedor' WHEN 3 THEN 'Não contemplado' WHEN 4 THEN 'Desclassificado' ELSE 'Cancelado' END AS DS_SITUACAO " & _
            "  FROM TB_LANCE L INNER JOIN TB_ASSEMBLEIA A ON A.CD_ASSEMBLEIA = L.CD_ASSEMBLEIA WHERE L.CD_COTA = " & cdCota & " ORDER BY A.NR_ASSEMBLEIA DESC")
        If lances.Rows.Count > 0 Then
            gvLances.DataSource = lances
            gvLances.DataBind()
            pnlLances.Visible = True
        End If
        If gvParcelas.FooterRow IsNot Nothing Then
            gvParcelas.FooterRow.Cells(0).Text = "Total"
            gvParcelas.FooterRow.Cells(2).Text = totFC.ToString("0.000000")
            gvParcelas.FooterRow.Cells(4).Text = totTA.ToString("0.000000")
            gvParcelas.FooterRow.Cells(5).Text = totFR.ToString("0.000000")
            gvParcelas.FooterRow.Cells(6).Text = (totFC + totTA + totFR).ToString("0.000000")
        End If
    End Sub

    Protected Sub btnDesistencia_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnDesistencia.Click
        Try
            AcessoDados.ExecutaProcedureRetorno("SP_DESISTE_COTA", _
                AcessoDados.Parametro("@CD_COTA", ViewState("CD_COTA")), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario), _
                AcessoDados.Parametro("@DT_DESISTENCIA", Date.Now), _
                AcessoDados.Parametro("@DS_MOTIVO", NuloSeVazio(txtMotivoDesistencia.Text)))
            Carrega(CInt(ViewState("CD_COTA")))
            MostraMensagem(lblMensagem, "Desistência registrada. A cota concorre aos sorteios para devolução.")
        Catch ex As System.Data.SqlClient.SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

End Class
