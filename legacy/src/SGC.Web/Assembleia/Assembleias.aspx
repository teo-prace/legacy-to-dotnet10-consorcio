<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Assembleias.aspx.vb" Inherits="Assembleia_Assembleias" Title="SGC - Assembleias" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Assembleias</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <div class="filtro">
        Grupo: <asp:DropDownList ID="ddlGrupo" runat="server" CssClass="campo" AutoPostBack="true" />
        &nbsp; <asp:Button ID="btnAgendar" runat="server" Text="Agendar próxima assembleia" CssClass="botao" />
    </div>

    <asp:GridView ID="gvAssembleias" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" EmptyDataText="Nenhuma assembleia.">
        <Columns>
            <asp:HyperLinkField DataTextField="NR_ASSEMBLEIA" HeaderText="AGO" DataNavigateUrlFields="CD_ASSEMBLEIA" DataNavigateUrlFormatString="AssembleiaEdicao.aspx?id={0}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DT_ASSEMBLEIA" HeaderText="Data" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Situação">
                <ItemTemplate><%# IIf(Eval("ID_SITUACAO") = 1, "<b>Agendada</b>", IIf(Eval("ID_SITUACAO") = 2, "Apurada", "Cancelada")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="NR_PREMIO_1" HeaderText="1º prêmio" />
            <asp:BoundField DataField="QT_LANCES" HeaderText="Lances" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="VL_RECURSOS" HeaderText="Recursos (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_CONTEMPLACOES" HeaderText="Contemplações" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="VL_CONTEMPLADO" HeaderText="Créditos (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
        </Columns>
    </asp:GridView>
</asp:Content>
