<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="PontosVenda.aspx.vb" Inherits="Cadastro_PontosVenda" Title="SGC - Pontos de venda" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Pontos de venda e vendedores</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />

    <table cellpadding="0" cellspacing="0"><tr>
    <td style="vertical-align: top; padding-right: 20px;">
        <h2>Pontos de venda</h2>
        <asp:GridView ID="gvPontos" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
            DataKeyNames="CD_PONTO_VENDA" SelectedRowStyle-BackColor="#ffe8a1">
            <Columns>
                <asp:ButtonField CommandName="Select" Text="Selecionar" />
                <asp:BoundField DataField="NM_PONTO_VENDA" HeaderText="Ponto de venda" />
                <asp:BoundField DataField="NM_CIDADE" HeaderText="Cidade" />
                <asp:BoundField DataField="SG_UF" HeaderText="UF" />
                <asp:BoundField DataField="QT_VENDEDORES" HeaderText="Vendedores" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="FL_ATIVO" HeaderText="Ativo" ItemStyle-CssClass="centro" />
            </Columns>
        </asp:GridView>
        <div class="filtro" style="margin-top: 8px;">
            Novo: <asp:TextBox ID="txtNomePonto" runat="server" CssClass="campo" Width="160px" MaxLength="80" />
            Cidade: <asp:TextBox ID="txtCidadePonto" runat="server" CssClass="campo" Width="110px" MaxLength="60" />
            <asp:DropDownList ID="ddlUFPonto" runat="server" CssClass="campo" />
            <asp:Button ID="btnIncluirPonto" runat="server" Text="Incluir" CssClass="botao" />
        </div>
    </td>
    <td style="vertical-align: top;">
        <asp:Panel ID="pnlVendedores" runat="server" Visible="false">
            <h2>Vendedores - <asp:Literal ID="litPonto" runat="server" /></h2>
            <asp:GridView ID="gvVendedores" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
                DataKeyNames="CD_VENDEDOR" EmptyDataText="Nenhum vendedor.">
                <Columns>
                    <asp:BoundField DataField="NM_VENDEDOR" HeaderText="Vendedor" />
                    <asp:BoundField DataField="QT_PROPOSTAS" HeaderText="Propostas" ItemStyle-CssClass="num" />
                    <asp:BoundField DataField="FL_ATIVO" HeaderText="Ativo" ItemStyle-CssClass="centro" />
                    <asp:ButtonField CommandName="AtivaInativa" Text="Ativar/inativar" />
                </Columns>
            </asp:GridView>
            <div class="filtro" style="margin-top: 8px;">
                Novo vendedor: <asp:TextBox ID="txtNomeVendedor" runat="server" CssClass="campo" Width="200px" MaxLength="100" />
                <asp:Button ID="btnIncluirVendedor" runat="server" Text="Incluir" CssClass="botao" />
            </div>
        </asp:Panel>
    </td>
    </tr></table>
</asp:Content>
