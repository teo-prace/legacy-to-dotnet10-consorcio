<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="ConsorciadoEdicao.aspx.vb" Inherits="Cadastro_ConsorciadoEdicao" Title="SGC - Consorciado" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server" Text="Novo consorciado" /></h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />
    <asp:ValidationSummary ID="vsResumo" runat="server" CssClass="msgErro" HeaderText="Corrija os campos:" />

    <div class="quadro">
    <table class="form">
        <tr>
            <td class="rotulo">CPF:</td>
            <td><asp:TextBox ID="txtCPF" runat="server" CssClass="campo" Width="110px" MaxLength="14" onkeyup="mascaraCPF(this)" />
                <asp:RequiredFieldValidator ID="rfvCPF" runat="server" ControlToValidate="txtCPF" ErrorMessage="CPF" Text="*" />
                <asp:CustomValidator ID="cvCPF" runat="server" ControlToValidate="txtCPF" ClientValidationFunction="validaCPFCampo" ErrorMessage="CPF inválido" Text="*" />
            </td>
            <td class="rotulo">Nome:</td>
            <td><asp:TextBox ID="txtNome" runat="server" CssClass="campo" Width="300px" MaxLength="120" />
                <asp:RequiredFieldValidator ID="rfvNome" runat="server" ControlToValidate="txtNome" ErrorMessage="Nome" Text="*" /></td>
        </tr>
        <tr>
            <td class="rotulo">Nascimento:</td>
            <td><asp:TextBox ID="txtNascimento" runat="server" CssClass="campo" Width="80px" MaxLength="10" onkeyup="mascaraData(this)" /></td>
            <td class="rotulo">RG:</td>
            <td><asp:TextBox ID="txtRG" runat="server" CssClass="campo" Width="120px" MaxLength="15" /></td>
        </tr>
        <tr>
            <td class="rotulo">Estado civil:</td>
            <td><asp:DropDownList ID="ddlEstadoCivil" runat="server" CssClass="campo">
                    <asp:ListItem Value="" Text="" />
                    <asp:ListItem Value="1" Text="Solteiro(a)" />
                    <asp:ListItem Value="2" Text="Casado(a)" />
                    <asp:ListItem Value="3" Text="Divorciado(a)" />
                    <asp:ListItem Value="4" Text="Viúvo(a)" />
                    <asp:ListItem Value="5" Text="União estável" />
                </asp:DropDownList></td>
            <td class="rotulo">Profissão:</td>
            <td><asp:TextBox ID="txtProfissao" runat="server" CssClass="campo" Width="200px" MaxLength="60" /></td>
        </tr>
        <tr>
            <td class="rotulo">Renda mensal:</td>
            <td><asp:TextBox ID="txtRenda" runat="server" CssClass="campo" Width="100px" /></td>
            <td class="rotulo">E-mail:</td>
            <td><asp:TextBox ID="txtEmail" runat="server" CssClass="campo" Width="250px" MaxLength="100" /></td>
        </tr>
        <tr>
            <td class="rotulo">Telefone:</td>
            <td><asp:TextBox ID="txtTelefone" runat="server" CssClass="campo" Width="110px" MaxLength="20" /></td>
            <td class="rotulo">Celular:</td>
            <td><asp:TextBox ID="txtCelular" runat="server" CssClass="campo" Width="110px" MaxLength="20" /></td>
        </tr>
        <tr>
            <td class="rotulo">CEP:</td>
            <td><asp:TextBox ID="txtCEP" runat="server" CssClass="campo" Width="80px" MaxLength="9" /></td>
            <td class="rotulo">Logradouro / nº:</td>
            <td><asp:TextBox ID="txtLogradouro" runat="server" CssClass="campo" Width="250px" MaxLength="100" />
                <asp:TextBox ID="txtNumero" runat="server" CssClass="campo" Width="50px" MaxLength="10" /></td>
        </tr>
        <tr>
            <td class="rotulo">Complemento:</td>
            <td><asp:TextBox ID="txtComplemento" runat="server" CssClass="campo" Width="120px" MaxLength="40" /></td>
            <td class="rotulo">Bairro:</td>
            <td><asp:TextBox ID="txtBairro" runat="server" CssClass="campo" Width="200px" MaxLength="60" /></td>
        </tr>
        <tr>
            <td class="rotulo">Cidade / UF:</td>
            <td colspan="3"><asp:TextBox ID="txtCidade" runat="server" CssClass="campo" Width="200px" MaxLength="60" />
                <asp:DropDownList ID="ddlUF" runat="server" CssClass="campo" /></td>
        </tr>
        <tr>
            <td class="rotulo">Ativo:</td>
            <td colspan="3"><asp:CheckBox ID="chkAtivo" runat="server" Checked="true" /></td>
        </tr>
    </table>
    </div>
    <asp:Button ID="btnGravar" runat="server" Text="Gravar" CssClass="botaoPrincipal" />
    <asp:Button ID="btnVoltar" runat="server" Text="Voltar" CssClass="botao" CausesValidation="false" />

    <asp:Panel ID="pnlCotas" runat="server" Visible="false">
        <h2>Cotas do consorciado</h2>
        <asp:GridView ID="gvCotas" runat="server" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt" EmptyDataText="Nenhuma cota.">
            <Columns>
                <asp:HyperLinkField DataTextField="DS_COTA" HeaderText="Grupo/cota" DataNavigateUrlFields="CD_COTA" DataNavigateUrlFormatString="~/Cotas/CotaDetalhe.aspx?id={0}" />
                <asp:BoundField DataField="DS_BEM" HeaderText="Bem" />
                <asp:BoundField DataField="DT_ADESAO" HeaderText="Adesão" DataFormatString="{0:dd/MM/yyyy}" />
                <asp:TemplateField HeaderText="Situação">
                    <ItemTemplate><%# DescricaoSituacaoCota(Eval("ID_SITUACAO")) %></ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>
</asp:Content>
