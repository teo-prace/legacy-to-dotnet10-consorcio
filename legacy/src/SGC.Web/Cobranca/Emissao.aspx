<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Emissao.aspx.vb" Inherits="Cobranca_Emissao" Title="SGC - Emissão da cobrança" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Emissão da cobrança</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <div class="aviso">A emissão roda automaticamente todo dia 5 pelo SGC.Batch. Use esta tela só para emitir fora do agendamento.</div>
    <div class="filtro">
        Vencimentos de: <asp:DropDownList ID="ddlMes" runat="server" CssClass="campo" />
        &nbsp; <asp:Button ID="btnEmitir" runat="server" Text="Emitir" CssClass="botaoPrincipal"
            OnClientClick="return confirma('Emitir a cobrança do mês selecionado?');" />
    </div>

    <asp:GridView ID="gvResultado" runat="server" AutoGenerateColumns="false" CssClass="grid" Visible="false" EmptyDataText="Nenhuma parcela a emitir neste mês.">
        <Columns>
            <asp:BoundField DataField="NR_GRUPO" HeaderText="Grupo" />
            <asp:BoundField DataField="QT_PARCELAS" HeaderText="Parcelas emitidas" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="VL_TOTAL" HeaderText="Valor (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
        </Columns>
    </asp:GridView>

    <h2>Situação da cobrança por mês de vencimento</h2>
    <asp:GridView ID="gvMeses" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt">
        <Columns>
            <asp:BoundField DataField="DS_MES" HeaderText="Vencimento" />
            <asp:BoundField DataField="QT_ABERTAS" HeaderText="Não emitidas" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_EMITIDAS" HeaderText="Emitidas em aberto" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_PAGAS" HeaderText="Pagas" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="VL_EMITIDO" HeaderText="Emitido (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="VL_RECEBIDO" HeaderText="Recebido (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
        </Columns>
    </asp:GridView>
</asp:Content>
