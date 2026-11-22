<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="AssembleiaEdicao.aspx.vb" Inherits="Assembleia_AssembleiaEdicao" Title="SGC - Assembleia" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server" /></h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <asp:Label ID="lblAviso" runat="server" CssClass="aviso" Visible="false" style="display:block" />

    <div class="quadro">
    <table class="form">
        <tr>
            <td class="rotulo">Grupo:</td><td><asp:HyperLink ID="lnkGrupo" runat="server" /></td>
            <td class="rotulo">Situação:</td><td><b><asp:Literal ID="litSituacao" runat="server" /></b></td>
        </tr>
        <tr>
            <td class="rotulo">Data:</td><td><asp:Literal ID="litData" runat="server" /></td>
            <td class="rotulo">Lances até:</td><td><asp:Literal ID="litLimite" runat="server" /></td>
        </tr>
        <tr>
            <td class="rotulo">Cotas aptas hoje:</td><td><asp:Literal ID="litAptas" runat="server" /></td>
            <td class="rotulo">Lance embutido máx. / fixo:</td><td><asp:Literal ID="litRegras" runat="server" /></td>
        </tr>
    </table>
    </div>

    <%-- ============ agendada ============ --%>
    <asp:Panel ID="pnlAgendada" runat="server" Visible="false">
        <h2>Loteria Federal</h2>
        <div class="quadro">
            Extração: <asp:TextBox ID="txtExtracao" runat="server" CssClass="campo" Width="50px" MaxLength="6" onkeypress="return somenteNumeros(event)" />
            &nbsp; 1º <asp:TextBox ID="txtP1" runat="server" CssClass="campo" Width="50px" MaxLength="5" onkeypress="return somenteNumeros(event)" />
            2º <asp:TextBox ID="txtP2" runat="server" CssClass="campo" Width="50px" MaxLength="5" onkeypress="return somenteNumeros(event)" />
            3º <asp:TextBox ID="txtP3" runat="server" CssClass="campo" Width="50px" MaxLength="5" onkeypress="return somenteNumeros(event)" />
            4º <asp:TextBox ID="txtP4" runat="server" CssClass="campo" Width="50px" MaxLength="5" onkeypress="return somenteNumeros(event)" />
            5º <asp:TextBox ID="txtP5" runat="server" CssClass="campo" Width="50px" MaxLength="5" onkeypress="return somenteNumeros(event)" />
            &nbsp; <asp:Button ID="btnSalvarPremios" runat="server" Text="Salvar prêmios" CssClass="botao" />
        </div>

        <h2>Lances registrados</h2>
        <asp:GridView ID="gvLancesAgendada" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt"
            DataKeyNames="CD_LANCE" EmptyDataText="Nenhum lance registrado.">
            <Columns>
                <asp:BoundField DataField="DS_COTA" HeaderText="Cota" />
                <asp:BoundField DataField="NM_CONSORCIADO" HeaderText="Consorciado" />
                <asp:BoundField DataField="DS_TIPO" HeaderText="Tipo" />
                <asp:BoundField DataField="PC_LANCE" HeaderText="% Lance" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="PC_EMBUTIDO" HeaderText="% Embutido" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="DT_REGISTRO" HeaderText="Registrado em" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                <asp:BoundField DataField="DS_APTA" HeaderText="Apta hoje" ItemStyle-CssClass="centro" />
                <asp:ButtonField CommandName="Cancelar" Text="Cancelar" />
            </Columns>
        </asp:GridView>

        <div class="filtro" style="margin-top:8px">
            Novo lance - cota nº <asp:TextBox ID="txtNrCota" runat="server" CssClass="campo" Width="40px" onkeypress="return somenteNumeros(event)" />
            &nbsp; Tipo: <asp:DropDownList ID="ddlTipoLance" runat="server" CssClass="campo">
                <asp:ListItem Value="1" Text="Livre" />
                <asp:ListItem Value="2" Text="Fixo" />
            </asp:DropDownList>
            &nbsp; % lance: <asp:TextBox ID="txtPcLance" runat="server" CssClass="campo" Width="50px" />
            &nbsp; % embutido: <asp:TextBox ID="txtPcEmbutido" runat="server" CssClass="campo" Width="50px" Text="0" />
            &nbsp; <asp:Button ID="btnRegistrarLance" runat="server" Text="Registrar lance" CssClass="botao" />
        </div>

        <br />
        <asp:Button ID="btnApurar" runat="server" Text="Apurar assembleia" CssClass="botaoPrincipal"
            OnClientClick="return confirma('Apurar a assembleia? A apuração não pode ser desfeita.');" />
    </asp:Panel>

    <%-- ============ apurada ============ --%>
    <asp:Panel ID="pnlApurada" runat="server" Visible="false">
        <h2>Resultado</h2>
        <div class="quadro">
        <table class="form">
            <tr>
                <td class="rotulo">Loteria Federal:</td><td colspan="3"><asp:Literal ID="litPremios" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Número base (1º prêmio):</td><td><asp:Literal ID="litBase" runat="server" /></td>
                <td class="rotulo">Apurada em:</td><td><asp:Literal ID="litApuracao" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Saldo do fundo comum:</td><td><asp:Literal ID="litSaldo" runat="server" /></td>
                <td class="rotulo">Recursos disponíveis:</td><td><b><asp:Literal ID="litRecursos" runat="server" /></b></td>
            </tr>
        </table>
        </div>

        <h2>Contemplações</h2>
        <asp:GridView ID="gvContemplacoes" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" EmptyDataText="Nenhuma contemplação nesta assembleia.">
            <Columns>
                <asp:BoundField DataField="DS_TIPO" HeaderText="Tipo" />
                <asp:HyperLinkField DataTextField="DS_COTA" HeaderText="Cota" DataNavigateUrlFields="CD_COTA" DataNavigateUrlFormatString="~/Cotas/CotaDetalhe.aspx?id={0}" />
                <asp:BoundField DataField="NM_CONSORCIADO" HeaderText="Consorciado" />
                <asp:BoundField DataField="VL_CREDITO" HeaderText="Crédito" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="PC_LANCE" HeaderText="% Lance" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="VL_EMBUTIDO" HeaderText="Embutido" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="VL_RECURSOS_PROPRIOS" HeaderText="Recursos próprios" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="VL_CREDITO_LIQUIDO" HeaderText="Crédito líquido" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="DS_SITUACAO" HeaderText="Situação" />
            </Columns>
        </asp:GridView>

        <h2>Lances</h2>
        <asp:GridView ID="gvLancesApurada" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" EmptyDataText="Nenhum lance.">
            <Columns>
                <asp:BoundField DataField="NR_ORDEM" HeaderText="Ordem" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="DS_COTA" HeaderText="Cota" />
                <asp:BoundField DataField="DS_TIPO" HeaderText="Tipo" />
                <asp:BoundField DataField="PC_LANCE" HeaderText="% Lance" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="PC_EMBUTIDO" HeaderText="% Embutido" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="DS_SITUACAO" HeaderText="Resultado" />
                <asp:BoundField DataField="DS_MOTIVO" HeaderText="Motivo" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
    <br />
    <asp:HyperLink ID="lnkVoltar" runat="server" Text="Voltar às assembleias do grupo" />
</asp:Content>
