'==============================================================================
' Formatacao padrao de telas e relatorios.
' 2011-04 M.S.
'==============================================================================
Public Class Formatacao

    Public Shared Function FormataCPF(ByVal cpf As Object) As String
        If cpf Is Nothing OrElse IsDBNull(cpf) Then Return ""
        Dim s As String = Validacao.SomenteNumeros(cpf.ToString())
        If s.Length <> 11 Then Return cpf.ToString()
        Return s.Substring(0, 3) & "." & s.Substring(3, 3) & "." & s.Substring(6, 3) & "-" & s.Substring(9, 2)
    End Function

    Public Shared Function FormataCEP(ByVal cep As Object) As String
        If cep Is Nothing OrElse IsDBNull(cep) Then Return ""
        Dim s As String = Validacao.SomenteNumeros(cep.ToString())
        If s.Length <> 8 Then Return cep.ToString()
        Return s.Substring(0, 5) & "-" & s.Substring(5, 3)
    End Function

    ' 15 / 0 -> "015-0"
    Public Shared Function FormataCota(ByVal numero As Object, ByVal versao As Object) As String
        Return CInt(numero).ToString("000") & "-" & CInt(versao).ToString()
    End Function

    ' "002087" / 15 / 0 -> "002087/015-0"
    Public Shared Function FormataGrupoCota(ByVal grupo As Object, ByVal numero As Object, ByVal versao As Object) As String
        Return grupo.ToString() & "/" & FormataCota(numero, versao)
    End Function

    Public Shared Function FormataMoeda(ByVal valor As Object) As String
        If valor Is Nothing OrElse IsDBNull(valor) Then Return ""
        Return "R$ " & CDec(valor).ToString("#,##0.00")
    End Function

    Public Shared Function FormataPercentual(ByVal valor As Object, Optional ByVal casas As Integer = 4) As String
        If valor Is Nothing OrElse IsDBNull(valor) Then Return ""
        Return CDec(valor).ToString("0." & New String("0"c, casas)) & "%"
    End Function

    Public Shared Function FormataData(ByVal valor As Object) As String
        If valor Is Nothing OrElse IsDBNull(valor) Then Return ""
        Return CDate(valor).ToString("dd/MM/yyyy")
    End Function

End Class
