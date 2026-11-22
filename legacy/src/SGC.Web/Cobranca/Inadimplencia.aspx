<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Inadimplencia.aspx.vb" Inherits="Cobranca_Inadimplencia" Title="SGC - Inadimplência" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Inadimplência</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <div class="aviso">A exclusão de cotas roda todas as noites pelo SGC.Batch (INADIMPLENCIA). Cotas ativas com
        <asp:Literal ID="litQt" runat="server" /> ou mais parcelas vencidas são excluídas; contempladas vão para a cobrança jurídica.</div>
    <div class="filtro">
        Grupo: <asp:DropDownList ID="ddlGrupo" runat="server" CssClass="campo" AutoPostBack="true" />
        &nbsp; <asp:Button ID="btnExcluir" runat="server" Text="Executar exclusão agora" CssClass="botao"
            OnClientClick="return confirma('Excluir agora as cotas ativas com parcelas vencidas acima do limite?');" />
    </div>

    <asp:GridView ID="gvAtraso" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" EmptyDataText="Nenhuma cota em atraso.">
        <Columns>
            <asp:HyperLinkField DataTextField="DS_COTA" HeaderText="Grupo/cota" DataNavigateUrlFields="CD_COTA" DataNavigateUrlFormatString="~/Cotas/CotaDetalhe.aspx?id={0}" />
            <asp:BoundField DataField="NM_CONSORCIADO" HeaderText="Consorciado" />
            <asp:BoundField DataField="NR_CELULAR" HeaderText="Celular" />
            <asp:TemplateField HeaderText="Situação da cota">
                <ItemTemplate><%# DescricaoSituacaoCota(Eval("ID_SITUACAO")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="QT_VENCIDAS" HeaderText="Vencidas" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DT_PRIMEIRA" HeaderText="Desde" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="VL_ABERTO" HeaderText="Em aberto (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:TemplateField HeaderText="Encaminhamento">
                <ItemTemplate><%# Encaminhamento(Eval("ID_SITUACAO"), Eval("QT_VENCIDAS")) %></ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Content>
