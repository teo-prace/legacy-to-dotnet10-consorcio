using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace Sgc.Novo.Billing;

/// <summary>
/// Fine and interest percentages (TB_PARAMETRO) and holidays (TB_FERIADO), read from the legacy database
/// and cached for 10 minutes. The legacy payment screen reads the same parameters; the batch has 2% and 1%
/// hard-coded (finding R1).
/// </summary>
public sealed class BillingParameters(IConfiguration cfg, IMemoryCache cache)
{
    readonly string _connectionString = cfg.GetConnectionString("SGC")!;

    public Task<LateFeeCalculator> CalculatorAsync() =>
        cache.GetOrCreateAsync("billing:calculator", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            await using var cn = new SqlConnection(_connectionString);
            var holidays = await cn.QueryAsync<DateTime>("SELECT DT_FERIADO FROM TB_FERIADO");
            var p = (await cn.QueryAsync<(string Name, string Value)>(
                "SELECT NM_PARAMETRO, DS_VALOR FROM TB_PARAMETRO WHERE NM_PARAMETRO IN ('PC_MULTA_ATRASO', 'PC_JUROS_MES')"))
                .ToDictionary(x => x.Name, x => decimal.Parse(x.Value, CultureInfo.InvariantCulture));   // stored with a decimal point
            return new LateFeeCalculator(new BusinessCalendar(holidays), p["PC_MULTA_ATRASO"], p["PC_JUROS_MES"]);
        })!;
}
