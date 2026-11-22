Imports System.IO

'==============================================================================
' Log em arquivo texto, um arquivo por dia.
' 2011-04 M.S.
'==============================================================================
Public Class Log

    Private Const PASTA_LOG As String = "C:\SGC\Logs"

    Public Shared Sub Grava(ByVal mensagem As String)
        Try
            If Not Directory.Exists(PASTA_LOG) Then Directory.CreateDirectory(PASTA_LOG)
            Dim arquivo As String = Path.Combine(PASTA_LOG, "SGC_" & Date.Now.ToString("yyyyMMdd") & ".log")
            File.AppendAllText(arquivo, Date.Now.ToString("dd/MM/yyyy HH:mm:ss") & "  " & mensagem & vbCrLf)
        Catch
            ' se nao conseguir gravar o log, segue
        End Try
    End Sub

    Public Shared Sub Grava(ByVal ex As Exception)
        Grava("ERRO: " & ex.ToString())
    End Sub

End Class
