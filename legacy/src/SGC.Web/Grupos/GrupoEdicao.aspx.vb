Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

Partial Class Grupos_GrupoEdicao
    Inherits PaginaBase

    Private ReadOnly Property CodigoGrupo() As Integer
        Get
            If ViewState("CD_GRUPO") Is Nothing Then Return 0
            Return CInt(ViewState("CD_GRUPO"))
        End Get
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            If Request.QueryString("id") <> "" Then
                ViewState("CD_GRUPO") = CInt(Request.QueryString("id"))
                CarregaExistente()
            Else
                CarregaCombo(ddlPlano, "SELECT CD_PLANO, NM_PLANO FROM TB_PLANO WHERE FL_ATIVO = 'S' ORDER BY NM_PLANO", "NM_PLANO", "CD_PLANO", "(selecione)")
                txtAbertura.Text = Date.Today.ToString("dd/MM/yyyy")
                btnCriar.Enabled = PodeGravar()
            End If
        End If
    End Sub

    ' numero sugerido: tipo de bem + sequencial (ex.: 2 -> 002088)
    Protected Sub ddlPlano_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlPlano.SelectedIndexChanged
        If ddlPlano.SelectedValue = "" Then Exit Sub
        Dim tipo As Integer = CInt(AcessoDados.ExecutaEscalar("SELECT ID_TIPO_BEM FROM TB_PLANO WHERE CD_PLANO = " & ddlPlano.SelectedValue))
        Dim ultimo As Object = AcessoDados.ExecutaEscalar("SELECT MAX(CAST(NR_GRUPO AS INT)) FROM TB_GRUPO WHERE NR_GRUPO LIKE '00" & tipo & "%'")
        If IsDBNull(ultimo) Then
            txtNumero.Text = (tipo * 1000 + 1).ToString("000000")
        Else
            txtNumero.Text = (CInt(ultimo) + 1).ToString("000000")
        End If
    End Sub

    Protected Sub btnCriar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCriar.Click
        If ddlPlano.SelectedValue = "" Or txtNumero.Text.Trim() = "" Or Not Validacao.ValidaData(txtAbertura.Text) Then
            MostraMensagem(lblMensagem, "Informe plano, número e data de abertura.", True)
            Exit Sub
        End If

        Dim numero As String = CInt(txtNumero.Text).ToString("000000")
        Dim abertura As Date = Date.ParseExact(txtAbertura.Text, "dd/MM/yyyy", Nothing)

        If CInt(AcessoDados.ExecutaEscalar("SELECT COUNT(*) FROM TB_GRUPO WHERE NR_GRUPO = '" & numero & "'")) > 0 Then
            MostraMensagem(lblMensagem, "Já existe o grupo " & numero & ".", True)
            Exit Sub
        End If

        Try
            Dim novo As Object = AcessoDados.ExecutaEscalar( _
                "INSERT INTO TB_GRUPO (NR_GRUPO, CD_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA, " & _
                "       PC_LANCE_EMBUTIDO_MAX, PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO, NR_DIA_ASSEMBLEIA, CD_INDICE, " & _
                "       DS_VERSAO_REGULAMENTO, DT_ABERTURA, ID_SITUACAO, DT_SITUACAO, CD_USUARIO_CADASTRO) " & _
                "SELECT @NR, CD_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA, " & _
                "       PC_LANCE_EMBUTIDO_MAX, PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO, NR_DIA_ASSEMBLEIA, CD_INDICE, " & _
                "       DS_VERSAO_REGULAMENTO, @ABERTURA, 1, GETDATE(), @USU FROM TB_PLANO WHERE CD_PLANO = @PLANO; SELECT @@IDENTITY", _
                AcessoDados.Parametro("@NR", numero), _
                AcessoDados.Parametro("@ABERTURA", abertura), _
                AcessoDados.Parametro("@USU", CodigoUsuario), _
                AcessoDados.Parametro("@PLANO", ddlPlano.SelectedValue))

            ViewState("CD_GRUPO") = CInt(novo)
            AcessoDados.ExecutaNonQuery("INSERT INTO TB_GRUPO_BEM (CD_GRUPO, CD_BEM) SELECT " & CodigoGrupo & ", CD_BEM FROM TB_PLANO_BEM WHERE CD_PLANO = " & ddlPlano.SelectedValue)
            AcessoDados.ExecutaNonQuery("EXEC SP_GRAVA_LOG " & CodigoUsuario & ", 'GRUPO_CRIADO', '" & numero & "'")

            Response.Redirect("GrupoEdicao.aspx?id=" & CodigoGrupo, False)
        Catch ex As Exception
            MostraMensagem(lblMensagem, "Erro ao criar o grupo.", True)
        End Try
    End Sub

    Protected Sub btnVoltarNovo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnVoltarNovo.Click
        Response.Redirect("Grupos.aspx")
    End Sub

    Private Sub CarregaExistente()
        pnlNovo.Visible = False
        pnlExistente.Visible = True

        Dim dt As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT G.*, P.NM_PLANO, I.SG_INDICE, " & _
            "       (SELECT COUNT(*) FROM TB_COTA C WHERE C.CD_GRUPO = G.CD_GRUPO AND C.ID_SITUACAO = 1) AS QT_ATIVAS " & _
            "  FROM TB_GRUPO G INNER JOIN TB_PLANO P ON P.CD_PLANO = G.CD_PLANO INNER JOIN TB_INDICE I ON I.CD_INDICE = G.CD_INDICE " & _
            " WHERE G.CD_GRUPO = " & CodigoGrupo)
        If dt.Rows.Count = 0 Then Exit Sub
        Dim g As DataRow = dt.Rows(0)

        litTitulo.Text = "Grupo " & g("NR_GRUPO")
        lnkPlano.Text = g("NM_PLANO").ToString()
        lnkPlano.NavigateUrl = "~/Comercial/PlanoEdicao.aspx?id=" & g("CD_PLANO")
        litSituacao.Text = DescricaoSituacaoGrupo(g("ID_SITUACAO"))
        litTipo.Text = DescricaoTipoBem(g("ID_TIPO_BEM"))
        litPrazo.Text = g("QT_PRAZO_MESES") & " meses (" & g("QT_ASSEMBLEIAS_REALIZADAS") & " assembleias realizadas)"
        litTaxa.Text = Formatacao.FormataPercentual(g("PC_TAXA_ADM"), 2)
        If CDec(g("PC_ANTECIPACAO_TA")) > 0 Then litTaxa.Text &= " (" & Formatacao.FormataPercentual(g("PC_ANTECIPACAO_TA"), 2) & " antecipada)"
        litFundo.Text = Formatacao.FormataPercentual(g("PC_FUNDO_RESERVA"), 2)
        litEmbutido.Text = Formatacao.FormataPercentual(g("PC_LANCE_EMBUTIDO_MAX"), 2)
        litLanceFixo.Text = IIf(IsDBNull(g("PC_LANCE_FIXO")), "não tem", Formatacao.FormataPercentual(g("PC_LANCE_FIXO"), 2))
        litCotas.Text = g("QT_ATIVAS") & " / " & g("QT_COTAS_MINIMO") & " / " & g("QT_COTAS_MAXIMO")
        litDias.Text = g("NR_DIA_VENCIMENTO") & " / " & g("NR_DIA_ASSEMBLEIA")
        litAbertura.Text = Formatacao.FormataData(g("DT_ABERTURA"))
        litConstituicao.Text = IIf(IsDBNull(g("DT_CONSTITUICAO")), "-", Formatacao.FormataData(g("DT_CONSTITUICAO")))
        litRegulamento.Text = g("SG_INDICE") & " / " & g("DS_VERSAO_REGULAMENTO")

        Dim bens As DataTable = AcessoDados.ExecutaDataTable("SELECT B.DS_BEM FROM TB_BEM B INNER JOIN TB_GRUPO_BEM GB ON GB.CD_BEM = B.CD_BEM WHERE GB.CD_GRUPO = " & CodigoGrupo & " ORDER BY B.VL_BEM")
        Dim lista As New List(Of String)
        For Each b As DataRow In bens.Rows
            lista.Add(b("DS_BEM").ToString())
        Next
        litBens.Text = String.Join("; ", lista.ToArray())

        ' constituicao
        If CInt(g("ID_SITUACAO")) = 1 Then
            pnlConstituir.Visible = True
            Dim ativas As Integer = CInt(g("QT_ATIVAS"))
            Dim minimo As Integer = CInt(g("QT_COTAS_MINIMO"))
            If ativas >= minimo Then
                litQuorum.Text = "<span style='color:#1b5e20'>Quórum atingido: " & ativas & " cotas (mínimo " & minimo & ").</span>"
                btnConstituir.Enabled = PodeGravar()
            Else
                litQuorum.Text = "Quórum não atingido: " & ativas & " de " & minimo & " cotas."
                btnConstituir.Enabled = False
            End If
            ' sugere a proxima data de assembleia
            Dim dia As Integer = CInt(g("NR_DIA_ASSEMBLEIA"))
            Dim prox As New Date(Date.Today.Year, Date.Today.Month, Math.Min(dia, 28))
            If prox <= Date.Today Then prox = prox.AddMonths(1)
            txtPrimeiraAGO.Text = prox.ToString("dd/MM/yyyy")
        Else
            pnlConstituir.Visible = False
        End If

        gvSaldos.DataSource = AcessoDados.ExecutaProcedure("SP_SALDO_GRUPO", AcessoDados.Parametro("@CD_GRUPO", CodigoGrupo))
        gvSaldos.DataBind()

        lnkMapa.NavigateUrl = "MapaCotas.aspx?id=" & CodigoGrupo
        lnkExtrato.NavigateUrl = "Extrato.aspx?id=" & CodigoGrupo
    End Sub

    Protected Sub btnConstituir_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnConstituir.Click
        If Not Validacao.ValidaData(txtPrimeiraAGO.Text) Then
            MostraMensagem(lblMensagem, "Data da 1ª assembleia inválida.", True)
            Exit Sub
        End If
        Try
            AcessoDados.ExecutaProcedureRetorno("SP_CONSTITUI_GRUPO", _
                AcessoDados.Parametro("@CD_GRUPO", CodigoGrupo), _
                AcessoDados.Parametro("@DT_PRIMEIRA_AGO", Date.ParseExact(txtPrimeiraAGO.Text, "dd/MM/yyyy", Nothing)), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            CarregaExistente()
            MostraMensagem(lblMensagem, "Grupo constituído. Vencimentos gerados.")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

End Class
