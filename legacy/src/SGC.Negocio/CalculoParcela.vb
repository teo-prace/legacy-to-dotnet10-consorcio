'==============================================================================
' Calculo de parcelas para simulacao e proposta.
' Mesma regra da SP_GERA_PLANO_PARCELAS (percentual do bem, 6 casas).
'
' 2011-05 M.S.  versao inicial (simulacao da retaguarda web)
' 2011-06 M.S.  ValorPrimeiraParcela para a proposta
'==============================================================================
Public Class CalculoParcela

    Public Shared Function Simula(ByVal valorBem As Decimal, ByVal prazo As Integer, ByVal taxaAdm As Decimal, _
                                  ByVal antecipacao As Decimal, ByVal fundoReserva As Decimal) As List(Of ParcelaSimulada)
        Dim lista As New List(Of ParcelaSimulada)

        Dim pcFC As Decimal = Math.Round(100D / prazo, 6)
        Dim pcTA As Decimal = Math.Round((taxaAdm - antecipacao) / prazo, 6)
        Dim pcFR As Decimal = Math.Round(fundoReserva / prazo, 6)

        For i As Integer = 1 To prazo
            Dim p As New ParcelaSimulada
            p.Numero = i
            p.PercentualFundoComum = pcFC
            p.PercentualTaxaAdm = pcTA
            If i = 1 Then p.PercentualTaxaAdm = pcTA + antecipacao
            p.PercentualFundoReserva = pcFR
            p.Valor = Math.Round(valorBem * p.PercentualTotal / 100D, 2)
            lista.Add(p)
        Next

        Return lista
    End Function

    Public Shared Function PercentualPrimeiraParcela(ByVal prazo As Integer, ByVal taxaAdm As Decimal, _
                                                     ByVal antecipacao As Decimal, ByVal fundoReserva As Decimal) As Decimal
        Return Math.Round(100D / prazo, 6) + Math.Round((taxaAdm - antecipacao) / prazo, 6) + antecipacao + Math.Round(fundoReserva / prazo, 6)
    End Function

    Public Shared Function ValorPrimeiraParcela(ByVal valorBem As Decimal, ByVal prazo As Integer, ByVal taxaAdm As Decimal, _
                                                ByVal antecipacao As Decimal, ByVal fundoReserva As Decimal) As Decimal
        Return Math.Round(valorBem * PercentualPrimeiraParcela(prazo, taxaAdm, antecipacao, fundoReserva) / 100D, 2)
    End Function

End Class
