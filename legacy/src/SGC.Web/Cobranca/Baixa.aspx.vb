Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

'==============================================================================
' Baixa manual (pagamento no caixa da loja, deposito identificado, acerto).
' 2014-06 J.P.
'==============================================================================
Partial Class Cobranca_Baixa
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            If Request.QueryString("id") <> "" Then
                ViewState("CD_PARCELA") = CInt(Request.QueryString("id"))
                Carrega()
            End If
        End If
    End Sub

    Protected Sub btnBuscar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnBuscar.Click
        Dim cd As Object = AcessoDados.ExecutaEscalar("SELECT CD_PARCELA FROM TB_PARCELA WHERE NR_NOSSO_NUMERO = @NN", _
                                                      AcessoDados.Parametro("@NN", txtNossoNumero.Text.Trim()))
        If cd Is Nothing Then
            pnlParcela.Visible = False
            MostraMensagem(lblMensagem, "Nosso número não encontrado.", True)
            Exit Sub
        End If
        lblMensagem.Visible = False
        ViewState("CD_PARCELA") = CInt(cd)
        Carrega()
    End Sub

    Private Function Parcela() As DataRow
        Return AcessoDados.ExecutaDataTable( _
            "SELECT P.*, C.NR_COTA, C.NR_VERSAO, G.NR_GRUPO, X.NM_CONSORCIADO, C.QT_PRAZO_COTA, " & _
            "       dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) AS DT_LIMITE " & _
            "  FROM TB_PARCELA P INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA " & _
            " INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
            " WHERE P.CD_PARCELA = " & ViewState("CD_PARCELA")).Rows(0)
    End Function

    Private Sub Carrega()
        Dim p As DataRow = Parcela()
        pnlParcela.Visible = True
        lnkCota.Text = Formatacao.FormataGrupoCota(p("NR_GRUPO"), p("NR_COTA"), p("NR_VERSAO"))
        lnkCota.NavigateUrl = "~/Cotas/CotaDetalhe.aspx?id=" & p("CD_COTA")
        litConsorciado.Text = p("NM_CONSORCIADO").ToString()
        litParcela.Text = p("NR_PARCELA") & " de " & p("QT_PRAZO_COTA")
        litNossoNumero.Text = p("NR_NOSSO_NUMERO").ToString()
        litVencimento.Text = Formatacao.FormataData(p("DT_VENCIMENTO"))
        If CDate(p("DT_LIMITE")) <> CDate(p("DT_VENCIMENTO")) Then
            litVencimento.Text &= " (pagável sem encargos até " & Formatacao.FormataData(p("DT_LIMITE")) & ")"
        End If
        litValor.Text = Formatacao.FormataMoeda(p("VL_PARCELA"))
        litSituacao.Text = DescricaoSituacaoParcela(p("ID_SITUACAO"))

        Dim situacao As Integer = CInt(p("ID_SITUACAO"))
        If situacao = 3 Then
            litSituacao.Text &= " em " & Formatacao.FormataData(p("DT_PAGAMENTO")) & " - " & Formatacao.FormataMoeda(p("VL_PAGO"))
        End If
        pnlBaixa.Visible = (situacao = 2 And PodeGravar())
        If pnlBaixa.Visible And txtDataPagamento.Text = "" Then
            txtDataPagamento.Text = Date.Today.ToString("dd/MM/yyyy")
            Calcula()
        End If
    End Sub

    ' multa 2% e juros 1% a.m. pro rata (TB_PARAMETRO), apos o primeiro dia util depois do vencimento
    Private Function Calcula() As Boolean
        If Not Validacao.ValidaData(txtDataPagamento.Text) Then
            MostraMensagem(lblMensagem, "Data de pagamento inválida.", True)
            Return False
        End If

        Dim p As DataRow = Parcela()
        Dim pagamento As Date = Date.ParseExact(txtDataPagamento.Text, "dd/MM/yyyy", Nothing)
        Dim valor As Decimal = CDec(p("VL_PARCELA"))
        Dim multa As Decimal = 0
        Dim juros As Decimal = 0
        Dim dias As Integer = 0

        If pagamento > CDate(p("DT_LIMITE")) Then
            dias = DateDiff(DateInterval.Day, CDate(p("DT_VENCIMENTO")), pagamento)
            multa = Math.Round(valor * Parametros.LeDecimal("PC_MULTA_ATRASO") / 100, 2)
            juros = Math.Round(valor * Parametros.LeDecimal("PC_JUROS_MES") / 100 / 30 * dias, 2)
        End If

        ViewState("VL_MULTA") = multa
        ViewState("VL_JUROS") = juros
        litMulta.Text = Formatacao.FormataMoeda(multa)
        litJuros.Text = Formatacao.FormataMoeda(juros)
        litDias.Text = IIf(dias > 0, "(" & dias & " dias)", "")
        litDevido.Text = Formatacao.FormataMoeda(valor + multa + juros)
        txtValorPago.Text = (valor + multa + juros).ToString("N2")
        Return True
    End Function

    Protected Sub btnCalcular_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCalcular.Click
        Calcula()
    End Sub

    Protected Sub btnBaixar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnBaixar.Click
        Dim valorPago As String = txtValorPago.Text
        If Not Calcula() Then Exit Sub
        txtValorPago.Text = valorPago

        Dim pagamento As Date = Date.ParseExact(txtDataPagamento.Text, "dd/MM/yyyy", Nothing)
        If pagamento > Date.Today Then
            MostraMensagem(lblMensagem, "Data de pagamento no futuro.", True)
            Exit Sub
        End If

        Try
            AcessoDados.ExecutaProcedureRetorno("SP_BAIXA_PARCELA", _
                AcessoDados.Parametro("@CD_PARCELA", ViewState("CD_PARCELA")), _
                AcessoDados.Parametro("@DT_PAGAMENTO", pagamento), _
                AcessoDados.Parametro("@VL_PAGO", CDec(valorPago)), _
                AcessoDados.Parametro("@VL_MULTA", ViewState("VL_MULTA")), _
                AcessoDados.Parametro("@VL_JUROS", ViewState("VL_JUROS")), _
                AcessoDados.Parametro("@ID_ORIGEM", 1), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            Carrega()
            MostraMensagem(lblMensagem, "Parcela baixada.")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

End Class
