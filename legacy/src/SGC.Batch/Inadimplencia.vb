Imports System.Data
Imports SGC.Comum

'==============================================================================
' Exclusao de cotas inadimplentes (diario) e relacao de contempladas
' inadimplentes para a cobranca juridica.
' 2016-02 J.P.
'==============================================================================
Module Inadimplencia

    Public Function Executa(ByVal referencia As Date) As Integer
        Escreve("Inadimplencia - referencia " & referencia.ToString("dd/MM/yyyy"))

        Dim ds As DataSet = AcessoDados.ExecutaProcedureDataSet("SP_EXCLUI_INADIMPLENTES", _
            AcessoDados.Parametro("@DT_REFERENCIA", referencia), _
            AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))

        Escreve("Cotas excluidas: " & ds.Tables(0).Rows.Count)
        For Each r As DataRow In ds.Tables(0).Rows
            Escreve("  " & r("NR_GRUPO") & "/" & CInt(r("NR_COTA")).ToString("000") & "-" & r("NR_VERSAO") & " " & r("NM_CONSORCIADO") & _
                    " (" & r("QT_VENCIDAS") & " vencidas, " & CDec(r("PC_AMORTIZADO")).ToString("0.0000") & "% pago)")
        Next

        Escreve("Contempladas inadimplentes (cobranca juridica): " & ds.Tables(1).Rows.Count)
        For Each r As DataRow In ds.Tables(1).Rows
            Escreve("  " & r("NR_GRUPO") & "/" & CInt(r("NR_COTA")).ToString("000") & "-" & r("NR_VERSAO") & " " & r("NM_CONSORCIADO") & _
                    " - " & r("QT_VENCIDAS") & " vencida(s), R$ " & CDec(r("VL_ABERTO")).ToString("#,##0.00"))
        Next
        Return 0
    End Function

End Module
