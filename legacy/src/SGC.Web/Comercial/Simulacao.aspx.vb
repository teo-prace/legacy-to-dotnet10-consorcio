Imports System.Data
Imports SGC.Comum
Imports SGC.Negocio

Partial Class Comercial_Simulacao
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaCombo(ddlPlano, "SELECT CD_PLANO, NM_PLANO FROM TB_PLANO WHERE FL_ATIVO = 'S' ORDER BY NM_PLANO", "NM_PLANO", "CD_PLANO")
            CarregaBens()
        End If
    End Sub

    Private Sub CarregaBens()
        If ddlPlano.SelectedValue = "" Then Exit Sub
        CarregaCombo(ddlBem, "SELECT B.CD_BEM, B.DS_BEM + ' - ' + REPLACE(CONVERT(VARCHAR, B.VL_BEM, 1), '.00', '') AS DS " & _
                             "  FROM TB_BEM B INNER JOIN TB_PLANO_BEM PB ON PB.CD_BEM = B.CD_BEM " & _
                             " WHERE PB.CD_PLANO = " & ddlPlano.SelectedValue & " AND B.FL_ATIVO = 'S' ORDER BY B.VL_BEM", "DS", "CD_BEM")
    End Sub

    Protected Sub ddlPlano_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlPlano.SelectedIndexChanged
        CarregaBens()
        pnlResultado.Visible = False
    End Sub

    Protected Sub btnSimular_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSimular.Click
        If ddlPlano.SelectedValue = "" Or ddlBem.SelectedValue = "" Then
            MostraMensagem(lblMensagem, "Selecione plano e crédito.", True)
            Exit Sub
        End If

        Dim plano As DataRow = AcessoDados.ExecutaDataTable("SELECT * FROM TB_PLANO WHERE CD_PLANO = " & ddlPlano.SelectedValue).Rows(0)
        Dim valorBem As Decimal = CDec(AcessoDados.ExecutaEscalar("SELECT VL_BEM FROM TB_BEM WHERE CD_BEM = " & ddlBem.SelectedValue))

        Dim prazo As Integer = CInt(plano("QT_PRAZO_MESES"))
        Dim taxa As Decimal = CDec(plano("PC_TAXA_ADM"))
        Dim antecipacao As Decimal = CDec(plano("PC_ANTECIPACAO_TA"))
        Dim fundo As Decimal = CDec(plano("PC_FUNDO_RESERVA"))

        Dim parcelas As List(Of ParcelaSimulada) = CalculoParcela.Simula(valorBem, prazo, taxa, antecipacao, fundo)

        litValorBem.Text = Formatacao.FormataMoeda(valorBem)
        litPrazo.Text = prazo.ToString()
        litTaxa.Text = Formatacao.FormataPercentual(taxa, 2) & IIf(antecipacao > 0, " (" & Formatacao.FormataPercentual(antecipacao, 2) & " antecipada na 1ª parcela)", "")
        litFundo.Text = Formatacao.FormataPercentual(fundo, 2)
        litPrimeira.Text = Formatacao.FormataMoeda(parcelas(0).Valor)
        If parcelas.Count > 1 Then litDemais.Text = Formatacao.FormataMoeda(parcelas(1).Valor)

        gvParcelas.DataSource = parcelas
        gvParcelas.DataBind()

        Dim totFC As Decimal = 0, totTA As Decimal = 0, totFR As Decimal = 0, totValor As Decimal = 0
        For Each p As ParcelaSimulada In parcelas
            totFC += p.PercentualFundoComum
            totTA += p.PercentualTaxaAdm
            totFR += p.PercentualFundoReserva
            totValor += p.Valor
        Next
        gvParcelas.FooterRow.Cells(0).Text = "Total"
        gvParcelas.FooterRow.Cells(1).Text = totFC.ToString("0.000000")
        gvParcelas.FooterRow.Cells(2).Text = totTA.ToString("0.000000")
        gvParcelas.FooterRow.Cells(3).Text = totFR.ToString("0.000000")
        gvParcelas.FooterRow.Cells(4).Text = (totFC + totTA + totFR).ToString("0.000000")
        gvParcelas.FooterRow.Cells(5).Text = totValor.ToString("N2")

        pnlResultado.Visible = True
    End Sub

End Class
