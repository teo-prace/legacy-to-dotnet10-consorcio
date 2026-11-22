<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="BemEdicao.aspx.vb" Inherits="Cadastro_BemEdicao" Title="SGC - Bem" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server" Text="Novo bem" /></h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />

    <div class="quadro">
    <table class="form">
        <tr>
            <td class="rotulo">Descrição:</td>
            <td><asp:TextBox ID="txtDescricao" runat="server" CssClass="campo" Width="280px" MaxLength="100" /></td>
        </tr>
        <tr>
            <td class="rotulo">Tipo:</td>
            <td><asp:DropDownList ID="ddlTipo" runat="server" CssClass="campo">
                    <asp:ListItem Value="1" Text="Imóvel" />
                    <asp:ListItem Value="2" Text="Veículo leve" />
                    <asp:ListItem Value="3" Text="Veículo pesado" />
                    <asp:ListItem Value="4" Text="Serviços" />
                </asp:DropDownList></td>
        </tr>
        <tr>
            <td class="rotulo">Índice de reajuste:</td>
            <td><asp:DropDownList ID="ddlIndice" runat="server" CssClass="campo" /></td>
        </tr>
        <tr>
            <td class="rotulo">Valor atual:</td>
            <td><asp:TextBox ID="txtValor" runat="server" CssClass="campo" Width="110px" />
                <asp:Label ID="lblValorInfo" runat="server" Text="(no bem novo vira a primeira vigência)" ForeColor="#777" /></td>
        </tr>
        <tr>
            <td class="rotulo">Ativo:</td>
            <td><asp:CheckBox ID="chkAtivo" runat="server" Checked="true" /></td>
        </tr>
    </table>
    </div>
    <asp:Button ID="btnGravar" runat="server" Text="Gravar" CssClass="botaoPrincipal" />
    <asp:Button ID="btnVoltar" runat="server" Text="Voltar" CssClass="botao" />

    <asp:Panel ID="pnlHistorico" runat="server" Visible="false">
        <h2>Histórico de valores</h2>
        <div class="filtro">
            Nova vigência: <asp:TextBox ID="txtVigencia" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" />
            &nbsp; Valor: <asp:TextBox ID="txtNovoValor" runat="server" CssClass="campo" Width="100px" />
            &nbsp; Motivo: <asp:TextBox ID="txtMotivo" runat="server" CssClass="campo" Width="200px" MaxLength="100" />
            &nbsp; <asp:Button ID="btnIncluirValor" runat="server" Text="Incluir valor" CssClass="botao" />
        </div>
        <asp:GridView ID="gvHistorico" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt">
            <Columns>
                <asp:BoundField DataField="DT_VIGENCIA" HeaderText="Vigência" DataFormatString="{0:dd/MM/yyyy}" />
                <asp:BoundField DataField="VL_BEM" HeaderText="Valor" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="DS_MOTIVO" HeaderText="Motivo" />
                <asp:BoundField DataField="NM_USUARIO" HeaderText="Usuário" />
                <asp:BoundField DataField="DT_CADASTRO" HeaderText="Incluído em" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
</asp:Content>
