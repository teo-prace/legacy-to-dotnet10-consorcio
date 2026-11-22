Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

Partial Class Comercial_PlanoEdicao
    Inherits PaginaBase

    Private ReadOnly Property CodigoPlano() As Integer
        Get
            If ViewState("CD_PLANO") Is Nothing Then Return 0
            Return CInt(ViewState("CD_PLANO"))
        End Get
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaCombo(ddlIndice, "SELECT CD_INDICE, SG_INDICE FROM TB_INDICE ORDER BY CD_INDICE", "SG_INDICE", "CD_INDICE")
            If Request.QueryString("id") <> "" Then
                ViewState("CD_PLANO") = CInt(Request.QueryString("id"))
                Carrega()
            Else
                txtDiaVencimento.Text = "10"
                txtDiaAssembleia.Text = "20"
                CarregaBens()
            End If
            btnGravar.Enabled = PodeGravar()
        End If
    End Sub

    Private Sub CarregaBens()
        cblBens.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT CD_BEM, DS_BEM FROM TB_BEM WHERE FL_ATIVO = 'S' AND ID_TIPO_BEM = " & ddlTipo.SelectedValue & " ORDER BY VL_BEM")
        cblBens.DataTextField = "DS_BEM"
        cblBens.DataValueField = "CD_BEM"
        cblBens.DataBind()
    End Sub

    Private Sub Carrega()
        Dim dt As DataTable = AcessoDados.ExecutaDataTable("SELECT * FROM TB_PLANO WHERE CD_PLANO = " & CodigoPlano)
        If dt.Rows.Count = 0 Then Exit Sub
        Dim r As DataRow = dt.Rows(0)

        litTitulo.Text = "Plano: " & r("NM_PLANO")
        txtNome.Text = r("NM_PLANO").ToString()
        ddlTipo.SelectedValue = r("ID_TIPO_BEM").ToString()
        txtPrazo.Text = r("QT_PRAZO_MESES").ToString()
        txtTaxaAdm.Text = CDec(r("PC_TAXA_ADM")).ToString("0.00")
        txtAntecipacao.Text = CDec(r("PC_ANTECIPACAO_TA")).ToString("0.00")
        txtFundoReserva.Text = CDec(r("PC_FUNDO_RESERVA")).ToString("0.00")
        txtEmbutido.Text = CDec(r("PC_LANCE_EMBUTIDO_MAX")).ToString("0.00")
        If Not IsDBNull(r("PC_LANCE_FIXO")) Then txtLanceFixo.Text = CDec(r("PC_LANCE_FIXO")).ToString("0.00")
        txtMinimo.Text = r("QT_COTAS_MINIMO").ToString()
        txtMaximo.Text = r("QT_COTAS_MAXIMO").ToString()
        txtDiaVencimento.Text = r("NR_DIA_VENCIMENTO").ToString()
        txtDiaAssembleia.Text = r("NR_DIA_ASSEMBLEIA").ToString()
        ddlIndice.SelectedValue = r("CD_INDICE").ToString()
        txtRegulamento.Text = r("DS_VERSAO_REGULAMENTO").ToString()
        chkAtivo.Checked = (r("FL_ATIVO").ToString() = "S")

        CarregaBens()
        Dim bens As DataTable = AcessoDados.ExecutaDataTable("SELECT CD_BEM FROM TB_PLANO_BEM WHERE CD_PLANO = " & CodigoPlano)
        For Each b As DataRow In bens.Rows
            Dim item As ListItem = cblBens.Items.FindByValue(b("CD_BEM").ToString())
            If item IsNot Nothing Then item.Selected = True
        Next

        gvGrupos.DataSource = AcessoDados.ExecutaDataTable("SELECT * FROM TB_GRUPO WHERE CD_PLANO = " & CodigoPlano & " ORDER BY NR_GRUPO")
        gvGrupos.DataBind()
        pnlGrupos.Visible = True
    End Sub

    Protected Sub ddlTipo_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlTipo.SelectedIndexChanged
        CarregaBens()
    End Sub

    Protected Sub btnGravar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnGravar.Click
        If txtNome.Text.Trim() = "" Or txtPrazo.Text = "" Or txtTaxaAdm.Text = "" Or txtFundoReserva.Text = "" Or _
           txtMinimo.Text = "" Or txtMaximo.Text = "" Or txtRegulamento.Text.Trim() = "" Then
            MostraMensagem(lblMensagem, "Preencha todos os campos obrigatórios.", True)
            Exit Sub
        End If
        If CInt(txtDiaVencimento.Text) < 1 Or CInt(txtDiaVencimento.Text) > 28 Then
            MostraMensagem(lblMensagem, "Dia de vencimento deve ser de 1 a 28.", True)
            Exit Sub
        End If
        If CInt(txtMinimo.Text) > CInt(txtMaximo.Text) Then
            MostraMensagem(lblMensagem, "Mínimo de cotas maior que o máximo.", True)
            Exit Sub
        End If

        Try
            Dim p() As SqlParameter = { _
                AcessoDados.Parametro("@NM", txtNome.Text.Trim().ToUpper()), _
                AcessoDados.Parametro("@TIPO", ddlTipo.SelectedValue), _
                AcessoDados.Parametro("@PRAZO", CInt(txtPrazo.Text)), _
                AcessoDados.Parametro("@TA", CDec(txtTaxaAdm.Text)), _
                AcessoDados.Parametro("@ANT", IIf(txtAntecipacao.Text = "", 0, CDec(txtAntecipacao.Text))), _
                AcessoDados.Parametro("@FR", CDec(txtFundoReserva.Text)), _
                AcessoDados.Parametro("@EMB", IIf(txtEmbutido.Text = "", 0, CDec(txtEmbutido.Text))), _
                AcessoDados.Parametro("@FIXO", DecimalOuNulo(txtLanceFixo.Text)), _
                AcessoDados.Parametro("@MIN", CInt(txtMinimo.Text)), _
                AcessoDados.Parametro("@MAX", CInt(txtMaximo.Text)), _
                AcessoDados.Parametro("@DIAV", CInt(txtDiaVencimento.Text)), _
                AcessoDados.Parametro("@DIAA", CInt(txtDiaAssembleia.Text)), _
                AcessoDados.Parametro("@IND", ddlIndice.SelectedValue), _
                AcessoDados.Parametro("@REG", txtRegulamento.Text.Trim().ToUpper()), _
                AcessoDados.Parametro("@ATIVO", IIf(chkAtivo.Checked, "S", "N")), _
                AcessoDados.Parametro("@CD", CodigoPlano)}

            If CodigoPlano = 0 Then
                Dim novo As Object = AcessoDados.ExecutaEscalar( _
                    "INSERT INTO TB_PLANO (NM_PLANO, ID_TIPO_BEM, QT_PRAZO_MESES, PC_TAXA_ADM, PC_ANTECIPACAO_TA, PC_FUNDO_RESERVA, PC_LANCE_EMBUTIDO_MAX, " & _
                    " PC_LANCE_FIXO, QT_COTAS_MINIMO, QT_COTAS_MAXIMO, NR_DIA_VENCIMENTO, NR_DIA_ASSEMBLEIA, CD_INDICE, DS_VERSAO_REGULAMENTO, FL_ATIVO) " & _
                    "VALUES (@NM, @TIPO, @PRAZO, @TA, @ANT, @FR, @EMB, @FIXO, @MIN, @MAX, @DIAV, @DIAA, @IND, @REG, @ATIVO); SELECT @@IDENTITY", p)
                ViewState("CD_PLANO") = CInt(novo)
            Else
                AcessoDados.ExecutaNonQuery( _
                    "UPDATE TB_PLANO SET NM_PLANO = @NM, ID_TIPO_BEM = @TIPO, QT_PRAZO_MESES = @PRAZO, PC_TAXA_ADM = @TA, PC_ANTECIPACAO_TA = @ANT, " & _
                    " PC_FUNDO_RESERVA = @FR, PC_LANCE_EMBUTIDO_MAX = @EMB, PC_LANCE_FIXO = @FIXO, QT_COTAS_MINIMO = @MIN, QT_COTAS_MAXIMO = @MAX, " & _
                    " NR_DIA_VENCIMENTO = @DIAV, NR_DIA_ASSEMBLEIA = @DIAA, CD_INDICE = @IND, DS_VERSAO_REGULAMENTO = @REG, FL_ATIVO = @ATIVO " & _
                    " WHERE CD_PLANO = @CD", p)
            End If

            ' bens permitidos: apaga e insere de novo
            AcessoDados.ExecutaNonQuery("DELETE FROM TB_PLANO_BEM WHERE CD_PLANO = " & CodigoPlano)
            For Each item As ListItem In cblBens.Items
                If item.Selected Then
                    AcessoDados.ExecutaNonQuery("INSERT INTO TB_PLANO_BEM (CD_PLANO, CD_BEM) VALUES (" & CodigoPlano & ", " & item.Value & ")")
                End If
            Next

            Carrega()
            MostraMensagem(lblMensagem, "Plano gravado.")
        Catch ex As Exception
            MostraMensagem(lblMensagem, "Erro ao gravar.", True)
        End Try
    End Sub

    Protected Sub btnVoltar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnVoltar.Click
        Response.Redirect("Planos.aspx")
    End Sub

End Class
