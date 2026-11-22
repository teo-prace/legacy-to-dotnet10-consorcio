using Dapper;
using Microsoft.Data.SqlClient;
using Sgc.Tests.Infra;

namespace Sgc.Tests.Characterization;

/// <summary>Business days: Monday to Friday, except the holidays in TB_FERIADO.</summary>
public sealed class BusinessCalendar(IEnumerable<DateTime> holidays)
{
    readonly HashSet<DateTime> _holidays = holidays.Select(d => d.Date).ToHashSet();

    public bool IsBusinessDay(DateTime d) => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_holidays.Contains(d.Date);

    public DateTime NextBusinessDay(DateTime d)
    {
        var x = d.Date;
        while (!IsBusinessDay(x)) x = x.AddDays(1);
        return x;
    }

    public static BusinessCalendar FromDatabase()
    {
        using var cn = new SqlConnection(TestEnvironment.ConnectionString);
        return new BusinessCalendar(cn.Query<DateTime>("SELECT DT_FERIADO FROM TB_FERIADO"));
    }
}

/// <summary>
/// What the back-office payment screen (Cobranca/Baixa.aspx.vb) does, written as code so it can be compared
/// with the other implementations over hundreds of thousands of cases. Test C-3 proves, through the
/// browser, that this reproduces the real screen.
///   - no fees when paid up to the next business day after the due date (holidays included)
///   - fine = value x fine %; interest = value x monthly % / 30 x days since the due date
///   - Math.Round default: banker's rounding (half to even)
/// </summary>
public sealed class PaymentScreenRule(BusinessCalendar calendar, decimal finePercent = 2m, decimal monthlyInterestPercent = 1m)
{
    public (decimal Fine, decimal Interest) Calculate(decimal value, DateTime due, DateTime paid)
    {
        if (paid.Date <= calendar.NextBusinessDay(due)) return (0, 0);
        var days = (paid.Date - due.Date).Days;
        return (Math.Round(value * finePercent / 100m, 2), Math.Round(value * monthlyInterestPercent / 100m / 30m * days, 2));
    }
}
