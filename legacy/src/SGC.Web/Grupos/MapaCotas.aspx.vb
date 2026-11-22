Imports System.Data
Imports System.Text
Imports SGC.Comum

Partial Class Grupos_MapaCotas
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim cdGrupo As Integer = CInt(Request.QueryString("id"))
        Dim g As DataRow = AcessoDados.ExecutaDataTable("SELECT NR_GRUPO, QT_COTAS_MAXIMO FROM TB_GRUPO WHERE CD_GRUPO = " & cdGrupo).Rows(0)
        litGrupo.Text = g("NR_GRUPO").ToString()
        lnkGrupo.NavigateUrl = "GrupoEdicao.aspx?id=" & cdGrupo

        ' versao vigente de cada numero (cancelada nao ocupa o numero)
        Dim cotas As DataTable = AcessoDados.ExecutaDataTable( _
            "SELECT C.CD_COTA, C.NR_COTA, C.NR_VERSAO, C.ID_SITUACAO, X.NM_CONSORCIADO " & _
            "  FROM TB_COTA C INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO " & _
            " WHERE C.CD_GRUPO = " & cdGrupo & " AND C.ID_SITUACAO <> 6")

        Dim porNumero As New Dictionary(Of Integer, DataRow)
        For Each r As DataRow In cotas.Rows
            porNumero(CInt(r("NR_COTA"))) = r
        Next

        Dim maximo As Integer = CInt(g("QT_COTAS_MAXIMO"))
        Dim sb As New StringBuilder
        sb.Append("<table class='mapa'><tr>")
        For n As Integer = 1 To maximo
            If porNumero.ContainsKey(n) Then
                Dim r As DataRow = porNumero(n)
                sb.AppendFormat("<td class='cota{0}' title='{1} - {2}'><a href='../Cotas/CotaDetalhe.aspx?id={3}'>{4}</a></td>", _
                                r("ID_SITUACAO"), Server.HtmlEncode(r("NM_CONSORCIADO").ToString()), DescricaoSituacaoCota(r("ID_SITUACAO")), _
                                r("CD_COTA"), Formatacao.FormataCota(r("NR_COTA"), r("NR_VERSAO")))
            Else
                sb.AppendFormat("<td class='cotaLivre'>{0}</td>", n.ToString("000"))
            End If
            If n Mod 15 = 0 Then sb.Append("</tr><tr>")
        Next
        sb.Append("</tr></table>")
        litMapa.Text = sb.ToString()

        litResumo.Text = porNumero.Count & " de " & maximo & " cotas ocupadas"
    End Sub

End Class
