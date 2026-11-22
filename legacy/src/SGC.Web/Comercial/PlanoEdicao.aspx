<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="PlanoEdicao.aspx.vb" Inherits="Comercial_PlanoEdicao" Title="SGC - Plano" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server" Text="Novo plano" /></h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />

    <div class="quadro">
    <table class="form">
        <tr>
            <td class="rotulo">Nome:</td>
            <td colspan="3"><asp:TextBox ID="txtNome" runat="server" CssClass="campo" Width="300px" MaxLength="80" /></td>
        </tr>
        <tr>
            <td class="rotulo">Tipo de bem:</td>
            <td><asp:DropDownList ID="ddlTipo" runat="server" CssClass="campo" AutoPostBack="true">
                    <asp:ListItem Value="1" Text="Imóvel" />
                    <asp:ListItem Value="2" Text="Veículo leve" />
                    <asp:ListItem Value="3" Text="Veículo pesado" />
                    <asp:ListItem Value="4" Text="Serviços" />
                </asp:DropDownList></td>
            <td class="rotulo">Prazo (meses):</td>
            <td><asp:TextBox ID="txtPrazo" runat="server" CssClass="campo" Width="50px" onkeypress="return somenteNumeros(event)" /></td>
        </tr>
        <tr>
            <td class="rotulo">Taxa de administração %:</td>
            <td><asp:TextBox ID="txtTaxaAdm" runat="server" CssClass="campo" Width="60px" /></td>
            <td class="rotulo">Antecipação da taxa %:</td>
            <td><asp:TextBox ID="txtAntecipacao" runat="server" CssClass="campo" Width="60px" /> <span style="color:#777">(na 1ª parcela)</span></td>
        </tr>
        <tr>
            <td class="rotulo">Fundo de reserva %:</td>
            <td><asp:TextBox ID="txtFundoReserva" runat="server" CssClass="campo" Width="60px" /></td>
            <td class="rotulo">Índice:</td>
            <td><asp:DropDownList ID="ddlIndice" runat="server" CssClass="campo" /></td>
        </tr>
        <tr>
            <td class="rotulo">Lance embutido máx. %:</td>
            <td><asp:TextBox ID="txtEmbutido" runat="server" CssClass="campo" Width="60px" /></td>
            <td class="rotulo">Lance fixo %:</td>
            <td><asp:TextBox ID="txtLanceFixo" runat="server" CssClass="campo" Width="60px" /> <span style="color:#777">(vazio = não tem)</span></td>
        </tr>
        <tr>
            <td class="rotulo">Cotas mínimo / máximo:</td>
            <td><asp:TextBox ID="txtMinimo" runat="server" CssClass="campo" Width="50px" onkeypress="return somenteNumeros(event)" /> /
                <asp:TextBox ID="txtMaximo" runat="server" CssClass="campo" Width="50px" onkeypress="return somenteNumeros(event)" /></td>
            <td class="rotulo">Dia venc. / assembleia:</td>
            <td><asp:TextBox ID="txtDiaVencimento" runat="server" CssClass="campo" Width="30px" onkeypress="return somenteNumeros(event)" /> /
                <asp:TextBox ID="txtDiaAssembleia" runat="server" CssClass="campo" Width="30px" onkeypress="return somenteNumeros(event)" /></td>
        </tr>
        <tr>
            <td class="rotulo">Versão do regulamento:</td>
            <td><asp:TextBox ID="txtRegulamento" runat="server" CssClass="campo" Width="120px" MaxLength="20" /></td>
            <td class="rotulo">Ativo:</td>
            <td><asp:CheckBox ID="chkAtivo" runat="server" Checked="true" /></td>
        </tr>
        <tr>
            <td class="rotulo" style="vertical-align: top">Bens permitidos:</td>
            <td colspan="3"><asp:CheckBoxList ID="cblBens" runat="server" RepeatColumns="2" /></td>
        </tr>
    </table>
    </div>
    <asp:Button ID="btnGravar" runat="server" Text="Gravar" CssClass="botaoPrincipal" />
    <asp:Button ID="btnVoltar" runat="server" Text="Voltar" CssClass="botao" />

    <asp:Panel ID="pnlGrupos" runat="server" Visible="false">
        <h2>Grupos deste plano</h2>
        <asp:GridView ID="gvGrupos" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" EmptyDataText="Nenhum grupo.">
            <Columns>
                <asp:HyperLinkField DataTextField="NR_GRUPO" HeaderText="Grupo" DataNavigateUrlFields="CD_GRUPO" DataNavigateUrlFormatString="~/Grupos/GrupoEdicao.aspx?id={0}" />
                <asp:TemplateField HeaderText="Situação">
                    <ItemTemplate><%# DescricaoSituacaoGrupo(Eval("ID_SITUACAO")) %></ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="PC_TAXA_ADM" HeaderText="Taxa adm %" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="PC_FUNDO_RESERVA" HeaderText="F. reserva %" DataFormatString="{0:0.00}" ItemStyle-CssClass="num" />
                <asp:BoundField DataField="DS_VERSAO_REGULAMENTO" HeaderText="Regulamento" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
</asp:Content>
