<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Default.aspx.vb" Inherits="_Default" Title="SGC - Painel" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Painel</h1>

    <div class="painel">Cotas vendidas no mês<div class="valor"><asp:Literal ID="litCotasMes" runat="server" /></div></div>
    <div class="painel">Propostas aguardando pagamento<div class="valor"><asp:Literal ID="litAguardando" runat="server" /></div></div>
    <div class="painel">Propostas digitadas<div class="valor"><asp:Literal ID="litDigitadas" runat="server" /></div></div>
    <div class="painel">Cotas ativas (todos os grupos)<div class="valor"><asp:Literal ID="litCotasAtivas" runat="server" /></div></div>

    <h2>Grupos em formação</h2>
    <asp:GridView ID="gvFormacao" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
        EmptyDataText="Nenhum grupo em formação.">
        <Columns>
            <asp:HyperLinkField DataTextField="NR_GRUPO" HeaderText="Grupo" DataNavigateUrlFields="CD_GRUPO" DataNavigateUrlFormatString="~/Grupos/GrupoEdicao.aspx?id={0}" />
            <asp:BoundField DataField="NM_PLANO" HeaderText="Plano" />
            <asp:BoundField DataField="DT_ABERTURA" HeaderText="Abertura" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="QT_ATIVAS" HeaderText="Cotas" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_COTAS_MINIMO" HeaderText="Mínimo" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_COTAS_MAXIMO" HeaderText="Máximo" ItemStyle-CssClass="num" />
            <asp:TemplateField HeaderText="Quórum">
                <ItemTemplate><%# Format(Eval("QT_ATIVAS") * 100 / Eval("QT_COTAS_MINIMO"), "0") %>%</ItemTemplate>
                <ItemStyle CssClass="num" />
            </asp:TemplateField>
            <asp:TemplateField HeaderText="">
                <ItemTemplate><%# IIf(Eval("QT_ATIVAS") >= Eval("QT_COTAS_MINIMO"), "<b style='color:#1b5e20'>Pronto para constituir</b>", "") %></ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <h2>Últimas adesões</h2>
    <asp:GridView ID="gvUltimas" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt">
        <Columns>
            <asp:BoundField DataField="DT_EFETIVACAO" HeaderText="Data" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:HyperLinkField DataTextField="NR_PROPOSTA" HeaderText="Proposta" DataNavigateUrlFields="CD_PROPOSTA" DataNavigateUrlFormatString="~/Comercial/PropostaEdicao.aspx?id={0}" />
            <asp:BoundField DataField="NM_CONSORCIADO" HeaderText="Consorciado" />
            <asp:HyperLinkField DataTextField="DS_COTA" HeaderText="Grupo/cota" DataNavigateUrlFields="CD_COTA" DataNavigateUrlFormatString="~/Cotas/CotaDetalhe.aspx?id={0}" />
            <asp:BoundField DataField="DS_BEM" HeaderText="Bem" />
            <asp:BoundField DataField="NM_VENDEDOR" HeaderText="Vendedor" />
        </Columns>
    </asp:GridView>
</asp:Content>
