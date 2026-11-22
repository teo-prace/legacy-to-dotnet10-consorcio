'==============================================================================
' Constantes do sistema. Mesmos codigos de TB_DOMINIO.
' 2011-04 M.S.
'==============================================================================
Public Enum SituacaoGrupo
    EmFormacao = 1
    EmAndamento = 2
    Encerrado = 3
    Cancelado = 4
End Enum

Public Enum SituacaoCota
    Ativa = 1
    Contemplada = 2
    Excluida = 3
    Desistente = 4
    Quitada = 5
    Cancelada = 6
End Enum

Public Enum SituacaoProposta
    Digitada = 1
    AguardandoPagamento = 2
    Efetivada = 3
    Cancelada = 4
    Arrependimento = 5
End Enum

Public Enum SituacaoParcela
    Aberta = 1
    Emitida = 2
    Paga = 3
    Cancelada = 4
End Enum

Public Enum Perfil
    Administrador = 1
    Operador = 2
    Consulta = 3
End Enum

Public Class Constantes
    Public Const NOME_SISTEMA As String = "SGC - Sistema de Gestão de Consórcios"
    Public Const VERSAO As String = "4.2"
    Public Const SENHA_PADRAO As String = "cecons123"
End Class
