using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Sgc.Novo.Billing;

public enum PostingStatus { Posted, Refused, Conflict }

/// <param name="Message">The procedure's message (business rule) or the conflict, shown to the user.</param>
/// <param name="Attempts">1 = first try; more than 1 = there was a deadlock and a retry.</param>
public sealed record PostingResult(PostingStatus Status, string Message, int Attempts);

/// <summary>What goes into novo.TB_ENCARGO_ARREDONDAMENTO (ADR-005).</summary>
public sealed record RoundingRecord(DateTime PaidOn, int DaysLate, decimal Installment,
    decimal ExactFine, decimal ExactInterest, decimal Fine, decimal Interest, decimal BankersFine, decimal BankersInterest);

/// <summary>
/// Posting a payment from the new screens. While old and new coexist, the write goes **only through the
/// legacy procedure** (SP_BAIXA_PARCELA, ADR-006): fund postings, settlement and the double-payment guard
/// stay in one place.
///
/// What this layer adds:
///   - fees calculated by the single rule (LateFeeCalculator, half up);
///   - retry on deadlock (error 1205); SQL Server has already rolled back the whole transaction;
///   - business errors (RAISERROR, number 50000) become results, "already paid" becomes Conflict;
///   - the rounding record, in the same transaction as the payment.
/// </summary>
public sealed class PaymentPosting(string connectionString, int maxAttempts = 4)
{
    public const byte ManualOrigin = 1;
    public const string RoundingRule = "meio-para-cima";

    /// <summary>Fees calculated here (never trusted from the browser), rounding recorded when there are fees.</summary>
    public Task<PostingResult> PostAsync(int installmentId, DateTime dueDate, decimal installment, DateTime paidOn,
                                         decimal amountPaid, int userId, LateFeeCalculator rule, CancellationToken ct = default)
    {
        var (exactFine, exactInterest, days) = rule.CalculateExact(installment, dueDate, paidOn);
        var charged = rule.Calculate(installment, dueDate, paidOn);
        RoundingRecord? record = null;
        if (charged.Fine + charged.Interest > 0)
            record = new(paidOn.Date, days, installment, exactFine, exactInterest, charged.Fine, charged.Interest,
                         decimal.Round(exactFine, 2, MidpointRounding.ToEven), decimal.Round(exactInterest, 2, MidpointRounding.ToEven));
        return PostAsync(installmentId, paidOn, amountPaid, charged.Fine, charged.Interest, userId, record, ct);
    }

    public async Task<PostingResult> PostAsync(int installmentId, DateTime paidOn, decimal amountPaid, decimal fine, decimal interest,
                                               int userId, RoundingRecord? record = null, CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var cn = new SqlConnection(connectionString);
                await cn.OpenAsync(ct);
                // the procedure opens its own transaction (nested in this one): the record goes in with the payment or not at all
                await using var tx = (SqlTransaction)await cn.BeginTransactionAsync(ct);
                await cn.ExecuteAsync(new CommandDefinition("SP_BAIXA_PARCELA", new
                {
                    CD_PARCELA = installmentId,
                    DT_PAGAMENTO = paidOn.Date,
                    VL_PAGO = amountPaid,
                    VL_MULTA = fine,
                    VL_JUROS = interest,
                    ID_ORIGEM = ManualOrigin,
                    CD_USUARIO = userId,
                }, tx, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                if (record is not null)
                    await cn.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO novo.TB_ENCARGO_ARREDONDAMENTO (CD_PARCELA, DT_PAGAMENTO, QT_DIAS_ATRASO, VL_PARCELA, VL_MULTA_EXATA, VL_JUROS_EXATO,
                               VL_MULTA, VL_JUROS, VL_MULTA_BANCARIO, VL_JUROS_BANCARIO, DS_REGRA, CD_USUARIO)
                        VALUES (@installmentId, @PaidOn, @DaysLate, @Installment, @ExactFine, @ExactInterest,
                               @Fine, @Interest, @BankersFine, @BankersInterest, @rule, @userId)
                        """,
                        new
                        {
                            installmentId, record.PaidOn, record.DaysLate, record.Installment, record.ExactFine, record.ExactInterest,
                            record.Fine, record.Interest, record.BankersFine, record.BankersInterest, rule = RoundingRule, userId,
                        }, tx, cancellationToken: ct));
                await tx.CommitAsync(ct);
                return new(PostingStatus.Posted, "Parcela baixada.", attempt);
            }
            catch (SqlException e) when (e.Number == 1205 && attempt < maxAttempts)
            {
                await Task.Delay(Random.Shared.Next(10, 60) * attempt, ct);
            }
            catch (SqlException e) when (e.Number == 50000)
            {
                var conflict = e.Message.Contains("ja paga", StringComparison.OrdinalIgnoreCase)
                            || e.Message.Contains("outro processo", StringComparison.OrdinalIgnoreCase);
                return new(conflict ? PostingStatus.Conflict : PostingStatus.Refused, e.Message, attempt);
            }
        }
    }
}
