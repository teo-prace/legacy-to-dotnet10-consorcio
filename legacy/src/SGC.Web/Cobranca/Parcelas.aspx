<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Parcelas.aspx.vb" Inherits="Cobranca_Parcelas" Title="SGC - Parcelas" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Consulta de parcelas</h1>
    <asp:Panel ID="pnlFiltro" runat="server" CssClass="filtro" DefaultButton="btnPesquisar">
        Grupo: <asp:DropDownList ID="ddlGrupo" runat="server" CssClass="campo" />
        &nbsp; Situação:
        <asp:DropDownList ID="ddlSituacao" runat="server" CssClass="campo">
            <asp:ListItem Value="V" Text="Vencidas em aberto" />
            <asp:ListItem Value="2" Text="Emitidas" />
            <asp:ListItem Value="3" Text="Pagas" />
            <asp:ListItem Value="1" Text="Não emitidas" />
            <asp:ListItem Value="" Text="(todas)" />
        </asp:DropDownList>
        &nbsp; Vencimento de: <asp:TextBox ID="txtDe" runat="server" CssClass="campo" Width="75px" onkeyup="mascaraData(this)" />
        até: <asp:TextBox ID="txtAte" runat="server" CssClass="campo" Width="75px" onkeyup="mascaraData(this)" />
        &nbsp; Nosso nº: <asp:TextBox ID="txtNossoNumero" runat="server" CssClass="campo" Width="90px" />
        &nbsp; <asp:Button ID="btnPesquisar" runat="server" Text="Pesquisar" CssClass="botao" />
    </asp:Panel>

    <asp:GridView ID="gvParcelas" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
        AllowPaging="true" PageSize="50" EmptyDataText="Nenhuma parcela encontrada.">
        <Columns>
            <asp:HyperLinkField DataTextField="DS_COTA" HeaderText="Grupo/cota" DataNavigateUrlFields="CD_COTA" DataNavigateUrlFormatString="~/Cotas/CotaDetalhe.aspx?id={0}" />
            <asp:BoundField DataField="NM_CONSORCIADO" HeaderText="Consorciado" />
            <asp:BoundField DataField="NR_PARCELA" HeaderText="Parc." ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DT_VENCIMENTO" HeaderText="Vencimento" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="NR_NOSSO_NUMERO" HeaderText="Nosso número" />
            <asp:BoundField DataField="VL_PARCELA" HeaderText="Valor" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DT_PAGAMENTO" HeaderText="Pago em" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="VL_PAGO" HeaderText="Valor pago" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_DIAS_ATRASO" HeaderText="Dias atraso" ItemStyle-CssClass="num" />
            <asp:TemplateField HeaderText="Situação">
                <ItemTemplate><%# DescricaoSituacaoParcela(Eval("ID_SITUACAO")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:HyperLink ID="lnkBoleto" runat="server" Text="Boleto" Target="_blank" NavigateUrl='<%# "Boleto.aspx?id=" & Eval("CD_PARCELA") %>' Visible='<%# Eval("ID_SITUACAO") = 2 %>' />
                    <asp:HyperLink ID="lnkBaixa" runat="server" Text="Baixar" NavigateUrl='<%# "Baixa.aspx?id=" & Eval("CD_PARCELA") %>' Visible='<%# Eval("ID_SITUACAO") = 2 %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <PagerStyle CssClass="rodapeGrid" />
    </asp:GridView>
    <asp:Label ID="lblTotal" runat="server" />
</asp:Content>
