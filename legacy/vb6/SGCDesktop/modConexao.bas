Attribute VB_Name = "modConexao"
'==============================================================================
' SGC Desktop - conexao com o banco
' 2005-03 R.A.
' 2009-06 R.A. le servidor do SGC.INI
'==============================================================================
Option Explicit

Public cn As ADODB.Connection

Public Function Conecta() As Boolean
    On Error GoTo Erro

    Dim servidor As String
    servidor = LeIni("BANCO", "SERVIDOR", "SRVSQL01")

    Set cn = New ADODB.Connection
    cn.ConnectionString = "Provider=SQLOLEDB.1;Integrated Security=SSPI;Persist Security Info=False;" & _
                          "Initial Catalog=SGC;Data Source=" & servidor
    cn.CommandTimeout = 300
    cn.Open
    Conecta = True
    Exit Function

Erro:
    MsgBox "Nao foi possivel conectar ao banco de dados." & vbCrLf & Err.Description, vbCritical, "SGC"
    Conecta = False
End Function

Public Function AbreRecordset(ByVal sql As String) As ADODB.Recordset
    Dim rs As ADODB.Recordset
    Set rs = New ADODB.Recordset
    rs.CursorLocation = adUseClient
    rs.Open sql, cn, adOpenStatic, adLockReadOnly
    Set AbreRecordset = rs
End Function

Public Sub Executa(ByVal sql As String)
    cn.Execute sql, , adExecuteNoRecords
End Sub
