<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Propostas.aspx.vb" Inherits="Comercial_Propostas" Title="SGC - Propostas" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Propostas de adesão</h1>
    <asp:Panel ID="pnlFiltro" runat="server" CssClass="filtro" DefaultButton="btnPesquisar">
        Nº: <asp:TextBox ID="txtNumero" runat="server" CssClass="campo" Width="90px" />
        &nbsp; CPF: <asp:TextBox ID="txtCPF" runat="server" CssClass="campo" Width="110px" onkeyup="mascaraCPF(this)" />
        &nbsp; Grupo: <asp:DropDownList ID="ddlGrupo" runat="server" CssClass="campo" />
        &nbsp; Situação:
        <asp:DropDownList ID="ddlSituacao" runat="server" CssClass="campo">
            <asp:ListItem Value="" Text="(todas)" />
            <asp:ListItem Value="1" Text="Digitada" />
            <asp:ListItem Value="2" Text="Aguardando pagamento" />
            <asp:ListItem Value="3" Text="Efetivada" />
            <asp:ListItem Value="4" Text="Cancelada" />
            <asp:ListItem Value="5" Text="Arrependimento" />
        </asp:DropDownList>
        &nbsp; <asp:Button ID="btnPesquisar" runat="server" Text="Pesquisar" CssClass="botao" />
        &nbsp; <asp:Button ID="btnNova" runat="server" Text="Nova proposta" CssClass="botao" />
    </asp:Panel>

    <asp:GridView ID="gvPropostas" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
        AllowPaging="true" PageSize="30" EmptyDataText="Nenhuma proposta encontrada.">
        <Columns>
            <asp:HyperLinkField DataTextField="NR_PROPOSTA" HeaderText="Nº" DataNavigateUrlFields="CD_PROPOSTA" DataNavigateUrlFormatString="PropostaEdicao.aspx?id={0}" />
            <asp:BoundField DataField="DT_PROPOSTA" HeaderText="Data" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="NM_CONSORCIADO" HeaderText="Consorciado" />
            <asp:BoundField DataField="NR_GRUPO" HeaderText="Grupo" />
            <asp:BoundField DataField="DS_BEM" HeaderText="Crédito" />
            <asp:BoundField DataField="VL_PRIMEIRA_PARCELA" HeaderText="1ª parcela" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="NM_PONTO_VENDA" HeaderText="Ponto de venda" />
            <asp:TemplateField HeaderText="Situação">
                <ItemTemplate><%# DescricaoSituacaoProposta(Eval("ID_SITUACAO")) %></ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <PagerStyle CssClass="rodapeGrid" />
    </asp:GridView>
</asp:Content>
