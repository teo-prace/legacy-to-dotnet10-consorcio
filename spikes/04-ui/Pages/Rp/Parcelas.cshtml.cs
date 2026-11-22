using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Poc04.Ui.Dados;

namespace Poc04.Ui.Pages.Rp;

[Authorize(Policy = "Logado")]
public class ParcelasModel(ConsultaParcelas consulta) : PageModel
{
    public static readonly (string Valor, string Texto)[] Situacoes =
        { ("V", "Vencidas em aberto"), ("2", "Emitidas"), ("3", "Pagas"), ("1", "Não emitidas"), ("T", "(todas)") };

    [BindProperty(SupportsGet = true)] public int? Grupo { get; set; }
    [BindProperty(SupportsGet = true)] public string? Situacao { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? De { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Ate { get; set; }
    [BindProperty(SupportsGet = true)] public string? Nn { get; set; }
    [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;

    public FiltroParcelas Filtro { get; private set; } = FiltroParcelas.Padrao;
    public IReadOnlyList<Grupo> Grupos { get; private set; } = [];
    public IReadOnlyList<LinhaParcela> Linhas { get; private set; } = [];
    public int Total { get; private set; }

    public async Task OnGetAsync()
    {
        // ACHADO (teste de paridade): "situacao=" vazio vira null no binding e caía no padrão "vencidas".
        // Parâmetro ausente = vencidas (padrão da tela); presente e vazio = todas.
        var situacao = Situacao ?? (Request.Query.ContainsKey("situacao") ? "T" : "V");
        Filtro = new FiltroParcelas(Grupo, situacao, De, Ate, Nn, Math.Max(1, Pagina));
        Grupos = await consulta.GruposAsync();
        (Linhas, Total) = await consulta.PesquisarAsync(Filtro);
    }

    public string UrlPagina(int pagina) =>
        Url.Page("/Rp/Parcelas", new { grupo = Filtro.Grupo, situacao = Filtro.Situacao, de = Filtro.De?.ToString("yyyy-MM-dd"),
                                       ate = Filtro.Ate?.ToString("yyyy-MM-dd"), nn = Filtro.NossoNumero, pagina })!;
}
