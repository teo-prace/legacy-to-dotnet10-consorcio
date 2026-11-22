<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Retornos.aspx.vb" Inherits="Cobranca_Retornos" Title="SGC - Retornos bancários" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Retornos bancários</h1>
    <div class="aviso">Os arquivos .RET são processados pelo SGC.Batch (RETORNO) a partir da pasta configurada em DS_PASTA_RETORNO.</div>

    <asp:GridView ID="gvArquivos" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
        DataKeyNames="CD_RETORNO_ARQUIVO" SelectedRowStyle-BackColor="#ffe8a1" EmptyDataText="Nenhum arquivo processado.">
        <Columns>
            <asp:ButtonField CommandName="Select" Text="Detalhes" />
            <asp:BoundField DataField="NM_ARQUIVO" HeaderText="Arquivo" />
            <asp:BoundField DataField="DT_GRAVACAO" HeaderText="Gravação" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="DT_PROCESSAMENTO" HeaderText="Processado em" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField DataField="QT_REGISTROS" HeaderText="Registros" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_BAIXADOS" HeaderText="Baixados" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="QT_REJEITADOS" HeaderText="Rejeitados" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="VL_TOTAL_BAIXADO" HeaderText="Total baixado" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
        </Columns>
    </asp:GridView>

    <asp:Panel ID="pnlDetalhe" runat="server" Visible="false">
        <h2>Registros do arquivo <asp:Literal ID="litArquivo" runat="server" /></h2>
        <asp:GridView ID="gvDetalhe" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt">
            <Columns>
                <asp:BoundField DataField="NR_LINHA" HeaderText="Linha" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="NR_NOSSO_NUMERO" HeaderText="Nosso número" />
                <asp:BoundField DataField="CD_OCORRENCIA" HeaderText="Ocorr." />
                <asp:BoundField DataField="DT_OCORRENCIA" HeaderText="Data" DataFormatString="{0:dd/MM/yyyy}" />
                <asp:BoundField DataField="VL_PAGO" HeaderText="Valor pago" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="VL_JUROS_MULTA" HeaderText="Juros/multa" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
                <asp:HyperLinkField DataTextField="DS_COTA" HeaderText="Cota" DataNavigateUrlFields="CD_COTA" DataNavigateUrlFormatString="~/Cotas/CotaDetalhe.aspx?id={0}" />
                <asp:TemplateField HeaderText="Situação">
                    <ItemTemplate><%# IIf(Eval("ID_SITUACAO") = 1, "Baixado", IIf(Eval("ID_SITUACAO") = 2, "<b style='color:#8b0000'>Rejeitado</b>", "Ignorado")) %></ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="DS_MOTIVO" HeaderText="Motivo" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
</asp:Content>
