<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Grupos.aspx.vb" Inherits="Grupos_Grupos" Title="SGC - Grupos" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Grupos</h1>
    <div class="filtro">
        Situação:
        <asp:DropDownList ID="ddlSituacao" runat="server" CssClass="campo" AutoPostBack="true">
            <asp:ListItem Value="" Text="(todas)" />
            <asp:ListItem Value="1" Text="Em formação" />
            <asp:ListItem Value="2" Text="Em andamento" />
            <asp:ListItem Value="3" Text="Encerrado" />
            <asp:ListItem Value="4" Text="Cancelado" />
        </asp:DropDownList>
        &nbsp; <asp:Button ID="btnNovo" runat="server" Text="Novo grupo" CssClass="botao" />
    </div>
    <asp:GridView ID="gvGrupos" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" EmptyDataText="Nenhum grupo.">
        <Columns>
            <asp:HyperLinkField DataTextField="NR_GRUPO" HeaderText="Grupo" DataNavigateUrlFields="CD_GRUPO" DataNavigateUrlFormatString="GrupoEdicao.aspx?id={0}" />
            <asp:BoundField DataField="NM_PLANO" HeaderText="Plano" />
            <asp:BoundField DataField="QT_PRAZO_MESES" HeaderText="Prazo" ItemStyle-CssClass="num" />
            <asp:TemplateField HeaderText="Cotas">
                <ItemTemplate><%# Eval("QT_ATIVAS") %> / <%# Eval("QT_COTAS_MAXIMO") %></ItemTemplate>
                <ItemStyle CssClass="num" />
            </asp:TemplateField>
            <asp:BoundField DataField="DT_ABERTURA" HeaderText="Abertura" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="DT_CONSTITUICAO" HeaderText="1ª assembleia" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="QT_ASSEMBLEIAS_REALIZADAS" HeaderText="AGOs" ItemStyle-CssClass="num" />
            <asp:TemplateField HeaderText="Situação">
                <ItemTemplate><%# DescricaoSituacaoGrupo(Eval("ID_SITUACAO")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:HyperLinkField Text="Mapa de cotas" DataNavigateUrlFields="CD_GRUPO" DataNavigateUrlFormatString="MapaCotas.aspx?id={0}" />
            <asp:HyperLinkField Text="Extrato" DataNavigateUrlFields="CD_GRUPO" DataNavigateUrlFormatString="Extrato.aspx?id={0}" />
        </Columns>
    </asp:GridView>
</asp:Content>
