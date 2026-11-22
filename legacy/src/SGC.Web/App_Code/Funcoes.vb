Imports System.Data
Imports System.Web.UI.WebControls
Imports SGC.Comum

'==============================================================================
' Funcoes de apoio das telas.
' 2011-04 M.S.
'==============================================================================
Public Module Funcoes

    Public Sub CarregaCombo(ByVal ddl As DropDownList, ByVal sql As String, ByVal campoTexto As String, _
                            ByVal campoValor As String, Optional ByVal textoInicial As String = "")
        ddl.Items.Clear()
        ddl.DataSource = AcessoDados.ExecutaDataTable(sql)
        ddl.DataTextField = campoTexto
        ddl.DataValueField = campoValor
        ddl.DataBind()
        If textoInicial <> "" Then ddl.Items.Insert(0, New ListItem(textoInicial, ""))
    End Sub

    Public Function NuloSeVazio(ByVal texto As String) As Object
        If texto Is Nothing OrElse texto.Trim() = "" Then Return DBNull.Value
        Return texto.Trim()
    End Function

    Public Function DataOuNulo(ByVal texto As String) As Object
        If texto Is Nothing OrElse texto.Trim() = "" Then Return DBNull.Value
        Return Date.ParseExact(texto.Trim(), "dd/MM/yyyy", Nothing)
    End Function

    Public Function DecimalOuNulo(ByVal texto As String) As Object
        If texto Is Nothing OrElse texto.Trim() = "" Then Return DBNull.Value
        Return CDec(texto.Replace("R$", "").Trim())
    End Function

    Public Function FormataMoeda(ByVal valor As Object) As String
        If IsDBNull(valor) OrElse valor Is Nothing Then Return ""
        Return Format(CDec(valor), "R$ #,##0.00")
    End Function

    Public Function DescricaoSituacaoCota(ByVal id As Object) As String
        Select Case CInt(id)
            Case 1 : Return "Ativa"
            Case 2 : Return "Contemplada"
            Case 3 : Return "Excluída"
            Case 4 : Return "Desistente"
            Case 5 : Return "Quitada"
            Case 6 : Return "Cancelada"
        End Select
        Return ""
    End Function

    Public Function DescricaoSituacaoGrupo(ByVal id As Object) As String
        Select Case CInt(id)
            Case 1 : Return "Em formação"
            Case 2 : Return "Em andamento"
            Case 3 : Return "Encerrado"
            Case 4 : Return "Cancelado"
        End Select
        Return ""
    End Function

    Public Function DescricaoSituacaoProposta(ByVal id As Object) As String
        Select Case CInt(id)
            Case 1 : Return "Digitada"
            Case 2 : Return "Aguardando pagamento"
            Case 3 : Return "Efetivada"
            Case 4 : Return "Cancelada"
            Case 5 : Return "Arrependimento"
        End Select
        Return ""
    End Function

    Public Function DescricaoSituacaoParcela(ByVal id As Object) As String
        Select Case CInt(id)
            Case 1 : Return "Aberta"
            Case 2 : Return "Emitida"
            Case 3 : Return "Paga"
            Case 4 : Return "Cancelada"
        End Select
        Return ""
    End Function

    Public Function DescricaoTipoBem(ByVal id As Object) As String
        Select Case CInt(id)
            Case 1 : Return "Imóvel"
            Case 2 : Return "Veículo leve"
            Case 3 : Return "Veículo pesado"
            Case 4 : Return "Serviços"
        End Select
        Return ""
    End Function

    Public Function DescricaoPerfil(ByVal id As Object) As String
        Select Case CInt(id)
            Case 1 : Return "Administrador"
            Case 2 : Return "Operador"
            Case 3 : Return "Consulta"
        End Select
        Return ""
    End Function

    Public Sub CarregaUF(ByVal ddl As DropDownList)
        ddl.Items.Clear()
        ddl.Items.Add(New ListItem("", ""))
        For Each uf As String In "AC,AL,AP,AM,BA,CE,DF,ES,GO,MA,MT,MS,MG,PA,PB,PR,PE,PI,RJ,RN,RS,RO,RR,SC,SP,SE,TO".Split(","c)
            ddl.Items.Add(uf)
        Next
    End Sub

End Module
