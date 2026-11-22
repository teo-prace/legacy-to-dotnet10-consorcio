using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Poc06.Escrita;

public enum StatusBaixa { Baixada, Recusada, Conflito }

/// <param name="Mensagem">Mensagem da procedure (regra de negócio) ou do conflito.</param>
/// <param name="Tentativas">1 = primeira; mais que 1 = houve deadlock e nova tentativa.</param>
public sealed record ResultadoBaixa(StatusBaixa Status, string Mensagem, int Tentativas);

/// <summary>
/// Escrita da tela nova no banco compartilhado: <b>só pela procedure do legado</b> (SP_BAIXA_PARCELA).
/// A tela nova não faz UPDATE em tabela do legado enquanto as duas convivem: a regra de gravação
/// (fundos, quitação, trava contra baixa dupla) continua num lugar só.
///
/// O que a camada nova acrescenta:
///   - nova tentativa em deadlock (erro 1205): a procedure é vítima de deadlock entre baixas da mesma
///     cota contemplada (POC-06); a transação inteira já foi desfeita pelo SQL Server, então repetir é seguro;
///   - erro de negócio (RAISERROR, número 50000) vira resultado, não exceção;
///   - "baixa dupla" (a parcela foi paga por outro processo entre a leitura e a gravação) vira Conflito.
/// </summary>
public sealed class BaixaParcelas(string conexao, int maxTentativas = 4)
{
    public const byte OrigemManual = 1;

    public async Task<ResultadoBaixa> BaixarAsync(int cdParcela, DateTime pagamento, decimal valorPago,
                                                  decimal multa, decimal juros, int cdUsuario, CancellationToken ct = default)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                await using var c = new SqlConnection(conexao);
                await c.ExecuteAsync(new CommandDefinition("SP_BAIXA_PARCELA", new
                {
                    CD_PARCELA = cdParcela,
                    DT_PAGAMENTO = pagamento.Date,
                    VL_PAGO = valorPago,
                    VL_MULTA = multa,
                    VL_JUROS = juros,
                    ID_ORIGEM = OrigemManual,
                    CD_USUARIO = cdUsuario,
                }, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                return new(StatusBaixa.Baixada, "Parcela baixada.", tentativa);
            }
            catch (SqlException e) when (e.Number == 1205 && tentativa < maxTentativas)
            {
                await Task.Delay(Random.Shared.Next(10, 60) * tentativa, ct);
            }
            catch (SqlException e) when (e.Number == 50000)
            {
                var conflito = e.Message.Contains("ja paga", StringComparison.OrdinalIgnoreCase)
                            || e.Message.Contains("outro processo", StringComparison.OrdinalIgnoreCase);
                return new(conflito ? StatusBaixa.Conflito : StatusBaixa.Recusada, e.Message, tentativa);
            }
        }
    }
}
