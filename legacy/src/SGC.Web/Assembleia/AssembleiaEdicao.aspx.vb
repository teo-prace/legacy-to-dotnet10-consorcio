Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

'==============================================================================
' Assembleia: premios, lances, apuracao e resultado.
' 2014-08 J.P. (substitui a tela de apuracao do SGC Desktop)
'==============================================================================
Partial Class Assembleia_AssembleiaEdicao
    Inherits PaginaBase

    Private ReadOnly Property CodigoAssembleia() As Integer
        Get
            Return CInt(ViewState("CD_ASSEMBLEIA"))
        End Get
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            ViewState("CD_ASSEMBLEIA") = CInt(Request.QueryString("id"))
            Carrega()
        End If
    End Sub

    Private Sub Carrega()
        Dim a As DataRow = AcessoDados.ExecutaDataTable( _
            "SELECT A.*, G.NR_GRUPO, G.PC_LANCE_EMBUTIDO_MAX, G.PC_LANCE_FIXO, U.NM_USUARIO " & _
            "  FROM TB_ASSEMBLEIA A INNER JOIN TB_GRUPO G ON G.CD_GRUPO = A.CD_GRUPO " & _
            "  LEFT JOIN TB_USUARIO U ON U.CD_USUARIO = A.CD_USUARIO_APURACAO " & _
            " WHERE A.CD_ASSEMBLEIA = " & CodigoAssembleia).Rows(0)

        Dim situacao As Integer = CInt(a("ID_SITUACAO"))
        ViewState("CD_GRUPO") = a("CD_GRUPO")
        litTitulo.Text = "Assembleia " & a("NR_ASSEMBLEIA") & " - grupo " & a("NR_GRUPO")
        lnkGrupo.Text = a("NR_GRUPO").ToString()
        lnkGrupo.NavigateUrl = "~/Grupos/GrupoEdicao.aspx?id=" & a("CD_GRUPO")
        lnkVoltar.NavigateUrl = "Assembleias.aspx?grupo=" & a("CD_GRUPO")
        litSituacao.Text = IIf(situacao = 1, "Agendada", IIf(situacao = 2, "Apurada", "Cancelada"))
        litData.Text = Formatacao.FormataData(a("DT_ASSEMBLEIA"))
        litLimite.Text = Formatacao.FormataData(a("DT_LIMITE_LANCE"))
        litRegras.Text = Formatacao.FormataPercentual(a("PC_LANCE_EMBUTIDO_MAX"), 2) & " / " & _
                         IIf(IsDBNull(a("PC_LANCE_FIXO")), "não tem", Formatacao.FormataPercentual(a("PC_LANCE_FIXO"), 2))
        litAptas.Text = AcessoDados.ExecutaEscalar("SELECT COUNT(*) FROM TB_COTA WHERE CD_GRUPO = " & a("CD_GRUPO") & _
                                                   " AND ID_SITUACAO = 1 AND dbo.FN_COTA_APTA(CD_COTA, GETDATE()) = 1").ToString()

        pnlAgendada.Visible = (situacao = 1)
        pnlApurada.Visible = (situacao = 2)

        If situacao = 1 Then
            txtExtracao.Text = a("NR_EXTRACAO").ToString()
            txtP1.Text = a("NR_PREMIO_1").ToString()
            txtP2.Text = a("NR_PREMIO_2").ToString()
            txtP3.Text = a("NR_PREMIO_3").ToString()
            txtP4.Text = a("NR_PREMIO_4").ToString()
            txtP5.Text = a("NR_PREMIO_5").ToString()
            gvLancesAgendada.DataSource = Lances()
            gvLancesAgendada.DataBind()

            Dim grava As Boolean = PodeGravar()
            btnSalvarPremios.Enabled = grava
            btnRegistrarLance.Enabled = grava
            btnApurar.Enabled = grava

            If CDate(a("DT_ASSEMBLEIA")) > Date.Today Then
                lblAviso.Text = "Atenção: a assembleia é em " & Formatacao.FormataData(a("DT_ASSEMBLEIA")) & ". Confira a extração da Loteria Federal antes de apurar."
                lblAviso.Visible = True
            End If
        ElseIf situacao = 2 Then
            litPremios.Text = "extração " & a("NR_EXTRACAO") & ": " & a("NR_PREMIO_1") & " - " & a("NR_PREMIO_2") & " - " & _
                              a("NR_PREMIO_3") & " - " & a("NR_PREMIO_4") & " - " & a("NR_PREMIO_5")
            litBase.Text = CInt(a("NR_COTA_SORTEADA")).ToString("000")
            litApuracao.Text = CDate(a("DT_APURACAO")).ToString("dd/MM/yyyy HH:mm") & " por " & a("NM_USUARIO")
            litSaldo.Text = Formatacao.FormataMoeda(a("VL_SALDO_FC"))
            litRecursos.Text = Formatacao.FormataMoeda(a("VL_RECURSOS"))

            gvContemplacoes.DataSource = AcessoDados.ExecutaDataTable( _
                "SELECT T.*, X.NM_CONSORCIADO, CASE T.ID_TIPO WHEN 1 THEN 'Sorteio' WHEN 3 THEN 'Devolução' ELSE 'Lance' END AS DS_TIPO, " & _
                "       CASE T.ID_SITUACAO WHEN 1 THEN 'Aguardando liberação' WHEN 2 THEN 'Crédito liberado' ELSE 'Cancelada' END AS DS_SITUACAO, " & _
                "       G.NR_GRUPO + '/' + RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR) AS DS_COTA " & _
                "  FROM TB_CONTEMPLACAO T INNER JOIN TB_COTA C ON C.CD_COTA = T.CD_COTA INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO " & _
                " INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
                " WHERE T.CD_ASSEMBLEIA = " & CodigoAssembleia & " ORDER BY T.ID_TIPO, T.CD_CONTEMPLACAO")
            gvContemplacoes.DataBind()
            gvLancesApurada.DataSource = Lances()
            gvLancesApurada.DataBind()
        End If
    End Sub

    Private Function Lances() As DataTable
        Return AcessoDados.ExecutaDataTable( _
            "SELECT L.*, X.NM_CONSORCIADO, CASE L.ID_TIPO_LANCE WHEN 1 THEN 'Livre' ELSE 'Fixo' END AS DS_TIPO, " & _
            "       CASE L.ID_SITUACAO WHEN 1 THEN 'Registrado' WHEN 2 THEN 'Vencedor' WHEN 3 THEN 'Não contemplado' WHEN 4 THEN 'Desclassificado' ELSE 'Cancelado' END AS DS_SITUACAO, " & _
            "       CASE WHEN dbo.FN_COTA_APTA(C.CD_COTA, GETDATE()) = 1 THEN 'sim' ELSE 'não' END AS DS_APTA, " & _
            "       RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR) AS DS_COTA " & _
            "  FROM TB_LANCE L INNER JOIN TB_COTA C ON C.CD_COTA = L.CD_COTA INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
            " WHERE L.CD_ASSEMBLEIA = " & CodigoAssembleia & " ORDER BY ISNULL(L.NR_ORDEM, 999), L.PC_LANCE DESC")
    End Function

    Protected Sub btnSalvarPremios_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSalvarPremios.Click
        AcessoDados.ExecutaNonQuery("UPDATE TB_ASSEMBLEIA SET NR_EXTRACAO = " & IIf(txtExtracao.Text = "", "NULL", txtExtracao.Text) & _
            ", NR_PREMIO_1 = '" & txtP1.Text.PadLeft(5, "0"c) & "', NR_PREMIO_2 = '" & txtP2.Text.PadLeft(5, "0"c) & _
            "', NR_PREMIO_3 = '" & txtP3.Text.PadLeft(5, "0"c) & "', NR_PREMIO_4 = '" & txtP4.Text.PadLeft(5, "0"c) & _
            "', NR_PREMIO_5 = '" & txtP5.Text.PadLeft(5, "0"c) & "' WHERE CD_ASSEMBLEIA = " & CodigoAssembleia & " AND ID_SITUACAO = 1")
        Carrega()
        MostraMensagem(lblMensagem, "Prêmios gravados.")
    End Sub

    Protected Sub btnRegistrarLance_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnRegistrarLance.Click
        If txtNrCota.Text = "" Or txtPcLance.Text = "" Then
            MostraMensagem(lblMensagem, "Informe a cota e o percentual do lance.", True)
            Exit Sub
        End If

        Dim cdCota As Object = AcessoDados.ExecutaEscalar("SELECT CD_COTA FROM TB_COTA WHERE CD_GRUPO = " & ViewState("CD_GRUPO") & _
                                                          " AND NR_COTA = " & CInt(txtNrCota.Text) & " AND ID_SITUACAO NOT IN (6)")
        If cdCota Is Nothing Then
            MostraMensagem(lblMensagem, "Cota não encontrada no grupo.", True)
            Exit Sub
        End If

        Try
            AcessoDados.ExecutaProcedureRetorno("SP_REGISTRA_LANCE", _
                AcessoDados.Parametro("@CD_ASSEMBLEIA", CodigoAssembleia), _
                AcessoDados.Parametro("@CD_COTA", cdCota), _
                AcessoDados.Parametro("@ID_TIPO_LANCE", CInt(ddlTipoLance.SelectedValue)), _
                AcessoDados.Parametro("@PC_LANCE", CDec(txtPcLance.Text)), _
                AcessoDados.Parametro("@PC_EMBUTIDO", IIf(txtPcEmbutido.Text = "", 0, CDec(txtPcEmbutido.Text))), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            txtNrCota.Text = ""
            txtPcLance.Text = ""
            txtPcEmbutido.Text = "0"
            Carrega()
            MostraMensagem(lblMensagem, "Lance registrado.")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

    Protected Sub gvLancesAgendada_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles gvLancesAgendada.RowCommand
        If e.CommandName = "Cancelar" AndAlso PodeGravar() Then
            Dim cd As Integer = CInt(gvLancesAgendada.DataKeys(CInt(e.CommandArgument)).Value)
            AcessoDados.ExecutaNonQuery("UPDATE TB_LANCE SET ID_SITUACAO = 5, DS_MOTIVO = 'Cancelado pelo operador' WHERE CD_LANCE = " & cd)
            Carrega()
        End If
    End Sub

    Protected Sub btnApurar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnApurar.Click
        Try
            AcessoDados.ExecutaProcedureRetorno("SP_APURA_ASSEMBLEIA", _
                AcessoDados.Parametro("@CD_ASSEMBLEIA", CodigoAssembleia), _
                AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))
            Carrega()
            MostraMensagem(lblMensagem, "Assembleia apurada.")
        Catch ex As SqlException
            MostraMensagem(lblMensagem, ex.Message, True)
        End Try
    End Sub

End Class
