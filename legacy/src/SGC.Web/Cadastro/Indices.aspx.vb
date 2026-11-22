Imports System.Data
Imports SGC.Comum

Partial Class Cadastro_Indices
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaCombo(ddlIndice, "SELECT CD_INDICE, SG_INDICE + ' - ' + NM_INDICE AS DS FROM TB_INDICE ORDER BY CD_INDICE", "DS", "CD_INDICE")
            CarregaCombo(ddlAno, "SELECT DISTINCT NR_ANO_MES / 100 AS ANO FROM TB_INDICE_VALOR ORDER BY 1 DESC", "ANO", "ANO")
            Carrega()
        End If
    End Sub

    Private Sub Carrega()
        If ddlIndice.SelectedValue = "" Or ddlAno.SelectedValue = "" Then Exit Sub

        ' traz 11 meses antes do ano para calcular o acumulado
        Dim ano As Integer = CInt(ddlAno.SelectedValue)
        Dim dt As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT NR_ANO_MES, PC_VARIACAO, DT_CADASTRO FROM TB_INDICE_VALOR " & _
            " WHERE CD_INDICE = " & ddlIndice.SelectedValue & " AND NR_ANO_MES BETWEEN " & ((ano - 1) * 100 + 2) & " AND " & (ano * 100 + 12) & _
            " ORDER BY NR_ANO_MES")

        Dim saida As New DataTable
        saida.Columns.Add("DS_MES", GetType(String))
        saida.Columns.Add("PC_VARIACAO", GetType(Decimal))
        saida.Columns.Add("PC_ACUMULADO_12M", GetType(Decimal))
        saida.Columns.Add("DT_CADASTRO", GetType(Date))

        Dim meses() As String = {"Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"}
        Dim acumuladoAno As Double = 1

        For i As Integer = 0 To dt.Rows.Count - 1
            Dim am As Integer = CInt(dt.Rows(i)("NR_ANO_MES"))
            If am \ 100 <> ano Then Continue For

            ' acumulado: produto dos ultimos 12 meses disponiveis
            Dim fator As Double = 1
            Dim qt As Integer = 0
            For j As Integer = i To 0 Step -1
                If qt = 12 Then Exit For
                fator = fator * (1 + CDbl(dt.Rows(j)("PC_VARIACAO")) / 100)
                qt += 1
            Next

            Dim r As DataRow = saida.NewRow()
            r("DS_MES") = meses((am Mod 100) - 1) & "/" & ano
            r("PC_VARIACAO") = dt.Rows(i)("PC_VARIACAO")
            If qt = 12 Then r("PC_ACUMULADO_12M") = Math.Round((fator - 1) * 100, 4)
            r("DT_CADASTRO") = dt.Rows(i)("DT_CADASTRO")
            saida.Rows.Add(r)

            acumuladoAno = acumuladoAno * (1 + CDbl(dt.Rows(i)("PC_VARIACAO")) / 100)
        Next

        gvValores.DataSource = saida
        gvValores.DataBind()
        If gvValores.FooterRow IsNot Nothing Then
            gvValores.FooterRow.Cells(0).Text = "No ano"
            gvValores.FooterRow.Cells(1).Text = Math.Round((acumuladoAno - 1) * 100, 4).ToString("0.0000")
            gvValores.FooterRow.Cells(1).CssClass = "num"
        End If
    End Sub

    Protected Sub ddlIndice_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlIndice.SelectedIndexChanged
        Carrega()
    End Sub

    Protected Sub ddlAno_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlAno.SelectedIndexChanged
        Carrega()
    End Sub

End Class
