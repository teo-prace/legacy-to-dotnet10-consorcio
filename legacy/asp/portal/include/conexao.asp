<%
'==============================================================================
' Portal do Consorciado - conexao com o banco
' 2007-08 R.A.
' 2019-08 J.P. driver ODBC 17 (TLS 1.2); antes: Provider=SQLOLEDB
'==============================================================================
Const STR_CONEXAO = "Driver={ODBC Driver 17 for SQL Server};Server=(localdb)\MSSQLLocalDB;Database=SGC;Trusted_Connection=Yes;"
'Const STR_CONEXAO = "Driver={ODBC Driver 17 for SQL Server};Server=SRVSQL01;Database=SGC;Uid=portal;Pwd=portal2007;"

Dim conn

Sub AbreConexao()
    Set conn = Server.CreateObject("ADODB.Connection")
    conn.Open STR_CONEXAO
End Sub

Sub FechaConexao()
    On Error Resume Next
    conn.Close
    Set conn = Nothing
End Sub

Function AbreRS(sql)
    Dim rs
    Set rs = Server.CreateObject("ADODB.Recordset")
    rs.Open sql, conn, 3, 1   ' adOpenStatic, adLockReadOnly
    Set AbreRS = rs
End Function
%>
