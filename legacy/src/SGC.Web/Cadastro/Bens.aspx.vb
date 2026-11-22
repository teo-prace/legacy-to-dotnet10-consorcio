Partial Class Cadastro_Bens
    Inherits PaginaBase

    Protected Sub btnNovo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnNovo.Click
        Response.Redirect("BemEdicao.aspx")
    End Sub

End Class
