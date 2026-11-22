<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Extrato.aspx.vb" Inherits="Grupos_Extrato" Title="SGC - Extrato do grupo" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Extrato do grupo <asp:Literal ID="litGrupo" runat="server" /></h1>
    <div class="filtro">
        Fundo:
        <asp:DropDownList ID="ddlFundo" runat="server" CssClass="campo">
            <asp:ListItem Value="" Text="(todos)" />
            <asp:ListItem Value="FC" Text="FC - Fundo comum" />
            <asp:ListItem Value="FR" Text="FR - Fundo de reserva" />
            <asp:ListItem Value="TA" Text="TA - Taxa de administração" />
            <asp:ListItem Value="RD" Text="RD - Recursos a devolver" />
        </asp:DropDownList>
        &nbsp; De: <asp:TextBox ID="txtDe" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" />
        &nbsp; Até: <asp:TextBox ID="txtAte" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" />
        &nbsp; <asp:Button ID="btnFiltrar" runat="server" Text="Filtrar" CssClass="botao" />
    </div>

    <h2>Saldos atuais</h2>
    <asp:GridView ID="gvSaldos" runat="server" AutoGenerateColumns="false" CssClass="grid">
        <Columns>
            <asp:BoundField DataField="SG_FUNDO" HeaderText="Fundo" />
            <asp:BoundField DataField="NM_FUNDO" HeaderText="Descrição" />
            <asp:BoundField DataField="VL_SALDO" HeaderText="Saldo (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
        </Columns>
    </asp:GridView>

    <h2>Lançamentos</h2>
    <asp:GridView ID="gvExtrato" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
        AllowPaging="true" PageSize="50" EmptyDataText="Nenhum lançamento no período.">
        <Columns>
            <asp:BoundField DataField="DT_MOVIMENTO" HeaderText="Data" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="SG_FUNDO" HeaderText="Fundo" />
            <asp:BoundField DataField="DS_TIPO_MOVIMENTO" HeaderText="Tipo" />
            <asp:BoundField DataField="DS_HISTORICO" HeaderText="Histórico" />
            <asp:BoundField DataField="VL_MOVIMENTO" HeaderText="Valor" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="VL_SALDO_FUNDO" HeaderText="Saldo do fundo" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
        </Columns>
        <PagerStyle CssClass="rodapeGrid" />
    </asp:GridView>
    <br />
    <asp:HyperLink ID="lnkGrupo" runat="server" Text="Voltar ao grupo" />
</asp:Content>
