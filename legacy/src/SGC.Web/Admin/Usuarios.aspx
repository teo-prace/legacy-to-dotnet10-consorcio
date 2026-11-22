<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Usuarios.aspx.vb" Inherits="Admin_Usuarios" Title="SGC - Usuários" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Usuários</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />

    <asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" DataKeyNames="CD_USUARIO">
        <Columns>
            <asp:ButtonField CommandName="Editar" Text="Editar" />
            <asp:BoundField DataField="DS_LOGIN" HeaderText="Login" />
            <asp:BoundField DataField="NM_USUARIO" HeaderText="Nome" />
            <asp:BoundField DataField="DS_EMAIL" HeaderText="E-mail" />
            <asp:TemplateField HeaderText="Perfil">
                <ItemTemplate><%# DescricaoPerfil(Eval("ID_PERFIL")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="FL_ATIVO" HeaderText="Ativo" ItemStyle-CssClass="centro" />
            <asp:BoundField DataField="DT_ULTIMO_ACESSO" HeaderText="Último acesso" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
        </Columns>
    </asp:GridView>

    <h2><asp:Literal ID="litForm" runat="server" Text="Novo usuário" /></h2>
    <div class="quadro">
        <table class="form">
            <tr>
                <td class="rotulo">Login:</td>
                <td><asp:TextBox ID="txtLogin" runat="server" CssClass="campo" Width="120px" MaxLength="30" /></td>
                <td class="rotulo">Nome:</td>
                <td><asp:TextBox ID="txtNome" runat="server" CssClass="campo" Width="220px" MaxLength="100" /></td>
            </tr>
            <tr>
                <td class="rotulo">E-mail:</td>
                <td><asp:TextBox ID="txtEmail" runat="server" CssClass="campo" Width="200px" MaxLength="100" /></td>
                <td class="rotulo">Perfil:</td>
                <td><asp:DropDownList ID="ddlPerfil" runat="server" CssClass="campo">
                        <asp:ListItem Value="2" Text="Operador" />
                        <asp:ListItem Value="3" Text="Consulta" />
                        <asp:ListItem Value="1" Text="Administrador" />
                    </asp:DropDownList></td>
            </tr>
            <tr>
                <td class="rotulo">Ativo:</td>
                <td><asp:CheckBox ID="chkAtivo" runat="server" Checked="true" /></td>
                <td class="rotulo">Senha:</td>
                <td><asp:CheckBox ID="chkResetSenha" runat="server" Text="Redefinir para a senha padrão" /></td>
            </tr>
        </table>
    </div>
    <asp:Button ID="btnGravar" runat="server" Text="Gravar" CssClass="botaoPrincipal" />
    <asp:Button ID="btnLimpar" runat="server" Text="Limpar" CssClass="botao" />
</asp:Content>
