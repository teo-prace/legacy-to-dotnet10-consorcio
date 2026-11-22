<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Login.aspx.vb" Inherits="Login" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SGC - Acesso</title>
    <link href="~/css/estilo.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server" defaultbutton="btnEntrar">
    <table class="topo" cellpadding="0" cellspacing="0">
        <tr><td class="topoLogo">SGC <span>CeCons Consórcios</span></td></tr>
    </table>
    <div style="width: 320px; margin: 80px auto; background: #fff; border: 1px solid #c5ced8; padding: 20px;">
        <h1>Acesso ao sistema</h1>
        <asp:Label ID="lblMensagem" runat="server" Visible="false" />
        <table class="form">
            <tr>
                <td class="rotulo" style="width: 60px">Usuário:</td>
                <td><asp:TextBox ID="txtLogin" runat="server" CssClass="campo" Width="160px" MaxLength="30" /></td>
            </tr>
            <tr>
                <td class="rotulo" style="width: 60px">Senha:</td>
                <td><asp:TextBox ID="txtSenha" runat="server" CssClass="campo" Width="160px" TextMode="Password" /></td>
            </tr>
            <tr>
                <td></td>
                <td><asp:Button ID="btnEntrar" runat="server" Text="Entrar" CssClass="botaoPrincipal" /></td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
