<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="GrupoEdicao.aspx.vb" Inherits="Grupos_GrupoEdicao" Title="SGC - Grupo" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server" Text="Novo grupo" /></h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />

    <%-- novo grupo --%>
    <asp:Panel ID="pnlNovo" runat="server">
        <div class="quadro">
            <table class="form">
                <tr>
                    <td class="rotulo">Plano:</td>
                    <td><asp:DropDownList ID="ddlPlano" runat="server" CssClass="campo" AutoPostBack="true" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Número do grupo:</td>
                    <td><asp:TextBox ID="txtNumero" runat="server" CssClass="campo" Width="70px" MaxLength="6" onkeypress="return somenteNumeros(event)" />
                        <span style="color:#777">(sugerido pelo tipo de bem)</span></td>
                </tr>
                <tr>
                    <td class="rotulo">Data de abertura:</td>
                    <td><asp:TextBox ID="txtAbertura" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" /></td>
                </tr>
            </table>
            <div style="color:#777">Os parâmetros do plano (taxas, prazo, lances, bens) são copiados para o grupo e passam a valer pelo regulamento.</div>
        </div>
        <asp:Button ID="btnCriar" runat="server" Text="Criar grupo" CssClass="botaoPrincipal" />
        <asp:Button ID="btnVoltarNovo" runat="server" Text="Voltar" CssClass="botao" />
    </asp:Panel>

    <%-- grupo existente --%>
    <asp:Panel ID="pnlExistente" runat="server" Visible="false">
        <div class="quadro">
        <table class="form">
            <tr>
                <td class="rotulo">Plano:</td><td><asp:HyperLink ID="lnkPlano" runat="server" /></td>
                <td class="rotulo">Situação:</td><td><b><asp:Literal ID="litSituacao" runat="server" /></b></td>
            </tr>
            <tr>
                <td class="rotulo">Tipo de bem:</td><td><asp:Literal ID="litTipo" runat="server" /></td>
                <td class="rotulo">Prazo:</td><td><asp:Literal ID="litPrazo" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Taxa de administração:</td><td><asp:Literal ID="litTaxa" runat="server" /></td>
                <td class="rotulo">Fundo de reserva:</td><td><asp:Literal ID="litFundo" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Lance embutido máx.:</td><td><asp:Literal ID="litEmbutido" runat="server" /></td>
                <td class="rotulo">Lance fixo:</td><td><asp:Literal ID="litLanceFixo" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Cotas (ativas / mín. / máx.):</td><td><asp:Literal ID="litCotas" runat="server" /></td>
                <td class="rotulo">Dia venc. / assembleia:</td><td><asp:Literal ID="litDias" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Abertura:</td><td><asp:Literal ID="litAbertura" runat="server" /></td>
                <td class="rotulo">1ª assembleia:</td><td><asp:Literal ID="litConstituicao" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Índice / regulamento:</td><td colspan="3"><asp:Literal ID="litRegulamento" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Bens permitidos:</td><td colspan="3"><asp:Literal ID="litBens" runat="server" /></td>
            </tr>
        </table>
        </div>

        <asp:Panel ID="pnlConstituir" runat="server" Visible="false" CssClass="quadro">
            <b>Constituição do grupo</b><br />
            <asp:Literal ID="litQuorum" runat="server" /><br /><br />
            Data da 1ª assembleia: <asp:TextBox ID="txtPrimeiraAGO" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" />
            &nbsp; <asp:Button ID="btnConstituir" runat="server" Text="Constituir grupo" CssClass="botaoPrincipal"
                OnClientClick="return confirma('Constituir o grupo? Os vencimentos das parcelas serão gerados para todas as cotas.');" />
        </asp:Panel>

        <h2>Saldos do grupo</h2>
        <asp:GridView ID="gvSaldos" runat="server" AutoGenerateColumns="false" CssClass="grid">
            <Columns>
                <asp:BoundField DataField="SG_FUNDO" HeaderText="Fundo" />
                <asp:BoundField DataField="NM_FUNDO" HeaderText="Descrição" />
                <asp:BoundField DataField="VL_SALDO" HeaderText="Saldo (R$)" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            </Columns>
        </asp:GridView>
        <br />
        <asp:HyperLink ID="lnkMapa" runat="server" Text="Mapa de cotas" /> &nbsp;|&nbsp;
        <asp:HyperLink ID="lnkExtrato" runat="server" Text="Extrato do grupo" /> &nbsp;|&nbsp;
        <a href="Grupos.aspx">Voltar</a>
    </asp:Panel>
</asp:Content>
