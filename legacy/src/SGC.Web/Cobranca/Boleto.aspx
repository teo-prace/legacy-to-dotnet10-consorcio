<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Boleto.aspx.vb" Inherits="Cobranca_Boleto" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Boleto</title>
    <style type="text/css">
        body { font-family: Arial, sans-serif; font-size: 10px; margin: 20px; }
        table.bol { border-collapse: collapse; width: 666px; }
        table.bol td { border: 1px solid #000; padding: 2px 4px; vertical-align: top; }
        .rot { font-size: 8px; color: #333; display: block; }
        .val { font-size: 11px; font-weight: bold; }
        .cab td { border: none !important; border-bottom: 2px solid #000 !important; font-size: 18px; font-weight: bold; }
        .linha { font-size: 13px; font-weight: bold; text-align: right; letter-spacing: 1px; }
        .barras { height: 50px; margin: 8px 0 0 0; white-space: nowrap; line-height: 0; }
        .barras span { display: inline-block; height: 50px; }
        .p { background: #000; } .b { background: #fff; }
        .f { width: 1px; } .g { width: 3px; }
        .corte { border-top: 1px dashed #000; margin: 14px 0; width: 666px; }
        .noprint { margin-bottom: 10px; }
        @media print { .noprint { display: none; } }
    </style>
</head>
<body>
    <div class="noprint"><a href="javascript:window.print()">Imprimir</a></div>
    <asp:Panel ID="pnlErro" runat="server" Visible="false"><b><asp:Literal ID="litErro" runat="server" /></b></asp:Panel>
    <asp:Panel ID="pnlBoleto" runat="server">
    <%-- recibo do sacado --%>
    <table class="bol">
        <tr class="cab"><td style="width:150px">CeCons</td><td style="width:60px">999-9</td><td class="linha" colspan="3"><asp:Literal ID="litLinha1" runat="server" /></td></tr>
        <tr>
            <td colspan="3"><span class="rot">Cedente</span><span class="val"><asp:Literal ID="litCedente1" runat="server" /></span></td>
            <td><span class="rot">Vencimento</span><span class="val"><asp:Literal ID="litVenc1" runat="server" /></span></td>
            <td><span class="rot">Valor do documento</span><span class="val"><asp:Literal ID="litValor1" runat="server" /></span></td>
        </tr>
        <tr>
            <td colspan="3"><span class="rot">Sacado</span><span class="val"><asp:Literal ID="litSacado1" runat="server" /></span></td>
            <td><span class="rot">Nosso número</span><span class="val"><asp:Literal ID="litNosso1" runat="server" /></span></td>
            <td><span class="rot">Grupo / cota / parcela</span><span class="val"><asp:Literal ID="litCota1" runat="server" /></span></td>
        </tr>
    </table>
    <div style="width:666px; text-align:right">Recibo do sacado - autenticação mecânica</div>
    <div class="corte"></div>

    <%-- ficha de compensacao --%>
    <table class="bol">
        <tr class="cab"><td style="width:150px">CeCons</td><td style="width:60px">999-9</td><td class="linha" colspan="4"><asp:Literal ID="litLinha" runat="server" /></td></tr>
        <tr>
            <td colspan="5"><span class="rot">Local de pagamento</span><span class="val">Pagável em qualquer banco até o vencimento. Após, somente no banco 999.</span></td>
            <td style="width:140px"><span class="rot">Vencimento</span><span class="val"><asp:Literal ID="litVencimento" runat="server" /></span></td>
        </tr>
        <tr>
            <td colspan="5"><span class="rot">Cedente</span><span class="val"><asp:Literal ID="litCedente" runat="server" /></span></td>
            <td><span class="rot">Agência / código cedente</span><span class="val"><asp:Literal ID="litAgencia" runat="server" /></span></td>
        </tr>
        <tr>
            <td><span class="rot">Data do documento</span><span class="val"><asp:Literal ID="litEmissao" runat="server" /></span></td>
            <td colspan="2"><span class="rot">Nº do documento</span><span class="val"><asp:Literal ID="litDocumento" runat="server" /></span></td>
            <td><span class="rot">Espécie doc.</span><span class="val">DM</span></td>
            <td><span class="rot">Carteira</span><span class="val"><asp:Literal ID="litCarteira" runat="server" /></span></td>
            <td><span class="rot">Nosso número</span><span class="val"><asp:Literal ID="litNosso" runat="server" /></span></td>
        </tr>
        <tr>
            <td colspan="5" rowspan="3"><span class="rot">Instruções (texto de responsabilidade do cedente)</span>
                Após o vencimento cobrar multa de <asp:Literal ID="litMulta" runat="server" />% e juros de <asp:Literal ID="litJuros" runat="server" />% ao mês.<br />
                Vencimento em dia não útil: pagável sem encargos no primeiro dia útil seguinte.<br />
                Consórcio - parcela <asp:Literal ID="litParcela" runat="server" /> - grupo/cota <asp:Literal ID="litCota" runat="server" />.<br />
                Não receber após 60 dias do vencimento.
            </td>
            <td><span class="rot">(=) Valor do documento</span><span class="val"><asp:Literal ID="litValor" runat="server" /></span></td>
        </tr>
        <tr><td><span class="rot">(+) Mora / multa</span>&nbsp;</td></tr>
        <tr><td><span class="rot">(=) Valor cobrado</span>&nbsp;</td></tr>
        <tr>
            <td colspan="6"><span class="rot">Sacado</span><span class="val"><asp:Literal ID="litSacado" runat="server" /></span><br />
                <asp:Literal ID="litEndereco" runat="server" /></td>
        </tr>
    </table>
    <div class="barras"><asp:Literal ID="litBarras" runat="server" /></div>
    <div style="width:666px; text-align:right">Ficha de compensação - autenticação mecânica</div>
    </asp:Panel>
</body>
</html>
