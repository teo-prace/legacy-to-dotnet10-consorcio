Imports System.Data
Imports SGC.Comum

'==============================================================================
' Emissao mensal da cobranca (chama SP_EMITE_COBRANCA).
' 2016-02 J.P.
'==============================================================================
Module Emissao

    Public Function Executa(ByVal anoMes As Integer) As Integer
        Escreve("Emissao da cobranca - vencimentos de " & (anoMes Mod 100).ToString("00") & "/" & (anoMes \ 100))

        Dim dt As DataTable = AcessoDados.ExecutaProcedure("SP_EMITE_COBRANCA", _
            AcessoDados.Parametro("@NR_ANO_MES", anoMes), _
            AcessoDados.Parametro("@DT_EMISSAO", Date.Now), _
            AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))

        Dim total As Integer = 0
        Dim valor As Decimal = 0
        For Each r As DataRow In dt.Rows
            Escreve("  Grupo " & r("NR_GRUPO") & ": " & r("QT_PARCELAS") & " parcelas, R$ " & CDec(r("VL_TOTAL")).ToString("#,##0.00"))
            total += CInt(r("QT_PARCELAS"))
            valor += CDec(r("VL_TOTAL"))
        Next
        Escreve("Total: " & total & " parcelas, R$ " & valor.ToString("#,##0.00"))
        Return 0
    End Function

End Module
