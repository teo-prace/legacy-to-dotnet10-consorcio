Imports System.Data
Imports SGC.Comum

Partial Class Cadastro_BemEdicao
    Inherits PaginaBase

    Private ReadOnly Property CodigoBem() As Integer
        Get
            If ViewState("CD_BEM") Is Nothing Then Return 0
            Return CInt(ViewState("CD_BEM"))
        End Get
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaCombo(ddlIndice, "SELECT CD_INDICE, SG_INDICE FROM TB_INDICE ORDER BY SG_INDICE", "SG_INDICE", "CD_INDICE")
            If Request.QueryString("id") <> "" Then
                ViewState("CD_BEM") = CInt(Request.QueryString("id"))
                Carrega()
            End If
            btnGravar.Enabled = PodeGravar()
            btnIncluirValor.Enabled = PodeGravar()
        End If
    End Sub

    Private Sub Carrega()
        Dim dt As DataTable = AcessoDados.ExecutaDataTable("SELECT * FROM TB_BEM WHERE CD_BEM = " & CodigoBem)
        If dt.Rows.Count = 0 Then Exit Sub
        Dim r As DataRow = dt.Rows(0)
        litTitulo.Text = "Bem: " & r("DS_BEM")
        txtDescricao.Text = r("DS_BEM").ToString()
        ddlTipo.SelectedValue = r("ID_TIPO_BEM").ToString()
        ddlIndice.SelectedValue = r("CD_INDICE").ToString()
        txtValor.Text = CDec(r("VL_BEM")).ToString("#,##0.00")
        txtValor.ReadOnly = True
        lblValorInfo.Text = "(alterar pelo histórico de valores)"
        chkAtivo.Checked = (r("FL_ATIVO").ToString() = "S")

        gvHistorico.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT V.DT_VIGENCIA, V.VL_BEM, V.DS_MOTIVO, U.NM_USUARIO, V.DT_CADASTRO " & _
            "  FROM TB_BEM_VALOR V LEFT JOIN TB_USUARIO U ON U.CD_USUARIO = V.CD_USUARIO " & _
            " WHERE V.CD_BEM = " & CodigoBem & " ORDER BY V.DT_VIGENCIA DESC")
        gvHistorico.DataBind()
        pnlHistorico.Visible = True
    End Sub

    Protected Sub btnGravar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnGravar.Click
        If txtDescricao.Text.Trim() = "" Then
            MostraMensagem(lblMensagem, "Informe a descrição.", True)
            Exit Sub
        End If

        Try
            If CodigoBem = 0 Then
                Dim valor As Decimal = CDec(txtValor.Text)
                Dim novo As Object = AcessoDados.ExecutaEscalar( _
                    "INSERT INTO TB_BEM (DS_BEM, ID_TIPO_BEM, CD_INDICE, VL_BEM, FL_ATIVO) VALUES (@DS, @TIPO, @INDICE, @VALOR, @ATIVO); SELECT @@IDENTITY", _
                    AcessoDados.Parametro("@DS", txtDescricao.Text.Trim().ToUpper()), _
                    AcessoDados.Parametro("@TIPO", ddlTipo.SelectedValue), _
                    AcessoDados.Parametro("@INDICE", ddlIndice.SelectedValue), _
                    AcessoDados.Parametro("@VALOR", valor), _
                    AcessoDados.Parametro("@ATIVO", IIf(chkAtivo.Checked, "S", "N")))
                ViewState("CD_BEM") = CInt(novo)
                AcessoDados.ExecutaNonQuery("INSERT INTO TB_BEM_VALOR (CD_BEM, DT_VIGENCIA, VL_BEM, DS_MOTIVO, CD_USUARIO) VALUES (" & _
                    CodigoBem & ", '" & Date.Today.ToString("yyyyMMdd") & "', @VALOR, 'Cadastro inicial', " & CodigoUsuario & ")", _
                    AcessoDados.Parametro("@VALOR", valor))
            Else
                AcessoDados.ExecutaNonQuery( _
                    "UPDATE TB_BEM SET DS_BEM = @DS, ID_TIPO_BEM = @TIPO, CD_INDICE = @INDICE, FL_ATIVO = @ATIVO WHERE CD_BEM = @CD", _
                    AcessoDados.Parametro("@DS", txtDescricao.Text.Trim().ToUpper()), _
                    AcessoDados.Parametro("@TIPO", ddlTipo.SelectedValue), _
                    AcessoDados.Parametro("@INDICE", ddlIndice.SelectedValue), _
                    AcessoDados.Parametro("@ATIVO", IIf(chkAtivo.Checked, "S", "N")), _
                    AcessoDados.Parametro("@CD", CodigoBem))
            End If
            Carrega()
            MostraMensagem(lblMensagem, "Bem gravado.")
        Catch ex As Exception
            MostraMensagem(lblMensagem, "Erro ao gravar.", True)
        End Try
    End Sub

    Protected Sub btnIncluirValor_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnIncluirValor.Click
        If Not Validacao.ValidaData(txtVigencia.Text) Or txtNovoValor.Text.Trim() = "" Then
            MostraMensagem(lblMensagem, "Informe vigência (dd/mm/aaaa) e valor.", True)
            Exit Sub
        End If

        Dim vigencia As Date = Date.ParseExact(txtVigencia.Text, "dd/MM/yyyy", Nothing)
        Dim valor As Decimal = CDec(txtNovoValor.Text)

        Try
            AcessoDados.ExecutaNonQuery( _
                "INSERT INTO TB_BEM_VALOR (CD_BEM, DT_VIGENCIA, VL_BEM, DS_MOTIVO, CD_USUARIO) VALUES (@CD, @DT, @VL, @MOTIVO, @USU)", _
                AcessoDados.Parametro("@CD", CodigoBem), _
                AcessoDados.Parametro("@DT", vigencia), _
                AcessoDados.Parametro("@VL", valor), _
                AcessoDados.Parametro("@MOTIVO", NuloSeVazio(txtMotivo.Text)), _
                AcessoDados.Parametro("@USU", CodigoUsuario))

            ' valor atual = vigencia mais recente ate hoje
            AcessoDados.ExecutaNonQuery("UPDATE TB_BEM SET VL_BEM = dbo.FN_VALOR_BEM(CD_BEM, GETDATE()) WHERE CD_BEM = " & CodigoBem)

            txtVigencia.Text = ""
            txtNovoValor.Text = ""
            txtMotivo.Text = ""
            Carrega()
            MostraMensagem(lblMensagem, "Valor incluído.")
        Catch ex As Exception
            MostraMensagem(lblMensagem, "Erro ao incluir valor. Verifique se já existe valor para esta vigência.", True)
        End Try
    End Sub

    Protected Sub btnVoltar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnVoltar.Click
        Response.Redirect("Bens.aspx")
    End Sub

End Class
