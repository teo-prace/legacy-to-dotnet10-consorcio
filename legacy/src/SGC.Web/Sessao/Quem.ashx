<%@ WebHandler Language="VB" Class="SessaoQuem" %>
' ==============================================================================
' Ponte de sessao para o SGC.Novo (migracao .NET 10).
' Devolve quem esta logado na sessao do legado. So responde para a fachada:
' exige o cabecalho X-SGC-Ponte com a chave do web.config (PonteSessaoChave).
' Somente leitura (IReadOnlySessionState): nao trava a sessao e renova o
' tempo de expiracao, como qualquer requisicao do usuario.
' ==============================================================================
Imports System.Data
Imports System.Web
Imports System.Web.SessionState
Imports System.Web.Script.Serialization
Imports System.Configuration

Public Class SessaoQuem
    Implements IHttpHandler, IReadOnlySessionState

    Public Sub ProcessRequest(ByVal ctx As HttpContext) Implements IHttpHandler.ProcessRequest
        ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        ctx.Response.ContentType = "application/json"

        Dim chave As String = ConfigurationManager.AppSettings("PonteSessaoChave")
        If String.IsNullOrEmpty(chave) OrElse Not IgualConstante(ctx.Request.Headers("X-SGC-Ponte"), chave) Then
            ctx.Response.StatusCode = 404
            Return
        End If

        Dim u As DataRow = TryCast(ctx.Session("Usuario"), DataRow)
        If u Is Nothing Then
            ctx.Response.StatusCode = 401
            ctx.Response.Write("{}")
            Return
        End If

        Dim dados As New Dictionary(Of String, Object)
        dados("codigo") = CInt(u("CD_USUARIO"))
        dados("login") = u("DS_LOGIN").ToString()
        dados("nome") = u("NM_USUARIO").ToString()
        dados("perfil") = CInt(u("ID_PERFIL"))
        dados("expiraEmMinutos") = ctx.Session.Timeout
        ctx.Response.Write(New JavaScriptSerializer().Serialize(dados))
    End Sub

    Private Shared Function IgualConstante(ByVal a As String, ByVal b As String) As Boolean
        If a Is Nothing OrElse a.Length <> b.Length Then Return False
        Dim dif As Integer = 0
        For i As Integer = 0 To a.Length - 1
            dif = dif Or (AscW(a(i)) Xor AscW(b(i)))
        Next
        Return dif = 0
    End Function

    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property

End Class
