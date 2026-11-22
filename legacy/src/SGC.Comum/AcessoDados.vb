Imports System.Data
Imports System.Data.SqlClient
Imports System.Configuration

'==============================================================================
' Acesso a dados (estilo SqlHelper).
' 2011-04 M.S.
' 2014-02 J.P. ExecutaProcedureRetorno (valor de RETURN das procedures)
'==============================================================================
Public Class AcessoDados

    Public Shared ReadOnly Property StringConexao() As String
        Get
            Return ConfigurationManager.ConnectionStrings("SGC").ConnectionString
        End Get
    End Property

    Public Shared Function Parametro(ByVal nome As String, ByVal valor As Object) As SqlParameter
        If valor Is Nothing Then valor = DBNull.Value
        Return New SqlParameter(nome, valor)
    End Function

    Public Shared Function ParametroSaida(ByVal nome As String, ByVal tipo As SqlDbType) As SqlParameter
        Dim p As New SqlParameter(nome, tipo)
        p.Direction = ParameterDirection.Output
        Return p
    End Function

    Public Shared Function ExecutaDataTable(ByVal sql As String, ByVal ParamArray parametros() As SqlParameter) As DataTable
        Dim dt As New DataTable
        Using cn As New SqlConnection(StringConexao)
            Using cmd As New SqlCommand(sql, cn)
                cmd.CommandTimeout = 120
                If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using
        Return dt
    End Function

    Public Shared Function ExecutaNonQuery(ByVal sql As String, ByVal ParamArray parametros() As SqlParameter) As Integer
        Using cn As New SqlConnection(StringConexao)
            Using cmd As New SqlCommand(sql, cn)
                cmd.CommandTimeout = 120
                If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
                cn.Open()
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

    Public Shared Function ExecutaEscalar(ByVal sql As String, ByVal ParamArray parametros() As SqlParameter) As Object
        Using cn As New SqlConnection(StringConexao)
            Using cmd As New SqlCommand(sql, cn)
                cmd.CommandTimeout = 120
                If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
                cn.Open()
                Return cmd.ExecuteScalar()
            End Using
        End Using
    End Function

    Public Shared Function ExecutaProcedure(ByVal nome As String, ByVal ParamArray parametros() As SqlParameter) As DataTable
        Dim dt As New DataTable
        Using cn As New SqlConnection(StringConexao)
            Using cmd As New SqlCommand(nome, cn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.CommandTimeout = 300
                If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using
        Return dt
    End Function

    ' Procedure com mais de um resultado (2016-02 J.P.)
    Public Shared Function ExecutaProcedureDataSet(ByVal nome As String, ByVal ParamArray parametros() As SqlParameter) As DataSet
        Dim ds As New DataSet
        Using cn As New SqlConnection(StringConexao)
            Using cmd As New SqlCommand(nome, cn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.CommandTimeout = 300
                If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(ds)
                End Using
            End Using
        End Using
        Return ds
    End Function

    ' Executa a procedure e devolve o RETURN. Parametros de saida ficam
    ' preenchidos nos objetos SqlParameter passados.
    Public Shared Function ExecutaProcedureRetorno(ByVal nome As String, ByVal ParamArray parametros() As SqlParameter) As Integer
        Using cn As New SqlConnection(StringConexao)
            Using cmd As New SqlCommand(nome, cn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.CommandTimeout = 300
                If parametros IsNot Nothing Then cmd.Parameters.AddRange(parametros)
                Dim ret As New SqlParameter("@RETURN_VALUE", SqlDbType.Int)
                ret.Direction = ParameterDirection.ReturnValue
                cmd.Parameters.Add(ret)
                cn.Open()
                cmd.ExecuteNonQuery()
                Return CInt(ret.Value)
            End Using
        End Using
    End Function

End Class
