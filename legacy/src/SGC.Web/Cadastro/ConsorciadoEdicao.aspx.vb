Imports System.Data
Imports System.Data.SqlClient
Imports SGC.Comum

Partial Class Cadastro_ConsorciadoEdicao
    Inherits PaginaBase

    Private ReadOnly Property CodigoConsorciado() As Integer
        Get
            If ViewState("CD_CONSORCIADO") Is Nothing Then Return 0
            Return CInt(ViewState("CD_CONSORCIADO"))
        End Get
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaUF(ddlUF)
            If Request.QueryString("id") <> "" Then
                ViewState("CD_CONSORCIADO") = CInt(Request.QueryString("id"))
                Carrega()
            End If
            btnGravar.Enabled = PodeGravar()
        End If
    End Sub

    Private Sub Carrega()
        Dim dt As DataTable = AcessoDados.ExecutaDataTable("SELECT * FROM TB_CONSORCIADO WHERE CD_CONSORCIADO = " & CodigoConsorciado)
        If dt.Rows.Count = 0 Then
            MostraMensagem(lblMensagem, "Consorciado não encontrado.", True)
            Exit Sub
        End If

        Dim r As DataRow = dt.Rows(0)
        litTitulo.Text = "Consorciado: " & r("NM_CONSORCIADO")
        txtCPF.Text = Formatacao.FormataCPF(r("NR_CPF"))
        txtCPF.ReadOnly = True
        txtNome.Text = r("NM_CONSORCIADO").ToString()
        txtNascimento.Text = Formatacao.FormataData(r("DT_NASCIMENTO"))
        txtRG.Text = r("NR_RG").ToString()
        ddlEstadoCivil.SelectedValue = r("ID_ESTADO_CIVIL").ToString()
        txtProfissao.Text = r("DS_PROFISSAO").ToString()
        If Not IsDBNull(r("VL_RENDA_MENSAL")) Then txtRenda.Text = CDec(r("VL_RENDA_MENSAL")).ToString("#,##0.00")
        txtEmail.Text = r("DS_EMAIL").ToString()
        txtTelefone.Text = r("NR_TELEFONE").ToString()
        txtCelular.Text = r("NR_CELULAR").ToString()
        txtCEP.Text = Formatacao.FormataCEP(r("NR_CEP"))
        txtLogradouro.Text = r("DS_LOGRADOURO").ToString()
        txtNumero.Text = r("NR_ENDERECO").ToString()
        txtComplemento.Text = r("DS_COMPLEMENTO").ToString()
        txtBairro.Text = r("NM_BAIRRO").ToString()
        txtCidade.Text = r("NM_CIDADE").ToString()
        If ddlUF.Items.FindByValue(r("SG_UF").ToString()) IsNot Nothing Then ddlUF.SelectedValue = r("SG_UF").ToString()
        chkAtivo.Checked = (r("FL_ATIVO").ToString() = "S")

        gvCotas.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT T.CD_COTA, T.DT_ADESAO, T.ID_SITUACAO, B.DS_BEM, " & _
            "       G.NR_GRUPO + '/' + RIGHT('000' + CAST(T.NR_COTA AS VARCHAR), 3) + '-' + CAST(T.NR_VERSAO AS VARCHAR) AS DS_COTA " & _
            "  FROM TB_COTA T INNER JOIN TB_GRUPO G ON G.CD_GRUPO = T.CD_GRUPO INNER JOIN TB_BEM B ON B.CD_BEM = T.CD_BEM " & _
            " WHERE T.CD_CONSORCIADO = " & CodigoConsorciado & " ORDER BY T.DT_ADESAO")
        gvCotas.DataBind()
        pnlCotas.Visible = True
    End Sub

    Protected Sub btnGravar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnGravar.Click
        If Not Page.IsValid Then Exit Sub

        Dim cpf As String = Validacao.SomenteNumeros(txtCPF.Text)
        If Not Validacao.ValidaCPF(cpf) Then
            MostraMensagem(lblMensagem, "CPF inválido.", True)
            Exit Sub
        End If
        If txtNascimento.Text <> "" AndAlso Not Validacao.ValidaData(txtNascimento.Text) Then
            MostraMensagem(lblMensagem, "Data de nascimento inválida.", True)
            Exit Sub
        End If

        Try
            Dim parametros As New List(Of SqlParameter)
            parametros.Add(AcessoDados.Parametro("@NR_CPF", cpf))
            parametros.Add(AcessoDados.Parametro("@NM_CONSORCIADO", txtNome.Text.Trim().ToUpper()))
            parametros.Add(AcessoDados.Parametro("@DT_NASCIMENTO", DataOuNulo(txtNascimento.Text)))
            parametros.Add(AcessoDados.Parametro("@NR_RG", NuloSeVazio(txtRG.Text)))
            parametros.Add(AcessoDados.Parametro("@ID_ESTADO_CIVIL", NuloSeVazio(ddlEstadoCivil.SelectedValue)))
            parametros.Add(AcessoDados.Parametro("@DS_PROFISSAO", NuloSeVazio(txtProfissao.Text.ToUpper())))
            parametros.Add(AcessoDados.Parametro("@VL_RENDA_MENSAL", DecimalOuNulo(txtRenda.Text)))
            parametros.Add(AcessoDados.Parametro("@DS_EMAIL", NuloSeVazio(txtEmail.Text.ToLower())))
            parametros.Add(AcessoDados.Parametro("@NR_TELEFONE", NuloSeVazio(txtTelefone.Text)))
            parametros.Add(AcessoDados.Parametro("@NR_CELULAR", NuloSeVazio(txtCelular.Text)))
            parametros.Add(AcessoDados.Parametro("@NR_CEP", NuloSeVazio(Validacao.SomenteNumeros(txtCEP.Text))))
            parametros.Add(AcessoDados.Parametro("@DS_LOGRADOURO", NuloSeVazio(txtLogradouro.Text.ToUpper())))
            parametros.Add(AcessoDados.Parametro("@NR_ENDERECO", NuloSeVazio(txtNumero.Text)))
            parametros.Add(AcessoDados.Parametro("@DS_COMPLEMENTO", NuloSeVazio(txtComplemento.Text.ToUpper())))
            parametros.Add(AcessoDados.Parametro("@NM_BAIRRO", NuloSeVazio(txtBairro.Text.ToUpper())))
            parametros.Add(AcessoDados.Parametro("@NM_CIDADE", NuloSeVazio(txtCidade.Text.ToUpper())))
            parametros.Add(AcessoDados.Parametro("@SG_UF", NuloSeVazio(ddlUF.SelectedValue)))
            parametros.Add(AcessoDados.Parametro("@FL_ATIVO", IIf(chkAtivo.Checked, "S", "N")))
            parametros.Add(AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))

            If CodigoConsorciado = 0 Then
                Dim existe As Object = AcessoDados.ExecutaEscalar("SELECT CD_CONSORCIADO FROM TB_CONSORCIADO WHERE NR_CPF = '" & cpf & "'")
                If existe IsNot Nothing Then
                    MostraMensagem(lblMensagem, "CPF já cadastrado.", True)
                    Exit Sub
                End If

                Dim novo As Object = AcessoDados.ExecutaEscalar( _
                    "INSERT INTO TB_CONSORCIADO (NR_CPF, NM_CONSORCIADO, DT_NASCIMENTO, NR_RG, ID_ESTADO_CIVIL, DS_PROFISSAO, VL_RENDA_MENSAL, " & _
                    " DS_EMAIL, NR_TELEFONE, NR_CELULAR, NR_CEP, DS_LOGRADOURO, NR_ENDERECO, DS_COMPLEMENTO, NM_BAIRRO, NM_CIDADE, SG_UF, FL_ATIVO, CD_USUARIO_CADASTRO) " & _
                    "VALUES (@NR_CPF, @NM_CONSORCIADO, @DT_NASCIMENTO, @NR_RG, @ID_ESTADO_CIVIL, @DS_PROFISSAO, @VL_RENDA_MENSAL, " & _
                    " @DS_EMAIL, @NR_TELEFONE, @NR_CELULAR, @NR_CEP, @DS_LOGRADOURO, @NR_ENDERECO, @DS_COMPLEMENTO, @NM_BAIRRO, @NM_CIDADE, @SG_UF, @FL_ATIVO, @CD_USUARIO); " & _
                    "SELECT @@IDENTITY", parametros.ToArray())
                ViewState("CD_CONSORCIADO") = CInt(novo)
            Else
                parametros.Add(AcessoDados.Parametro("@CD_CONSORCIADO", CodigoConsorciado))
                AcessoDados.ExecutaNonQuery( _
                    "UPDATE TB_CONSORCIADO SET NM_CONSORCIADO = @NM_CONSORCIADO, DT_NASCIMENTO = @DT_NASCIMENTO, NR_RG = @NR_RG, " & _
                    " ID_ESTADO_CIVIL = @ID_ESTADO_CIVIL, DS_PROFISSAO = @DS_PROFISSAO, VL_RENDA_MENSAL = @VL_RENDA_MENSAL, DS_EMAIL = @DS_EMAIL, " & _
                    " NR_TELEFONE = @NR_TELEFONE, NR_CELULAR = @NR_CELULAR, NR_CEP = @NR_CEP, DS_LOGRADOURO = @DS_LOGRADOURO, NR_ENDERECO = @NR_ENDERECO, " & _
                    " DS_COMPLEMENTO = @DS_COMPLEMENTO, NM_BAIRRO = @NM_BAIRRO, NM_CIDADE = @NM_CIDADE, SG_UF = @SG_UF, FL_ATIVO = @FL_ATIVO, " & _
                    " DT_ALTERACAO = GETDATE() WHERE CD_CONSORCIADO = @CD_CONSORCIADO", parametros.ToArray())
            End If

            Carrega()
            MostraMensagem(lblMensagem, "Consorciado gravado.")
        Catch ex As Exception
            MostraMensagem(lblMensagem, "Erro ao gravar.", True)
        End Try
    End Sub

    Protected Sub btnVoltar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnVoltar.Click
        Response.Redirect("Consorciados.aspx")
    End Sub

End Class
