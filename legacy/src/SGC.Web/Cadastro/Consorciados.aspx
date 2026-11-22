<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Consorciados.aspx.vb" Inherits="Cadastro_Consorciados" Title="SGC - Consorciados" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Consorciados</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <asp:Panel ID="pnlFiltro" runat="server" CssClass="filtro" DefaultButton="btnPesquisar">
        Nome: <asp:TextBox ID="txtNome" runat="server" CssClass="campo" Width="220px" />
        &nbsp; CPF: <asp:TextBox ID="txtCPF" runat="server" CssClass="campo" Width="110px" onkeyup="mascaraCPF(this)" />
        &nbsp; Cidade: <asp:TextBox ID="txtCidade" runat="server" CssClass="campo" Width="120px" />
        &nbsp; <asp:Button ID="btnPesquisar" runat="server" Text="Pesquisar" CssClass="botao" />
        &nbsp; <asp:Button ID="btnNovo" runat="server" Text="Novo consorciado" CssClass="botao" />
    </asp:Panel>

    <asp:GridView ID="gvConsorciados" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
        AllowPaging="true" PageSize="25" EmptyDataText="Nenhum consorciado encontrado.">
        <Columns>
            <asp:TemplateField HeaderText="CPF">
                <ItemTemplate><%# Formatacao.FormataCPF(Eval("NR_CPF")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:HyperLinkField DataTextField="NM_CONSORCIADO" HeaderText="Nome" DataNavigateUrlFields="CD_CONSORCIADO" DataNavigateUrlFormatString="ConsorciadoEdicao.aspx?id={0}" />
            <asp:BoundField DataField="NM_CIDADE" HeaderText="Cidade" />
            <asp:BoundField DataField="SG_UF" HeaderText="UF" />
            <asp:BoundField DataField="NR_CELULAR" HeaderText="Celular" />
            <asp:BoundField DataField="QT_COTAS" HeaderText="Cotas" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="FL_ATIVO" HeaderText="Ativo" ItemStyle-CssClass="centro" />
        </Columns>
        <PagerStyle CssClass="rodapeGrid" />
    </asp:GridView>
    <asp:Label ID="lblTotal" runat="server" />
</asp:Content>
