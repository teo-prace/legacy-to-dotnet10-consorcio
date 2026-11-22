<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Baixa.aspx.vb" Inherits="Cobranca_Baixa" Title="SGC - Baixa manual" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Baixa manual de parcela</h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />

    <asp:Panel ID="pnlBusca" runat="server" CssClass="filtro" DefaultButton="btnBuscar">
        Nosso número: <asp:TextBox ID="txtNossoNumero" runat="server" CssClass="campo" Width="110px" MaxLength="11" />
        &nbsp; <asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="botao" />
    </asp:Panel>

    <asp:Panel ID="pnlParcela" runat="server" Visible="false">
        <div class="quadro">
        <table class="form">
            <tr>
                <td class="rotulo">Grupo/cota:</td><td><asp:HyperLink ID="lnkCota" runat="server" /></td>
                <td class="rotulo">Consorciado:</td><td><asp:Literal ID="litConsorciado" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Parcela:</td><td><asp:Literal ID="litParcela" runat="server" /></td>
                <td class="rotulo">Nosso número:</td><td><asp:Literal ID="litNossoNumero" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Vencimento:</td><td><asp:Literal ID="litVencimento" runat="server" /></td>
                <td class="rotulo">Valor da parcela:</td><td><b><asp:Literal ID="litValor" runat="server" /></b></td>
            </tr>
            <tr>
                <td class="rotulo">Situação:</td><td colspan="3"><asp:Literal ID="litSituacao" runat="server" /></td>
            </tr>
        </table>
        </div>

        <asp:Panel ID="pnlBaixa" runat="server" CssClass="quadro">
            <table class="form">
                <tr>
                    <td class="rotulo">Data do pagamento:</td>
                    <td><asp:TextBox ID="txtDataPagamento" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" />
                        <asp:Button ID="btnCalcular" runat="server" Text="Calcular encargos" CssClass="botao" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Multa:</td><td><asp:Literal ID="litMulta" runat="server" Text="-" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Juros:</td><td><asp:Literal ID="litJuros" runat="server" Text="-" /> <asp:Literal ID="litDias" runat="server" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Total devido:</td><td><b><asp:Literal ID="litDevido" runat="server" Text="-" /></b></td>
                </tr>
                <tr>
                    <td class="rotulo">Valor pago:</td>
                    <td><asp:TextBox ID="txtValorPago" runat="server" CssClass="campo" Width="90px" /></td>
                </tr>
            </table>
            <asp:Button ID="btnBaixar" runat="server" Text="Confirmar baixa" CssClass="botaoPrincipal"
                OnClientClick="return confirma('Confirma a baixa da parcela?');" />
        </asp:Panel>
    </asp:Panel>
</asp:Content>
