'==============================================================================
' Validacoes.
' 2011-04 M.S.
'==============================================================================
Public Class Validacao

    Public Shared Function SomenteNumeros(ByVal texto As String) As String
        If texto Is Nothing Then Return ""
        Dim sb As New System.Text.StringBuilder
        For Each c As Char In texto
            If Char.IsDigit(c) Then sb.Append(c)
        Next
        Return sb.ToString()
    End Function

    Public Shared Function ValidaCPF(ByVal cpf As String) As Boolean
        cpf = SomenteNumeros(cpf)
        If cpf.Length <> 11 Then Return False
        If cpf = New String(cpf(0), 11) Then Return False

        Dim soma As Integer = 0
        Dim i As Integer
        For i = 0 To 8
            soma += CInt(cpf(i).ToString()) * (10 - i)
        Next
        Dim dv1 As Integer = 11 - (soma Mod 11)
        If dv1 >= 10 Then dv1 = 0

        soma = 0
        For i = 0 To 9
            soma += CInt(cpf(i).ToString()) * (11 - i)
        Next
        Dim dv2 As Integer = 11 - (soma Mod 11)
        If dv2 >= 10 Then dv2 = 0

        Return cpf(9).ToString() = dv1.ToString() AndAlso cpf(10).ToString() = dv2.ToString()
    End Function

    Public Shared Function ValidaEmail(ByVal email As String) As Boolean
        If email Is Nothing OrElse email.Trim() = "" Then Return True
        Return email.Contains("@") AndAlso email.Contains(".")
    End Function

    Public Shared Function ValidaData(ByVal texto As String) As Boolean
        Dim d As Date
        Return Date.TryParseExact(texto, "dd/MM/yyyy", Nothing, Globalization.DateTimeStyles.None, d)
    End Function

End Class
