Imports System.Security.Cryptography
Imports System.Text

'==============================================================================
' Mesmo algoritmo do SGC Desktop (VB6) para os usuarios continuarem com a
' mesma senha nos dois sistemas.
' 2011-04 M.S.
'==============================================================================
Public Class Criptografia

    Public Shared Function MD5(ByVal texto As String) As String
        Dim md As System.Security.Cryptography.MD5 = System.Security.Cryptography.MD5.Create()
        Dim bytes() As Byte = md.ComputeHash(Encoding.ASCII.GetBytes(texto))
        Dim sb As New StringBuilder
        For Each b As Byte In bytes
            sb.Append(b.ToString("X2"))
        Next
        Return sb.ToString()
    End Function

End Class
