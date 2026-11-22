Imports System.Data
Imports SGC.Comum

Partial Class _Default
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            CarregaPainel()
        End If
    End Sub

    Private Sub CarregaPainel()
        Dim inicioMes As Date = New Date(Date.Today.Year, Date.Today.Month, 1)

        litCotasMes.Text = AcessoDados.ExecutaEscalar("SELECT COUNT(*) FROM TB_COTA WHERE ID_SITUACAO <> 6 AND DT_ADESAO >= '" & inicioMes.ToString("yyyyMMdd") & "'").ToString()
        litAguardando.Text = AcessoDados.ExecutaEscalar("SELECT COUNT(*) FROM TB_PROPOSTA WHERE ID_SITUACAO = 2").ToString()
        litDigitadas.Text = AcessoDados.ExecutaEscalar("SELECT COUNT(*) FROM TB_PROPOSTA WHERE ID_SITUACAO = 1").ToString()
        litCotasAtivas.Text = AcessoDados.ExecutaEscalar("SELECT COUNT(*) FROM TB_COTA WHERE ID_SITUACAO IN (1, 2)").ToString()

        gvFormacao.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT G.CD_GRUPO, G.NR_GRUPO, P.NM_PLANO, G.DT_ABERTURA, G.QT_COTAS_MINIMO, G.QT_COTAS_MAXIMO, " & _
            "       (SELECT COUNT(*) FROM TB_COTA C WHERE C.CD_GRUPO = G.CD_GRUPO AND C.ID_SITUACAO = 1) AS QT_ATIVAS " & _
            "  FROM TB_GRUPO G INNER JOIN TB_PLANO P ON P.CD_PLANO = G.CD_PLANO " & _
            " WHERE G.ID_SITUACAO = 1 ORDER BY G.DT_ABERTURA")
        gvFormacao.DataBind()

        gvUltimas.DataSource = AcessoDados.ExecutaDataTable( _
            "SELECT TOP 10 P.CD_PROPOSTA, P.NR_PROPOSTA, P.DT_EFETIVACAO, C.NM_CONSORCIADO, B.DS_BEM, V.NM_VENDEDOR, P.CD_COTA, " & _
            "       G.NR_GRUPO + '/' + RIGHT('000' + CAST(T.NR_COTA AS VARCHAR), 3) + '-' + CAST(T.NR_VERSAO AS VARCHAR) AS DS_COTA " & _
            "  FROM TB_PROPOSTA P " & _
            " INNER JOIN TB_CONSORCIADO C ON C.CD_CONSORCIADO = P.CD_CONSORCIADO " & _
            " INNER JOIN TB_BEM B ON B.CD_BEM = P.CD_BEM " & _
            " INNER JOIN TB_VENDEDOR V ON V.CD_VENDEDOR = P.CD_VENDEDOR " & _
            " INNER JOIN TB_COTA T ON T.CD_COTA = P.CD_COTA " & _
            " INNER JOIN TB_GRUPO G ON G.CD_GRUPO = T.CD_GRUPO " & _
            " WHERE P.ID_SITUACAO = 3 ORDER BY P.DT_EFETIVACAO DESC, P.CD_PROPOSTA DESC")
        gvUltimas.DataBind()
    End Sub

End Class
