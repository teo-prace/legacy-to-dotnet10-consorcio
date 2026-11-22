<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="CotaDetalhe.aspx.vb" Inherits="Cotas_CotaDetalhe" Title="SGC - Cota" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Cota <asp:Literal ID="litCota" runat="server" /></h1>

    <div class="quadro">
    <table class="form">
        <tr>
            <td class="rotulo">Consorciado:</td><td><asp:HyperLink ID="lnkConsorciado" runat="server" /></td>
            <td class="rotulo">Situação:</td><td><b><asp:Literal ID="litSituacao" runat="server" /></b></td>
        </tr>
        <tr>
            <td class="rotulo">Grupo:</td><td><asp:HyperLink ID="lnkGrupo" runat="server" /></td>
            <td class="rotulo">Proposta:</td><td><asp:HyperLink ID="lnkProposta" runat="server" /></td>
        </tr>
        <tr>
            <td class="rotulo">Bem:</td><td><asp:Literal ID="litBem" runat="server" /></td>
            <td class="rotulo">Adesão:</td><td><asp:Literal ID="litAdesao" runat="server" /></td>
        </tr>
        <tr>
            <td class="rotulo">Crédito na adesão:</td><td><asp:Literal ID="litCreditoAdesao" runat="server" /></td>
            <td class="rotulo">Crédito atual:</td><td><b><asp:Literal ID="litCreditoAtual" runat="server" /></b></td>
        </tr>
        <tr>
            <td class="rotulo">Prazo da cota:</td><td><asp:Literal ID="litPrazo" runat="server" /></td>
            <td class="rotulo">% amortizado (fundo comum):</td><td><asp:Literal ID="litAmortizado" runat="server" /></td>
        </tr>
        <tr>
            <td class="rotulo">Parcelas pagas:</td><td><asp:Literal ID="litPagas" runat="server" /></td>
            <td class="rotulo">Saldo devedor:</td><td><asp:Literal ID="litSaldoDevedor" runat="server" /></td>
        </tr>
    </table>
    </div>

    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <asp:Panel ID="pnlDesistencia" runat="server" Visible="false" CssClass="quadro">
        <b>Desistência</b> - o consorciado deixa o grupo; o valor pago ao fundo comum é devolvido quando a cota for sorteada
        (estimativa hoje: <asp:Literal ID="litEstimativa" runat="server" />, já descontada a multa rescisória).<br /><br />
        Motivo: <asp:TextBox ID="txtMotivoDesistencia" runat="server" CssClass="campo" Width="300px" MaxLength="150" />
        <asp:Button ID="btnDesistencia" runat="server" Text="Registrar desistência" CssClass="botao"
            OnClientClick="return confirma('Registrar a desistência? As parcelas em aberto serão canceladas.');" />
    </asp:Panel>

    <asp:Panel ID="pnlContemplacao" runat="server" Visible="false">
        <h2>Contemplação</h2>
        <div class="quadro">
            <asp:Literal ID="litContemplacao" runat="server" />
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlLances" runat="server" Visible="false">
        <h2>Lances ofertados</h2>
        <asp:GridView ID="gvLances" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt">
            <Columns>
                <asp:HyperLinkField DataTextField="NR_ASSEMBLEIA" HeaderText="AGO" DataNavigateUrlFields="CD_ASSEMBLEIA" DataNavigateUrlFormatString="~/Assembleia/AssembleiaEdicao.aspx?id={0}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="DT_ASSEMBLEIA" HeaderText="Data" DataFormatString="{0:dd/MM/yyyy}" />
                <asp:BoundField DataField="PC_LANCE" HeaderText="% Lance" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="PC_EMBUTIDO" HeaderText="% Embutido" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="DS_SITUACAO" HeaderText="Resultado" />
                <asp:BoundField DataField="DS_MOTIVO" HeaderText="Motivo" />
            </Columns>
        </asp:GridView>
    </asp:Panel>

    <h2>Plano de parcelas</h2>
    <asp:GridView ID="gvParcelas" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" ShowFooter="true">
        <Columns>
            <asp:BoundField DataField="NR_PARCELA" HeaderText="Nº" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DT_VENCIMENTO" HeaderText="Vencimento" DataFormatString="{0:dd/MM/yyyy}" NullDisplayText="a definir" />
            <asp:BoundField DataField="PC_FUNDO_COMUM" HeaderText="% FC" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
            <asp:BoundField DataField="PC_FC_AMORTIZADO_LANCE" HeaderText="% FC lance" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="PC_TAXA_ADM" HeaderText="% TA" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
            <asp:BoundField DataField="PC_FUNDO_RESERVA" HeaderText="% FR" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
            <asp:BoundField DataField="PC_TOTAL" HeaderText="% Total" DataFormatString="{0:0.000000}" ItemStyle-CssClass="num" FooterStyle-CssClass="num" />
            <asp:BoundField DataField="VL_ESTIMADO" HeaderText="Valor estimado" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="VL_PARCELA" HeaderText="Valor emitido" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DT_PAGAMENTO" HeaderText="Pago em" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="VL_PAGO" HeaderText="Valor pago" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:TemplateField HeaderText="Situação">
                <ItemTemplate><%# DescricaoSituacaoParcela(Eval("ID_SITUACAO")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:HyperLink ID="lnkBoleto" runat="server" Text="Boleto" Target="_blank" NavigateUrl='<%# "~/Cobranca/Boleto.aspx?id=" & Eval("CD_PARCELA") %>' Visible='<%# Eval("ID_SITUACAO") = 2 %>' />
                    <asp:HyperLink ID="lnkBaixa" runat="server" Text="Baixar" NavigateUrl='<%# "~/Cobranca/Baixa.aspx?id=" & Eval("CD_PARCELA") %>' Visible='<%# Eval("ID_SITUACAO") = 2 %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <FooterStyle CssClass="rodapeGrid" />
    </asp:GridView>
    <div style="color:#777; margin-top:4px">Valor estimado = % total × crédito atual. O valor cobrado é calculado na emissão.</div>
</asp:Content>
