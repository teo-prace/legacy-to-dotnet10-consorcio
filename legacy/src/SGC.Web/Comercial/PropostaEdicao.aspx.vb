Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum
Imports SGC.Negocio

'==============================================================================
' Proposta de adesao: digitacao, liberacao para pagamento, efetivacao,
' cancelamento e arrependimento.
' 2011-06 M.S.
' 2013-03 J.P. grupo escolhido na proposta (prazo da cota depende do grupo)
'==============================================================================
Partial Class Comercial_PropostaEdicao
    Inherits PaginaBase

    Private ReadOnly Property CodigoProposta() As Integer
        Get
            If ViewState("CD_PROPOSTA") Is Nothing Then Return 0
            Return CInt(ViewState("CD_PROPOSTA"))
        End Get
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            If Request.QueryString("id") <> "" Then
                ViewState("CD_PROPOSTA") = CInt(Request.QueryString("id"))
                CarregaExistente()
            Else
                CarregaUF(ddlUF)
                ddlUF.SelectedValue = "RS"
                CarregaCombo(ddlPlano, "SELECT CD_PLANO, NM_PLANO FROM TB_PLANO WHERE FL_ATIVO = 'S' ORDER BY NM_PLANO", "NM_PLANO", "CD_PLANO", "(selecione)")
                CarregaCombo(ddlPontoVenda, "SELECT CD_PONTO_VENDA, NM_PONTO_VENDA FROM TB_PONTO_VENDA WHERE FL_ATIVO = 'S' ORDER BY NM_PONTO_VENDA", "NM_PONTO_VENDA", "CD_PONTO_VENDA", "(selecione)")
                btnSalvar.Enabled = PodeGravar()
            End If
        End If
    End Sub

#Region "Nova proposta"

    Protected Sub btnBuscarCPF_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnBuscarCPF.Click
        Dim cpf As String = Validacao.SomenteNumeros(txtCPF.Text)
        ViewState("CD_CONSORCIADO") = Nothing
        lblConsorciado.Text = ""
        pnlCadastroRapido.Visible = False

        If Not Validacao.ValidaCPF(cpf) Then
            MostraMensagem(lblMensagem, "CPF inválido.", True)
            Exit Sub
        End If
        lblMensagem.Visible = False

        Dim dt As DataTable = AcessoDados.ExecutaDataTable("SELECT CD_CONSORCIADO, NM_CONSORCIADO, FL_ATIVO FROM TB_CONSORCIADO WHERE NR_CPF = @CPF", _
                                                           AcessoDados.Parametro("@CPF", cpf))
        If dt.Rows.Count > 0 Then
            If dt.Rows(0)("FL_ATIVO").ToString() <> "S" Then
                MostraMensagem(lblMensagem, "Consorciado inativo.", True)
                Exit Sub
            End If
            ViewState("CD_CONSORCIADO") = dt.Rows(0)("CD_CONSORCIADO")
            lblConsorciado.Text = dt.Rows(0)("NM_CONSORCIADO").ToString()
        Else
            pnlCadastroRapido.Visible = True
        End If
    End Sub

    Protected Sub ddlPlano_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlPlano.SelectedIndexChanged
        ddlGrupo.Items.Clear()
        ddlBem.Items.Clear()
        If ddlPlano.SelectedValue = "" Then Exit Sub

        ' grupos do plano que aceitam adesao e tem vaga
        Dim dt As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT G.CD_GRUPO, G.NR_GRUPO, G.ID_SITUACAO, G.QT_COTAS_MAXIMO, G.QT_PRAZO_MESES - G.QT_ASSEMBLEIAS_REALIZADAS AS QT_PRAZO_RESTANTE, " & _
            "       (SELECT COUNT(*) FROM TB_COTA C WHERE C.CD_GRUPO = G.CD_GRUPO AND C.ID_SITUACAO <> 6) AS QT_COTAS " & _
            "  FROM TB_GRUPO G WHERE G.CD_PLANO = " & ddlPlano.SelectedValue & " AND G.ID_SITUACAO IN (1, 2) ORDER BY G.ID_SITUACAO, G.NR_GRUPO")

        ddlGrupo.Items.Add(New ListItem("(selecione)", ""))
        For Each r As DataRow In dt.Rows
            If CInt(r("QT_COTAS")) < CInt(r("QT_COTAS_MAXIMO")) Then
                ddlGrupo.Items.Add(New ListItem(r("NR_GRUPO") & " - " & DescricaoSituacaoGrupo(r("ID_SITUACAO")) & " - " & _
                                                r("QT_COTAS") & "/" & r("QT_COTAS_MAXIMO") & " cotas - prazo " & r("QT_PRAZO_RESTANTE"), _
                                                r("CD_GRUPO").ToString()))
            End If
        Next
        CalculaPrimeiraParcela()
    End Sub

    Protected Sub ddlGrupo_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlGrupo.SelectedIndexChanged
        ddlBem.Items.Clear()
        If ddlGrupo.SelectedValue <> "" Then
            CarregaCombo(ddlBem, "SELECT B.CD_BEM, B.DS_BEM FROM TB_BEM B INNER JOIN TB_GRUPO_BEM GB ON GB.CD_BEM = B.CD_BEM " & _
                                 " WHERE GB.CD_GRUPO = " & ddlGrupo.SelectedValue & " AND B.FL_ATIVO = 'S' ORDER BY B.VL_BEM", "DS_BEM", "CD_BEM", "(selecione)")
        End If
        CalculaPrimeiraParcela()
    End Sub

    Protected Sub ddlBem_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlBem.SelectedIndexChanged
        CalculaPrimeiraParcela()
    End Sub

    Protected Sub ddlPontoVenda_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlPontoVenda.SelectedIndexChanged
        ddlVendedor.Items.Clear()
        If ddlPontoVenda.SelectedValue <> "" Then
            CarregaCombo(ddlVendedor, "SELECT CD_VENDEDOR, NM_VENDEDOR FROM TB_VENDEDOR WHERE FL_ATIVO = 'S' AND CD_PONTO_VENDA = " & ddlPontoVenda.SelectedValue & _
                                      " ORDER BY NM_VENDEDOR", "NM_VENDEDOR", "CD_VENDEDOR", "(selecione)")
        End If
    End Sub

    ' devolve o valor da 1a parcela e preenche valor do bem / prazo
    Private Function CalculaPrimeiraParcela() As Decimal
        lblPrimeiraParcela.Text = "-"
        lblPrazoCota.Text = ""
        If ddlGrupo.SelectedValue = "" Or ddlBem.SelectedValue = "" Then Return 0

        Dim g As DataRow = AcessoDados.ExecutaDataTable("SELECT * FROM TB_GRUPO WHERE CD_GRUPO = " & ddlGrupo.SelectedValue).Rows(0)
        Dim valorBem As Decimal = CDec(AcessoDados.ExecutaEscalar("SELECT dbo.FN_VALOR_BEM(" & ddlBem.SelectedValue & ", GETDATE())"))
        Dim prazo As Integer = CInt(g("QT_PRAZO_MESES")) - CInt(g("QT_ASSEMBLEIAS_REALIZADAS"))

        Dim valor As Decimal = CalculoParcela.ValorPrimeiraParcela(valorBem, prazo, CDec(g("PC_TAXA_ADM")), _
                                                                   CDec(g("PC_ANTECIPACAO_TA")), CDec(g("PC_FUNDO_RESERVA")))
        ViewState("VL_BEM") = valorBem
        lblPrimeiraParcela.Text = Formatacao.FormataMoeda(valor)
        lblPrazoCota.Text = " (crédito " & Formatacao.FormataMoeda(valorBem) & ", prazo da cota " & prazo & " meses)"
        Return valor
    End Function

    Protected Sub btnSalvar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSalvar.Click
        If ViewState("CD_CONSORCIADO") Is Nothing And Not pnlCadastroRapido.Visible Then
            MostraMensagem(lblMensagem, "Busque o consorciado pelo CPF.", True)
            Exit Sub
        End If
        If ddlGrupo.SelectedValue = "" Or ddlBem.SelectedValue = "" Or ddlVendedor.SelectedValue = "" Then
            MostraMensagem(lblMensagem, "Selecione grupo, crédito, ponto de venda e vendedor.", True)
            Exit Sub
        End If

        Try
            ' cadastro rapido
            If ViewState("CD_CONSORCIADO") Is Nothing Then
                If txtNome.Text.Trim() = "" Then
                    MostraMensagem(lblMensagem, "Informe o nome do consorciado.", True)
                    Exit Sub
                End If
                Dim pCodigo As SqlParameter = AcessoDados.ParametroSaida("@CD_CONSORCIADO", SqlDbType.Int)
                AcessoDados.ExecutaProcedureRetorno("SP_INCLUI_CONSORCIADO", _
                    AcessoDados.Parametro("@NR_CPF", Validacao.SomenteNumeros(txtCPF.Text)), _
                    AcessoDados.Parametro("@NM_CONSORCIADO", txtNome.Text.Trim()), _
                    AcessoDados.Parametro("@DT_NASCIMENTO", DataOuNulo(txtNascimento.Text)), _
                    AcessoDados.Parametro("@DS_EMAIL", NuloSeVazio(txtEmail.Text)), _
                    AcessoDados.Parametro("@NR_CELULAR", NuloSeVazio(txtCelular.Text)), _
                    AcessoDados.Parametro("@NM_CIDADE", NuloSeVazio(txtCidade.Text.ToUpper())), _
                    AcessoDados.Parametro("@SG_UF", NuloSeVazio(ddlUF.SelectedValue)), _
                    AcessoDados.Parametro("@VL_RENDA_MENSAL", DecimalOuNulo(txtRenda.Text)), _
                    AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario), _
                    pCodigo)
                ViewState("CD_CONSORCIADO") = pCodigo.Value
            End If

            Dim valorParcela As Decimal = CalculaPrimeiraParcela()
            Dim ano As String = Date.Today.Year.ToString()
            Dim seq As Integer = CInt(AcessoDados.ExecutaEscalar("SELECT COUNT(*) + 1 FROM TB_PROPOSTA WHERE NR_PROPOSTA LIKE '" & ano & "-%'"))
            Dim numero As String = ano & "-" & seq.ToString("000000")

            Dim novo As Object = AcessoDados.ExecutaEscalar( _
                "INSERT INTO TB_PROPOSTA (NR_PROPOSTA, CD_CONSORCIADO, CD_PLANO, CD_GRUPO, CD_BEM, CD_PONTO_VENDA, CD_VENDEDOR, DT_PROPOSTA, " & _
                " VL_BEM_PROPOSTA, VL_PRIMEIRA_PARCELA, ID_SITUACAO, DT_SITUACAO, DS_OBSERVACAO, CD_USUARIO_CADASTRO) " & _
                "VALUES (@NR, @CONS, @PLANO, @GRUPO, @BEM, @PV, @VEND, GETDATE(), @VLBEM, @VL1, 1, GETDATE(), @OBS, @USU); SELECT @@IDENTITY", _
                AcessoDados.Parametro("@NR", numero), _
                AcessoDados.Parametro("@CONS", ViewState("CD_CONSORCIADO")), _
                AcessoDados.Parametro("@PLANO", ddlPlano.SelectedValue), _
                AcessoDados.Parametro("@GRUPO", ddlGrupo.SelectedValue), _
                AcessoDados.Parametro("@BEM", ddlBem.SelectedValue), _
                AcessoDados.Parametro("@PV", ddlPontoVenda.SelectedValue), _
                AcessoDados.Parametro("@VEND", ddlVendedor.SelectedValue), _
                AcessoDados.Parametro("@VLBEM", ViewState("VL_BEM")), _
                AcessoDados.Parametro("@VL1", valorParcela), _
                AcessoDados.Parametro("@OBS", NuloSeVazio(txtObservacao.Text)), _
                AcessoDados.Parametro("@USU", CodigoUsuario))

            Log.Grava("Proposta " & numero & " incluida por " & UsuarioLogado("DS_LOGIN"))
            Response.Redirect("PropostaEdicao.aspx?id=" & CInt(novo) & "&nova=1", False)
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        Catch ex As Exception
            MostraMensagem(lblMensagem, "Erro ao gravar a proposta.", True)
        End Try
    End Sub

    Protected Sub btnVoltarNova_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnVoltarNova.Click
        Response.Redirect("Propostas.aspx")
    End Sub

#End Region

#Region "Proposta existente"

    Private Sub CarregaExistente()
        pnlNova.Visible = False
        pnlExistente.Visible = True

        Dim dt As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT P.*, C.NM_CONSORCIADO, C.NR_CPF, PL.NM_PLANO, G.NR_GRUPO, B.DS_BEM, PV.NM_PONTO_VENDA, V.NM_VENDEDOR, " & _
            "       T.NR_COTA, T.NR_VERSAO " & _
            "  FROM TB_PROPOSTA P " & _
            " INNER JOIN TB_CONSORCIADO C ON C.CD_CONSORCIADO = P.CD_CONSORCIADO " & _
            " INNER JOIN TB_PLANO PL ON PL.CD_PLANO = P.CD_PLANO " & _
            " INNER JOIN TB_GRUPO G ON G.CD_GRUPO = P.CD_GRUPO " & _
            " INNER JOIN TB_BEM B ON B.CD_BEM = P.CD_BEM " & _
            " INNER JOIN TB_PONTO_VENDA PV ON PV.CD_PONTO_VENDA = P.CD_PONTO_VENDA " & _
            " INNER JOIN TB_VENDEDOR V ON V.CD_VENDEDOR = P.CD_VENDEDOR " & _
            "  LEFT JOIN TB_COTA T ON T.CD_COTA = P.CD_COTA " & _
            " WHERE P.CD_PROPOSTA = " & CodigoProposta)

        If dt.Rows.Count = 0 Then
            MostraMensagem(lblMensagem, "Proposta não encontrada.", True)
            pnlExistente.Visible = False
            Exit Sub
        End If

        Dim r As DataRow = dt.Rows(0)
        Dim situacao As Integer = CInt(r("ID_SITUACAO"))

        litTitulo.Text = "Proposta " & r("NR_PROPOSTA")
        litNumero.Text = r("NR_PROPOSTA").ToString()
        litSituacao.Text = DescricaoSituacaoProposta(situacao)
        lnkConsorciado.Text = r("NM_CONSORCIADO").ToString()
        lnkConsorciado.NavigateUrl = "~/Cadastro/ConsorciadoEdicao.aspx?id=" & r("CD_CONSORCIADO")
        litCPF.Text = Formatacao.FormataCPF(r("NR_CPF"))
        litPlano.Text = r("NM_PLANO").ToString()
        lnkGrupo.Text = r("NR_GRUPO").ToString()
        lnkGrupo.NavigateUrl = "~/Grupos/GrupoEdicao.aspx?id=" & r("CD_GRUPO")
        litBem.Text = r("DS_BEM").ToString()
        litValorBem.Text = Formatacao.FormataMoeda(r("VL_BEM_PROPOSTA"))
        litPrimeira.Text = Formatacao.FormataMoeda(r("VL_PRIMEIRA_PARCELA"))
        litVenda.Text = r("NM_PONTO_VENDA") & " / " & r("NM_VENDEDOR")
        litData.Text = CDate(r("DT_PROPOSTA")).ToString("dd/MM/yyyy HH:mm")
        litAssinatura.Text = Formatacao.FormataData(r("DT_ASSINATURA"))
        litEfetivacao.Text = Formatacao.FormataData(r("DT_EFETIVACAO"))
        litObservacao.Text = Server.HtmlEncode(r("DS_OBSERVACAO").ToString())
        If Not IsDBNull(r("CD_COTA")) Then
            lnkCota.Text = Formatacao.FormataGrupoCota(r("NR_GRUPO"), r("NR_COTA"), r("NR_VERSAO"))
            lnkCota.NavigateUrl = "~/Cotas/CotaDetalhe.aspx?id=" & r("CD_COTA")
        End If

        Dim grava As Boolean = PodeGravar()
        pnlDigitada.Visible = (situacao = 1 And grava)
        pnlPagamento.Visible = (situacao = 2 And grava)
        btnCancelar.Visible = ((situacao = 1 Or situacao = 2) And grava)
        btnArrependimento.Visible = (situacao = 3 And grava)

        If situacao = 2 Then
            txtDataPagamento.Text = Date.Today.ToString("dd/MM/yyyy")
            txtValorPago.Text = CDec(r("VL_PRIMEIRA_PARCELA")).ToString("N2")
        End If

        If Request.QueryString("nova") = "1" And Not IsPostBack Then
            MostraMensagem(lblMensagem, "Proposta incluída. Após a assinatura, libere para pagamento.")
        End If
    End Sub

    Protected Sub btnEnviarPagamento_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnEnviarPagamento.Click
        AcessoDados.ExecutaNonQuery("UPDATE TB_PROPOSTA SET ID_SITUACAO = 2, DT_SITUACAO = GETDATE(), DT_ASSINATURA = GETDATE() " & _
                                    " WHERE CD_PROPOSTA = " & CodigoProposta & " AND ID_SITUACAO = 1")
        AcessoDados.ExecutaNonQuery("EXEC SP_GRAVA_LOG " & CodigoUsuario & ", 'PROPOSTA_ASSINADA', '" & CodigoProposta & "'")
        CarregaExistente()
        MostraMensagem(lblMensagem, "Proposta liberada para pagamento da 1ª parcela.")
    End Sub

    Protected Sub btnConfirmarPagamento_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnConfirmarPagamento.Click
        If Not Validacao.ValidaData(txtDataPagamento.Text) Then
            MostraMensagem(lblMensagem, "Data de pagamento inválida.", True)
            Exit Sub
        End If
        Dim dataPagamento As Date = Date.ParseExact(txtDataPagamento.Text, "dd/MM/yyyy", Nothing)
        If dataPagamento > Date.Today Then
            MostraMensagem(lblMensagem, "Data de pagamento no futuro.", True)
            Exit Sub
        End If

        Try
            Dim pCota As SqlParameter = AcessoDados.ParametroSaida("@CD_COTA", SqlDbType.Int)
            AcessoDados.ExecutaProcedureRetorno("SP_EFETIVA_PROPOSTA", _
                AcessoDados.Parametro("@CD_PROPOSTA", CodigoProposta), _
                AcessoDados.Parametro("@DT_PAGAMENTO", dataPagamento), _
                AcessoDados.Parametro("@VL_PAGO", CDec(txtValorPago.Text)), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario), _
                pCota)
            CarregaExistente()
            MostraMensagem(lblMensagem, "Proposta efetivada. Cota " & lnkCota.Text & ".")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

    Protected Sub btnCancelar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCancelar.Click
        AcessoDados.ExecutaNonQuery("UPDATE TB_PROPOSTA SET ID_SITUACAO = 4, DT_SITUACAO = GETDATE() WHERE CD_PROPOSTA = " & CodigoProposta & " AND ID_SITUACAO IN (1, 2)")
        AcessoDados.ExecutaNonQuery("EXEC SP_GRAVA_LOG " & CodigoUsuario & ", 'PROPOSTA_CANCELADA', '" & CodigoProposta & "'")
        CarregaExistente()
        MostraMensagem(lblMensagem, "Proposta cancelada.")
    End Sub

    Protected Sub btnArrependimento_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArrependimento.Click
        Try
            AcessoDados.ExecutaProcedureRetorno("SP_ARREPENDIMENTO_PROPOSTA", _
                AcessoDados.Parametro("@CD_PROPOSTA", CodigoProposta), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            CarregaExistente()
            MostraMensagem(lblMensagem, "Arrependimento registrado. Cota cancelada e valores estornados no caixa do grupo.")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

    Protected Sub btnVoltar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnVoltar.Click
        Response.Redirect("Propostas.aspx")
    End Sub

#End Region

End Class
