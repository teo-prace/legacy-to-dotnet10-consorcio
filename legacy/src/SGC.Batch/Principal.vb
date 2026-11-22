Imports SGC.Comum

'==============================================================================
' SGC.Batch - processos noturnos (agendador de tarefas do servidor)
'
'   SGC.Batch EMISSAO [AAAAMM]     emissao da cobranca (padrao: mes atual)
'   SGC.Batch RETORNO [pasta]      processa arquivos .RET (padrao: DS_PASTA_RETORNO)
'   SGC.Batch INADIMPLENCIA [data] exclusao de inadimplentes (padrao: hoje)
'   SGC.Batch REAJUSTE AAAAMM      reajuste anual dos bens
'
' Codigo de saida: 0 ok, 1 erro, 2 parametros invalidos.
' 2016-02 J.P.  substitui os jobs do SQL Agent e os scripts VBS
'==============================================================================
Module Principal

    Public CodigoUsuario As Integer = 1

    Function Main(ByVal args() As String) As Integer
        Escreve("SGC.Batch " & Constantes.VERSAO & " - " & Date.Now.ToString("dd/MM/yyyy HH:mm:ss"))

        If args.Length = 0 Then
            Ajuda()
            Return 2
        End If

        Dim cfg As String = System.Configuration.ConfigurationManager.AppSettings("CodigoUsuarioBatch")
        If cfg <> "" Then CodigoUsuario = CInt(cfg)

        Try
            Select Case args(0).ToUpper()
                Case "EMISSAO"
                    Dim anoMes As Integer
                    If args.Length > 1 Then
                        anoMes = CInt(args(1))
                    Else
                        anoMes = Date.Today.Year * 100 + Date.Today.Month
                    End If
                    Return Emissao.Executa(anoMes)

                Case "RETORNO"
                    Dim pasta As String
                    If args.Length > 1 Then
                        pasta = args(1)
                    Else
                        pasta = Parametros.Le("DS_PASTA_RETORNO")
                    End If
                    Return Retorno.Executa(pasta)

                Case "INADIMPLENCIA"
                    Dim referencia As Date = Date.Now
                    If args.Length > 1 Then referencia = Date.ParseExact(args(1), "yyyyMMdd", Nothing).AddHours(23)
                    Return Inadimplencia.Executa(referencia)

                Case "REAJUSTE"
                    If args.Length < 2 Then
                        Ajuda()
                        Return 2
                    End If
                    Return Reajuste.Executa(CInt(args(1)))

                Case Else
                    Ajuda()
                    Return 2
            End Select
        Catch ex As Exception
            Escreve("ERRO: " & ex.Message)
            Log.Grava(ex)
            Return 1
        End Try
    End Function

    Public Sub Escreve(ByVal texto As String)
        Console.WriteLine(texto)
        Log.Grava("[BATCH] " & texto)
    End Sub

    Private Sub Ajuda()
        Console.WriteLine("Uso:")
        Console.WriteLine("  SGC.Batch EMISSAO [AAAAMM]")
        Console.WriteLine("  SGC.Batch RETORNO [pasta]")
        Console.WriteLine("  SGC.Batch INADIMPLENCIA [AAAAMMDD]")
        Console.WriteLine("  SGC.Batch REAJUSTE AAAAMM")
    End Sub

End Module
