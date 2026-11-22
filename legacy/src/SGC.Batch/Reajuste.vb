Imports System.Data
Imports SGC.Comum

'==============================================================================
' Reajuste anual dos bens (rodar em janeiro, depois do cadastro do indice de
' dezembro no SGC Desktop).
' 2016-02 J.P.
'==============================================================================
Module Reajuste

    Public Function Executa(ByVal anoMes As Integer) As Integer
        Escreve("Reajuste dos bens - vigencia 01/" & (anoMes Mod 100).ToString("00") & "/" & (anoMes \ 100))

        Dim dt As DataTable = AcessoDados.ExecutaProcedure("SP_REAJUSTA_BENS", _
            AcessoDados.Parametro("@NR_ANO_MES", anoMes), _
            AcessoDados.Parametro("@CD_USUARIO", CodigoUsuario))

        Dim pendentes As Integer = 0
        For Each r As DataRow In dt.Rows
            Dim linha As String = "  " & r("DS_BEM").ToString().PadRight(30) & " " & r("SG_INDICE").ToString().PadRight(7) & " " & _
                                  CDec(r("VL_ANTERIOR")).ToString("#,##0.00").PadLeft(12)
            If Not IsDBNull(r("VL_NOVO")) Then
                linha &= " -> " & CDec(r("VL_NOVO")).ToString("#,##0.00").PadLeft(12) & " (+" & CDec(r("PC_REAJUSTE")).ToString("0.0000") & "%)"
            Else
                linha &= "    " & r("DS_SITUACAO")
                If r("DS_SITUACAO").ToString().StartsWith("Faltam") Then pendentes += 1
            End If
            Escreve(linha)
        Next

        If pendentes > 0 Then
            Escreve("ATENCAO: " & pendentes & " bem(ns) sem reajuste por falta de indice.")
            Return 1
        End If
        Return 0
    End Function

End Module
