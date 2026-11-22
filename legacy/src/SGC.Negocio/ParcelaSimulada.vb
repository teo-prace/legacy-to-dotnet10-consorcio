'==============================================================================
' Linha da simulacao de parcelas.
' 2011-05 M.S.
'==============================================================================
Public Class ParcelaSimulada

    Private _numero As Integer
    Private _pcFundoComum As Decimal
    Private _pcTaxaAdm As Decimal
    Private _pcFundoReserva As Decimal
    Private _valor As Decimal

    Public Property Numero() As Integer
        Get
            Return _numero
        End Get
        Set(ByVal value As Integer)
            _numero = value
        End Set
    End Property

    Public Property PercentualFundoComum() As Decimal
        Get
            Return _pcFundoComum
        End Get
        Set(ByVal value As Decimal)
            _pcFundoComum = value
        End Set
    End Property

    Public Property PercentualTaxaAdm() As Decimal
        Get
            Return _pcTaxaAdm
        End Get
        Set(ByVal value As Decimal)
            _pcTaxaAdm = value
        End Set
    End Property

    Public Property PercentualFundoReserva() As Decimal
        Get
            Return _pcFundoReserva
        End Get
        Set(ByVal value As Decimal)
            _pcFundoReserva = value
        End Set
    End Property

    Public ReadOnly Property PercentualTotal() As Decimal
        Get
            Return _pcFundoComum + _pcTaxaAdm + _pcFundoReserva
        End Get
    End Property

    Public Property Valor() As Decimal
        Get
            Return _valor
        End Get
        Set(ByVal value As Decimal)
            _valor = value
        End Set
    End Property

End Class
