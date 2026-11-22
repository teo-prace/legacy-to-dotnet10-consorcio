<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Indices.aspx.vb" Inherits="Cadastro_Indices" Title="SGC - Índices" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Índices de reajuste</h1>
    <div class="aviso">Consulta. A inclusão de índices é feita no <b>SGC Desktop</b> (Tabelas &gt; Índices).</div>
    <div class="filtro">
        Índice: <asp:DropDownList ID="ddlIndice" runat="server" CssClass="campo" AutoPostBack="true" />
        &nbsp; Ano: <asp:DropDownList ID="ddlAno" runat="server" CssClass="campo" AutoPostBack="true" />
    </div>

    <asp:GridView ID="gvValores" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" ShowFooter="true">
        <Columns>
            <asp:BoundField DataField="DS_MES" HeaderText="Mês" />
            <asp:BoundField DataField="PC_VARIACAO" HeaderText="Variação %" DataFormatString="{0:0.0000}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="PC_ACUMULADO_12M" HeaderText="Acumulado 12 meses %" DataFormatString="{0:0.0000}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DT_CADASTRO" HeaderText="Cadastrado em" DataFormatString="{0:dd/MM/yyyy}" />
        </Columns>
        <FooterStyle CssClass="rodapeGrid" />
    </asp:GridView>
</asp:Content>
