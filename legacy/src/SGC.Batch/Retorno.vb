Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports SGC.Comum

'==============================================================================
' Retorno bancario CNAB 400 - banco 999.
'
' Registro 0 (header):  77-79 banco, 95-100 data de gravacao DDMMAA
' Registro 1 (detalhe): 63-73 nosso numero, 109-110 ocorrencia (06 = liquidacao),
'                       111-116 data DDMMAA, 254-266 valor pago, 267-279 juros/multa
' Registro 9 (trailer)
'
' 2014-05 J.P. (script VBS)
' 2016-02 J.P. migrado para o SGC.Batch
'==============================================================================
Module Retorno

    Public Function Executa(ByVal pasta As String) As Integer
        If Not Directory.Exists(pasta) Then
            Escreve("Pasta de retorno nao encontrada: " & pasta)
            Return 1
        End If

        Dim processados As String = Path.Combine(pasta, "processados")
        If Not Directory.Exists(processados) Then Directory.CreateDirectory(processados)

        Dim arquivos() As String = Directory.GetFiles(pasta, "*.RET")
        Escreve(arquivos.Length & " arquivo(s) em " & pasta)

        Dim erros As Integer = 0
        For Each arquivo As String In arquivos
            Try
                ProcessaArquivo(arquivo)
                File.Move(arquivo, Path.Combine(processados, Path.GetFileName(arquivo)))
            Catch ex As Exception
                erros += 1
                Escreve("ERRO no arquivo " & Path.GetFileName(arquivo) & ": " & ex.Message)
                Log.Grava(ex)
            End Try
        Next

        Return IIf(erros = 0, 0, 1)
    End Function

    Private Sub ProcessaArquivo(ByVal arquivo As String)
        Dim nome As String = Path.GetFileName(arquivo).ToUpper()
        Escreve("Arquivo " & nome)

        If CInt(AcessoDados.ExecutaEscalar("SELECT COUNT(*) FROM TB_RETORNO_ARQUIVO WHERE NM_ARQUIVO = '" & nome & "'")) > 0 Then
            Throw New Exception("arquivo ja processado")
        End If

        Dim linhas() As String = File.ReadAllLines(arquivo)
        If linhas.Length < 2 OrElse Not linhas(0).StartsWith("0") Then
            Throw New Exception("arquivo sem header")
        End If
        If linhas(0).Substring(76, 3) <> "999" Then
            Throw New Exception("arquivo de outro banco (" & linhas(0).Substring(76, 3) & ")")
        End If

        Dim dtGravacao As Date = DataCNAB(linhas(0).Substring(94, 6))
        Dim cdArquivo As Integer = CInt(AcessoDados.ExecutaEscalar( _
            "INSERT INTO TB_RETORNO_ARQUIVO (NM_ARQUIVO, DT_GRAVACAO) VALUES (@NM, @DT); SELECT @@IDENTITY", _
            AcessoDados.Parametro("@NM", nome), AcessoDados.Parametro("@DT", dtGravacao)))

        Dim qtRegistros As Integer = 0, qtBaixados As Integer = 0, qtRejeitados As Integer = 0
        Dim total As Decimal = 0

        For i As Integer = 1 To linhas.Length - 1
            Dim l As String = linhas(i)
            If l.Length = 0 OrElse l(0) <> "1"c Then Continue For
            If l.Length < 279 Then l = l.PadRight(400)
            qtRegistros += 1

            Dim nossoNumero As String = l.Substring(62, 11)
            Dim ocorrencia As String = l.Substring(108, 2)
            Dim dtOcorrencia As Date = DataCNAB(l.Substring(110, 6))
            Dim vlPago As Decimal = CDec(l.Substring(253, 13)) / 100D
            Dim vlJuros As Decimal = CDec(l.Substring(266, 13)) / 100D

            Dim situacao As Integer
            Dim motivo As String = ""
            Dim cdParcela As Object = DBNull.Value

            If ocorrencia <> "06" Then
                situacao = 3
                motivo = "Ocorrencia " & ocorrencia & " ignorada"
            Else
                Dim dt As DataTable = AcessoDados.ExecutaDataTable( _
                    "SELECT CD_PARCELA, VL_PARCELA, DT_VENCIMENTO FROM TB_PARCELA WHERE NR_NOSSO_NUMERO = '" & nossoNumero & "'")
                If dt.Rows.Count = 0 Then
                    situacao = 2
                    motivo = "Nosso numero nao encontrado"
                Else
                    cdParcela = dt.Rows(0)("CD_PARCELA")
                    Dim valor As Decimal = CDec(dt.Rows(0)("VL_PARCELA"))
                    Dim venc As Date = CDate(dt.Rows(0)("DT_VENCIMENTO"))
                    Dim multa As Decimal = Encargos.Multa(valor, venc, dtOcorrencia)
                    Dim juros As Decimal = Encargos.Juros(valor, venc, dtOcorrencia)

                    Try
                        Dim cdDetalhe As Integer = InsereDetalhe(cdArquivo, i + 1, nossoNumero, ocorrencia, dtOcorrencia, vlPago, vlJuros, cdParcela, 1, "")
                        AcessoDados.ExecutaProcedureRetorno("SP_BAIXA_PARCELA", _
                            AcessoDados.Parametro("@CD_PARCELA", cdParcela), _
                            AcessoDados.Parametro("@DT_PAGAMENTO", dtOcorrencia), _
                            AcessoDados.Parametro("@VL_PAGO", vlPago), _
                            AcessoDados.Parametro("@VL_MULTA", multa), _
                            AcessoDados.Parametro("@VL_JUROS", juros), _
                            AcessoDados.Parametro("@ID_ORIGEM", 2), _
                            AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario), _
                            AcessoDados.Parametro("@CD_RETORNO_DETALHE", cdDetalhe))
                        qtBaixados += 1
                        total += vlPago
                        Continue For
                    Catch ex As SqlException
                        ' desfaz o detalhe "baixado" e registra como rejeitado
                        AcessoDados.ExecutaNonQuery("DELETE FROM TB_RETORNO_DETALHE WHERE CD_RETORNO_ARQUIVO = " & cdArquivo & " AND NR_LINHA = " & (i + 1))
                        situacao = 2
                        motivo = ex.Message
                    End Try
                End If
            End If

            If situacao = 2 Then qtRejeitados += 1
            InsereDetalhe(cdArquivo, i + 1, nossoNumero, ocorrencia, dtOcorrencia, vlPago, vlJuros, cdParcela, situacao, motivo)
        Next

        AcessoDados.ExecutaNonQuery("UPDATE TB_RETORNO_ARQUIVO SET QT_REGISTROS = " & qtRegistros & ", QT_BAIXADOS = " & qtBaixados & _
                                    ", QT_REJEITADOS = " & qtRejeitados & ", VL_TOTAL_BAIXADO = @VL WHERE CD_RETORNO_ARQUIVO = " & cdArquivo, _
                                    AcessoDados.Parametro("@VL", total))

        Escreve("  " & qtRegistros & " registros: " & qtBaixados & " baixados (R$ " & total.ToString("#,##0.00") & "), " & qtRejeitados & " rejeitados")
    End Sub

    Private Function InsereDetalhe(ByVal cdArquivo As Integer, ByVal linha As Integer, ByVal nossoNumero As String, ByVal ocorrencia As String, _
                                   ByVal data As Date, ByVal valor As Decimal, ByVal juros As Decimal, ByVal cdParcela As Object, _
                                   ByVal situacao As Integer, ByVal motivo As String) As Integer
        Return CInt(AcessoDados.ExecutaEscalar( _
            "INSERT INTO TB_RETORNO_DETALHE (CD_RETORNO_ARQUIVO, NR_LINHA, NR_NOSSO_NUMERO, CD_OCORRENCIA, DT_OCORRENCIA, VL_PAGO, VL_JUROS_MULTA, CD_PARCELA, ID_SITUACAO, DS_MOTIVO) " & _
            "VALUES (@ARQ, @LIN, @NN, @OC, @DT, @VL, @JU, @PAR, @SIT, @MOT); SELECT @@IDENTITY", _
            AcessoDados.Parametro("@ARQ", cdArquivo), AcessoDados.Parametro("@LIN", linha), AcessoDados.Parametro("@NN", nossoNumero), _
            AcessoDados.Parametro("@OC", ocorrencia), AcessoDados.Parametro("@DT", data), AcessoDados.Parametro("@VL", valor), _
            AcessoDados.Parametro("@JU", juros), AcessoDados.Parametro("@PAR", cdParcela), AcessoDados.Parametro("@SIT", situacao), _
            AcessoDados.Parametro("@MOT", IIf(motivo = "", DBNull.Value, motivo))))
    End Function

    ' DDMMAA -> data (ano 2000+)
    Private Function DataCNAB(ByVal texto As String) As Date
        Return New Date(2000 + CInt(texto.Substring(4, 2)), CInt(texto.Substring(2, 2)), CInt(texto.Substring(0, 2)))
    End Function

End Module
