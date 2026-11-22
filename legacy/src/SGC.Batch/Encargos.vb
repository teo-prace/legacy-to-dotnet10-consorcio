'==============================================================================
' Multa e juros para o retorno bancario.
' Regra do regulamento: 2% de multa e 1% ao mes pro rata, apos o vencimento;
' pagamento no primeiro dia util seguinte ao vencimento nao tem encargos.
' 2016-02 J.P. (copiado da tela de baixa)
'==============================================================================
Module Encargos

    Private Const PC_MULTA As Decimal = 2D
    Private Const PC_JUROS_MES As Decimal = 1D

    Public Function DiaUtil(ByVal data As Date) As Date
        Dim d As Date = data.Date
        While d.DayOfWeek = DayOfWeek.Saturday Or d.DayOfWeek = DayOfWeek.Sunday
            d = d.AddDays(1)
        End While
        Return d
    End Function

    Public Function Multa(ByVal valor As Decimal, ByVal vencimento As Date, ByVal pagamento As Date) As Decimal
        If pagamento.Date <= DiaUtil(vencimento) Then Return 0
        Return Math.Round(valor * PC_MULTA / 100D, 2, MidpointRounding.AwayFromZero)
    End Function

    Public Function Juros(ByVal valor As Decimal, ByVal vencimento As Date, ByVal pagamento As Date) As Decimal
        If pagamento.Date <= DiaUtil(vencimento) Then Return 0
        Dim dias As Integer = CInt((pagamento.Date - vencimento.Date).TotalDays)
        Return Math.Round(valor * PC_JUROS_MES / 100D / 30D * dias, 2, MidpointRounding.AwayFromZero)
    End Function

End Module
