<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="PropostaEdicao.aspx.vb" Inherits="Comercial_PropostaEdicao" Title="SGC - Proposta" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server" Text="Nova proposta de adesão" /></h1>
    <asp:Label ID="lblMensagem" runat="server" Visible="false" />

    <%-- ===================== NOVA PROPOSTA ===================== --%>
    <asp:Panel ID="pnlNova" runat="server">
        <h2>1. Consorciado</h2>
        <div class="quadro">
            CPF: <asp:TextBox ID="txtCPF" runat="server" CssClass="campo" Width="110px" MaxLength="14" onkeyup="mascaraCPF(this)" />
            <asp:Button ID="btnBuscarCPF" runat="server" Text="Buscar" CssClass="botao" />
            &nbsp; <b><asp:Label ID="lblConsorciado" runat="server" /></b>

            <asp:Panel ID="pnlCadastroRapido" runat="server" Visible="false">
                <div class="aviso" style="margin-top:6px">CPF não cadastrado. Preencha o cadastro rápido (o cadastro completo pode ser feito depois).</div>
                <table class="form">
                    <tr>
                        <td class="rotulo">Nome:</td>
                        <td colspan="3"><asp:TextBox ID="txtNome" runat="server" CssClass="campo" Width="300px" MaxLength="120" /></td>
                    </tr>
                    <tr>
                        <td class="rotulo">Nascimento:</td>
                        <td><asp:TextBox ID="txtNascimento" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" /></td>
                        <td class="rotulo">Renda mensal:</td>
                        <td><asp:TextBox ID="txtRenda" runat="server" CssClass="campo" Width="90px" /></td>
                    </tr>
                    <tr>
                        <td class="rotulo">Celular:</td>
                        <td><asp:TextBox ID="txtCelular" runat="server" CssClass="campo" Width="110px" MaxLength="20" /></td>
                        <td class="rotulo">E-mail:</td>
                        <td><asp:TextBox ID="txtEmail" runat="server" CssClass="campo" Width="200px" MaxLength="100" /></td>
                    </tr>
                    <tr>
                        <td class="rotulo">Cidade / UF:</td>
                        <td colspan="3"><asp:TextBox ID="txtCidade" runat="server" CssClass="campo" Width="160px" MaxLength="60" />
                            <asp:DropDownList ID="ddlUF" runat="server" CssClass="campo" /></td>
                    </tr>
                </table>
            </asp:Panel>
        </div>

        <h2>2. Plano, grupo e crédito</h2>
        <div class="quadro">
            <table class="form">
                <tr>
                    <td class="rotulo">Plano:</td>
                    <td><asp:DropDownList ID="ddlPlano" runat="server" CssClass="campo" AutoPostBack="true" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Grupo:</td>
                    <td><asp:DropDownList ID="ddlGrupo" runat="server" CssClass="campo" AutoPostBack="true" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Crédito:</td>
                    <td><asp:DropDownList ID="ddlBem" runat="server" CssClass="campo" AutoPostBack="true" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Ponto de venda:</td>
                    <td><asp:DropDownList ID="ddlPontoVenda" runat="server" CssClass="campo" AutoPostBack="true" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Vendedor:</td>
                    <td><asp:DropDownList ID="ddlVendedor" runat="server" CssClass="campo" /></td>
                </tr>
                <tr>
                    <td class="rotulo">1ª parcela:</td>
                    <td><b><asp:Label ID="lblPrimeiraParcela" runat="server" Text="-" /></b>
                        <asp:Label ID="lblPrazoCota" runat="server" ForeColor="#777" /></td>
                </tr>
                <tr>
                    <td class="rotulo">Observação:</td>
                    <td><asp:TextBox ID="txtObservacao" runat="server" CssClass="campo" Width="400px" TextMode="MultiLine" Rows="2" /></td>
                </tr>
            </table>
        </div>
        <asp:Button ID="btnSalvar" runat="server" Text="Salvar proposta" CssClass="botaoPrincipal" />
        <asp:Button ID="btnVoltarNova" runat="server" Text="Voltar" CssClass="botao" />
    </asp:Panel>

    <%-- ===================== PROPOSTA EXISTENTE ===================== --%>
    <asp:Panel ID="pnlExistente" runat="server" Visible="false">
        <div class="quadro">
        <table class="form">
            <tr>
                <td class="rotulo">Número:</td><td><b><asp:Literal ID="litNumero" runat="server" /></b></td>
                <td class="rotulo">Situação:</td><td><b><asp:Literal ID="litSituacao" runat="server" /></b></td>
            </tr>
            <tr>
                <td class="rotulo">Consorciado:</td><td><asp:HyperLink ID="lnkConsorciado" runat="server" /></td>
                <td class="rotulo">CPF:</td><td><asp:Literal ID="litCPF" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Plano:</td><td><asp:Literal ID="litPlano" runat="server" /></td>
                <td class="rotulo">Grupo:</td><td><asp:HyperLink ID="lnkGrupo" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Crédito:</td><td><asp:Literal ID="litBem" runat="server" /></td>
                <td class="rotulo">Valor do bem na proposta:</td><td><asp:Literal ID="litValorBem" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">1ª parcela:</td><td><b><asp:Literal ID="litPrimeira" runat="server" /></b></td>
                <td class="rotulo">Ponto de venda / vendedor:</td><td><asp:Literal ID="litVenda" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Data da proposta:</td><td><asp:Literal ID="litData" runat="server" /></td>
                <td class="rotulo">Assinatura:</td><td><asp:Literal ID="litAssinatura" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Efetivação:</td><td><asp:Literal ID="litEfetivacao" runat="server" /></td>
                <td class="rotulo">Cota:</td><td><asp:HyperLink ID="lnkCota" runat="server" /></td>
            </tr>
            <tr>
                <td class="rotulo">Observação:</td><td colspan="3"><asp:Literal ID="litObservacao" runat="server" /></td>
            </tr>
        </table>
        </div>

        <asp:Panel ID="pnlDigitada" runat="server" Visible="false">
            <asp:Button ID="btnEnviarPagamento" runat="server" Text="Assinada - liberar para pagamento" CssClass="botaoPrincipal" />
        </asp:Panel>

        <asp:Panel ID="pnlPagamento" runat="server" Visible="false" CssClass="quadro">
            <b>Confirmar pagamento da 1ª parcela</b><br /><br />
            Data do pagamento: <asp:TextBox ID="txtDataPagamento" runat="server" CssClass="campo" Width="80px" onkeyup="mascaraData(this)" />
            &nbsp; Valor pago: <asp:TextBox ID="txtValorPago" runat="server" CssClass="campo" Width="90px" />
            &nbsp; <asp:Button ID="btnConfirmarPagamento" runat="server" Text="Confirmar e efetivar" CssClass="botaoPrincipal"
                OnClientClick="return confirma('Confirma o pagamento e a efetivação da proposta?');" />
        </asp:Panel>

        <br />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar proposta" CssClass="botao" Visible="false"
            OnClientClick="return confirma('Confirma o cancelamento da proposta?');" />
        <asp:Button ID="btnArrependimento" runat="server" Text="Registrar arrependimento" CssClass="botao" Visible="false"
            OnClientClick="return confirma('Registrar arrependimento? A cota será cancelada e o valor estornado.');" />
        <asp:Button ID="btnVoltar" runat="server" Text="Voltar" CssClass="botao" />
    </asp:Panel>
</asp:Content>
