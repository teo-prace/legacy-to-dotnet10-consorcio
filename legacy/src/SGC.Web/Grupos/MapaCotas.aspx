<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="MapaCotas.aspx.vb" Inherits="Grupos_MapaCotas" Title="SGC - Mapa de cotas" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Mapa de cotas - grupo <asp:Literal ID="litGrupo" runat="server" /></h1>
    <div class="filtro">
        <span class="cota1" style="padding:2px 8px; border:1px solid #999">Ativa</span>
        <span class="cota2" style="padding:2px 8px; border:1px solid #999">Contemplada</span>
        <span class="cota3" style="padding:2px 8px; border:1px solid #999">Excluída</span>
        <span class="cota4" style="padding:2px 8px; border:1px solid #999">Desistente</span>
        <span class="cota5" style="padding:2px 8px; border:1px solid #999">Quitada</span>
        <span class="cotaLivre" style="padding:2px 8px; border:1px solid #999">Livre</span>
        &nbsp; <asp:Literal ID="litResumo" runat="server" />
    </div>
    <asp:Literal ID="litMapa" runat="server" />
    <br />
    <asp:HyperLink ID="lnkGrupo" runat="server" Text="Voltar ao grupo" />
</asp:Content>
