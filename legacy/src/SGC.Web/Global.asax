<%@ Application Language="VB" %>
<%@ Import Namespace="SGC.Comum" %>

<script runat="server">

    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        Log.Grava("Aplicacao iniciada")
    End Sub

    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
        Dim ex As Exception = Server.GetLastError()
        If ex IsNot Nothing Then
            Log.Grava(Request.RawUrl & " - " & ex.ToString())
        End If
    End Sub

    Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
    End Sub

</script>
