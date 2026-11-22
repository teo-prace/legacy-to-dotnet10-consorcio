<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Planos.aspx.vb" Inherits="Comercial_Planos" Title="SGC - Planos" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Planos</h1>
    <div class="filtro">
        <asp:CheckBox ID="chkInativos" runat="server" Text="Mostrar inativos" AutoPostBack="true" />
        &nbsp; <asp:Button ID="btnNovo" runat="server" Text="Novo plano" CssClass="botao" />
    </div>
    <asp:GridView ID="gvPlanos" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt">
        <Columns>
            <asp:HyperLinkField DataTextField="NM_PLANO" HeaderText="Plano" DataNavigateUrlFields="CD_PLANO" DataNavigateUrlFormatString="PlanoEdicao.aspx?id={0}" />
            <asp:TemplateField HeaderText="Tipo">
                <ItemTemplate><%# DescricaoTipoBem(Eval("ID_TIPO_BEM")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="QT_PRAZO_MESES" HeaderText="Prazo" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="PC_TAXA_ADM" HeaderText="Taxa adm %" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="PC_ANTECIPACAO_TA" HeaderText="Antecip. %" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="PC_FUNDO_RESERVA" HeaderText="F. reserva %" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_COTAS_MINIMO" HeaderText="Mín." ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_COTAS_MAXIMO" HeaderText="Máx." ItemStyle-CssClass="num" />
            <asp:BoundField DataField="SG_INDICE" HeaderText="Índice" />
            <asp:BoundField DataField="DS_VERSAO_REGULAMENTO" HeaderText="Regulamento" />
            <asp:BoundField DataField="QT_GRUPOS" HeaderText="Grupos" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="FL_ATIVO" HeaderText="Ativo" ItemStyle-CssClass="centro" />
        </Columns>
    </asp:GridView>
</asp:Content>
