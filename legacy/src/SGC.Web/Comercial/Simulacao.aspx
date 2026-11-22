<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Simulacao.aspx.vb" Inherits="Comercial_Simulacao" Title="SGC - Simulação" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Simulação de parcelas</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <div class="filtro">
        Plano: <asp:DropDownList ID="ddlPlano" runat="server" CssClass="campo" AutoPostBack="true" />
        &nbsp; Crédito: <asp:DropDownList ID="ddlBem" runat="server" CssClass="campo" />
        &nbsp; <asp:Button ID="btnSimular" runat="server" Text="Simular" CssClass="botaoPrincipal" />
    </div>

    <asp:Panel ID="pnlResultado" runat="server" Visible="false">
        <div class="quadro">
            <table class="form">
                <tr>
                    <td class="rotulo">Valor do crédito:</td><td><b><asp:Literal ID="litValorBem" runat="server" /></b></td>
                    <td class="rotulo">Prazo:</td><td><asp:Literal ID="litPrazo" runat="server" /> meses</td>
                </tr>
                <tr>
                    <td class="rotulo">Taxa de administração:</td><td><asp:Literal ID="litTaxa" runat="server" /></td>
                    <td class="rotulo">Fundo de reserva:</td><td><asp:Literal ID="litFundo" runat="server" /></td>
                </tr>
                <tr>
                    <td class="rotulo">1ª parcela:</td><td><b><asp:Literal ID="litPrimeira" runat="server" /></b></td>
                    <td class="rotulo">Demais parcelas:</td><td><b><asp:Literal ID="litDemais" runat="server" /></b></td>
                </tr>
            </table>
            <div style="color:#777; margin-top:4px">Valores com base no crédito vigente hoje. As parcelas acompanham o reajuste do bem.</div>
        </div>

        <asp:GridView ID="gvParcelas" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" ShowFooter="true">
            <Columns>
                <asp:BoundField DataField="Numero" HeaderText="Parcela" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="PercentualFundoComum" HeaderText="% Fundo comum" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
                <asp:BoundField DataField="PercentualTaxaAdm" HeaderText="% Taxa adm" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
                <asp:BoundField DataField="PercentualFundoReserva" HeaderText="% Fundo reserva" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
                <asp:BoundField DataField="PercentualTotal" HeaderText="% Total" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
                <asp:BoundField DataField="Valor" HeaderText="Valor (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
            </Columns>
            <FooterStyle CssClass="rodapeGrid" />
        </asp:GridView>
    </asp:Panel>
</asp:Content>
