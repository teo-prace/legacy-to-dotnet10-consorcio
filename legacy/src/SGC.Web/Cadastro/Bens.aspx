<%@ Page Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Bens.aspx.vb" Inherits="Cadastro_Bens" Title="SGC - Bens" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cphConteudo" runat="server">
    <h1>Bens / créditos de referência</h1>
    <div class="filtro">
        Tipo:
        <asp:DropDownList ID="ddlTipo" runat="server" CssClass="campo" AutoPostBack="true">
            <asp:ListItem Value="0" Text="(todos)" />
            <asp:ListItem Value="1" Text="Imóvel" />
            <asp:ListItem Value="2" Text="Veículo leve" />
            <asp:ListItem Value="3" Text="Veículo pesado" />
            <asp:ListItem Value="4" Text="Serviços" />
        </asp:DropDownList>
        &nbsp; <asp:Button ID="btnNovo" runat="server" Text="Novo bem" CssClass="botao" />
    </div>

    <asp:SqlDataSource ID="sdsBens" runat="server" ConnectionString="<%$ ConnectionStrings:SGC %>"
        SelectCommand="SELECT B.CD_BEM, B.DS_BEM, B.ID_TIPO_BEM, I.SG_INDICE, B.VL_BEM, B.FL_ATIVO,
                              (SELECT MAX(V.DT_VIGENCIA) FROM TB_BEM_VALOR V WHERE V.CD_BEM = B.CD_BEM) AS DT_VIGENCIA
                         FROM TB_BEM B INNER JOIN TB_INDICE I ON I.CD_INDICE = B.CD_INDICE
                        WHERE (@ID_TIPO_BEM = 0 OR B.ID_TIPO_BEM = @ID_TIPO_BEM)
                        ORDER BY B.ID_TIPO_BEM, B.VL_BEM">
        <SelectParameters>
            <asp:ControlParameter Name="ID_TIPO_BEM" ControlID="ddlTipo" PropertyName="SelectedValue" Type="Int32" />
        </SelectParameters>
    </asp:SqlDataSource>

    <asp:GridView ID="gvBens" runat="server" DataSourceID="sdsBens" AutoGenerateColumns="false" CssClass="grid" AlternatingRowStyle-CssClass="alt">
        <Columns>
            <asp:BoundField DataField="CD_BEM" HeaderText="Código" ItemStyle-CssClass="num" />
            <asp:HyperLinkField DataTextField="DS_BEM" HeaderText="Descrição" DataNavigateUrlFields="CD_BEM" DataNavigateUrlFormatString="BemEdicao.aspx?id={0}" />
            <asp:TemplateField HeaderText="Tipo">
                <ItemTemplate><%# DescricaoTipoBem(Eval("ID_TIPO_BEM")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="SG_INDICE" HeaderText="Índice" />
            <asp:BoundField DataField="VL_BEM" HeaderText="Valor atual" DataFormatString="{0:N2}" ItemStyle-CssClass="num" />
            <asp:BoundField DataField="DT_VIGENCIA" HeaderText="Vigência" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="FL_ATIVO" HeaderText="Ativo" ItemStyle-CssClass="centro" />
        </Columns>
    </asp:GridView>
</asp:Content>
