<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Contemplacoes.aspx.vb" Inherits="Assembleia_Contemplacoes" Title="SGC - Contemplações" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Contemplações</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <div class="filtro">
        Grupo: <asp:DropDownList ID="ddlGrupo" runat="server" CssClass="campo" AutoPostBack="true" />
        &nbsp; Situação:
        <asp:DropDownList ID="ddlSituacao" runat="server" CssClass="campo" AutoPostBack="true">
            <asp:ListItem Value="1" Text="Aguardando liberação" />
            <asp:ListItem Value="2" Text="Crédito liberado" />
            <asp:ListItem Value="3" Text="Cancelada" />
            <asp:ListItem Value="" Text="(todas)" />
        </asp:DropDownList>
    </div>

    <asp:GridView ID="gvContemplacoes" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
        DataKeyNames="CD_CONTEMPLACAO" SelectedRowStyle-BackColor="#ffe8a1" EmptyDataText="Nenhuma contemplação.">
        <Columns>
            <asp:ButtonField CommandName="Select" Text="Selecionar" />
            <asp:BoundField DataField="DT_CONTEMPLACAO" HeaderText="Contemplação" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="NR_ASSEMBLEIA" HeaderText="AGO" ItemStyle-CssClass="num" />
            <asp:HyperLinkField DataTextField="DS_COTA" HeaderText="Cota" DataNavigateUrlFields="CD_COTA" DataNavigateUrlFormatString="~/Cotas/CotaDetalhe.aspx?id={0}" />
            <asp:BoundField DataField="NM_CONSORCIADO" HeaderText="Consorciado" />
            <asp:BoundField DataField="DS_TIPO" HeaderText="Tipo" />
            <asp:BoundField DataField="VL_CREDITO_LIQUIDO" HeaderText="Crédito líquido" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DS_SITUACAO" HeaderText="Situação" />
            <asp:BoundField DataField="DT_LIBERACAO" HeaderText="Liberado em" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="DS_FAVORECIDO" HeaderText="Favorecido" />
        </Columns>
    </asp:GridView>

    <asp:Panel ID="pnlLiberar" runat="server" Visible="false" CssClass="quadro" style="margin-top:10px">
        <b>Liberação do crédito - <asp:Literal ID="litCota" runat="server" /></b><br /><br />
        Favorecido (vendedor do bem): <asp:TextBox ID="txtFavorecido" runat="server" CssClass="campo" Width="300px" MaxLength="150" />
        &nbsp; Data: <asp:TextBox ID="txtDataLiberacao" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" />
        &nbsp; <asp:Button ID="btnLiberar" runat="server" Text="Liberar crédito" CssClass="botaoPrincipal"
            OnClientClick="return confirma('Confirma a liberação do crédito?');" />
        &nbsp; <asp:Button ID="btnCancelar" runat="server" Text="Cancelar contemplação" CssClass="botao"
            OnClientClick="return confirma('Cancelar a contemplação? A cota volta a concorrer.');" />
    </asp:Panel>
</asp:Content>
