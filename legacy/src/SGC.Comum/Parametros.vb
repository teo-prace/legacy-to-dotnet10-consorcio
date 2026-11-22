'==============================================================================
' Leitura de TB_PARAMETRO.
' 2011-04 M.S.
'==============================================================================
Public Class Parametros

    Public Shared Function Le(ByVal nome As String) As String
        Dim o As Object = AcessoDados.ExecutaEscalar("SELECT DS_VALOR FROM TB_PARAMETRO WHERE NM_PARAMETRO = @NOME", _
                                                     AcessoDados.Parametro("@NOME", nome))
        If o Is Nothing OrElse IsDBNull(o) Then Return ""
        Return o.ToString()
    End Function

    Public Shared Function LeDecimal(ByVal nome As String) As Decimal
        Dim s As String = Le(nome)
        If s = "" Then Return 0
        ' gravado com ponto decimal
        Return Decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture)
    End Function

End Class
