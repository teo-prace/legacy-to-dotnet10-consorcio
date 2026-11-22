Imports System.Data
Imports System.Text
Imports SGC.Comum
Imports SGC.Negocio

Partial Class Cobranca_Boleto
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim dt As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT P.*, C.NR_COTA, C.NR_VERSAO, C.QT_PRAZO_COTA, G.NR_GRUPO, X.NM_CONSORCIADO, X.NR_CPF, " & _
            "       X.DS_LOGRADOURO, X.NR_ENDERECO, X.DS_COMPLEMENTO, X.NM_BAIRRO, X.NM_CIDADE, X.SG_UF, X.NR_CEP " & _
            "  FROM TB_PARCELA P INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA " & _
            " INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
            " WHERE P.CD_PARCELA = " & CInt(Request.QueryString("id")))

        If dt.Rows.Count = 0 OrElse CInt(dt.Rows(0)("ID_SITUACAO")) <> 2 Then
            pnlBoleto.Visible = False
            pnlErro.Visible = True
            litErro.Text = "Parcela não encontrada ou não está emitida."
            Exit Sub
        End If

        Dim p As DataRow = dt.Rows(0)
        Dim agencia As String = Parametros.Le("NR_AGENCIA")
        Dim conta As String = Parametros.Le("NR_CONTA_CEDENTE")
        Dim carteira As String = Parametros.Le("NR_CARTEIRA")
        Dim cedente As String = Parametros.Le("NM_ADMINISTRADORA")
        Dim nosso As String = p("NR_NOSSO_NUMERO").ToString()
        Dim venc As Date = CDate(p("DT_VENCIMENTO"))
        Dim valor As Decimal = CDec(p("VL_PARCELA"))

        Dim codigo As String = Boleto.CodigoBarras(venc, valor, agencia, carteira, nosso, conta)
        Dim linha As String = Boleto.LinhaDigitavel(codigo)
        Dim cota As String = Formatacao.FormataGrupoCota(p("NR_GRUPO"), p("NR_COTA"), p("NR_VERSAO"))
        Dim sacado As String = p("NM_CONSORCIADO") & " - CPF " & Formatacao.FormataCPF(p("NR_CPF"))
        Dim nossoFormatado As String = carteira & "/" & nosso.Substring(0, 10) & "-" & nosso.Substring(10, 1)

        litLinha.Text = linha
        litLinha1.Text = linha
        litCedente.Text = cedente
        litCedente1.Text = cedente
        litVencimento.Text = venc.ToString("dd/MM/yyyy")
        litVenc1.Text = litVencimento.Text
        litAgencia.Text = agencia & " / " & conta
        litEmissao.Text = Formatacao.FormataData(p("DT_EMISSAO"))
        litDocumento.Text = p("NR_GRUPO") & p("NR_COTA").ToString().PadLeft(3, "0"c) & p("NR_PARCELA").ToString().PadLeft(3, "0"c)
        litCarteira.Text = carteira
        litNosso.Text = nossoFormatado
        litNosso1.Text = nossoFormatado
        litValor.Text = valor.ToString("N2")
        litValor1.Text = litValor.Text
        litMulta.Text = Parametros.Le("PC_MULTA_ATRASO").Replace(".", ",")
        litJuros.Text = Parametros.Le("PC_JUROS_MES").Replace(".", ",")
        litParcela.Text = p("NR_PARCELA") & "/" & p("QT_PRAZO_COTA")
        litCota.Text = cota
        litCota1.Text = cota & " - " & p("NR_PARCELA") & "/" & p("QT_PRAZO_COTA")
        litSacado.Text = Server.HtmlEncode(sacado)
        litSacado1.Text = litSacado.Text
        litEndereco.Text = Server.HtmlEncode(p("DS_LOGRADOURO") & ", " & p("NR_ENDERECO") & " " & p("DS_COMPLEMENTO") & " - " & _
                                             p("NM_BAIRRO") & " - " & p("NM_CIDADE") & "/" & p("SG_UF") & " - CEP " & Formatacao.FormataCEP(p("NR_CEP")))

        ' barras I2of5
        Dim barras As String = Boleto.BarrasI25(codigo)
        Dim sb As New StringBuilder
        For i As Integer = 0 To barras.Length - 1
            sb.AppendFormat("<span class='{0} {1}'></span>", IIf(i Mod 2 = 0, "p", "b"), barras(i))
        Next
        litBarras.Text = sb.ToString()
    End Sub

End Class
