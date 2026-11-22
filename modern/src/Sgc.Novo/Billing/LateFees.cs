namespace Sgc.Novo.Billing;

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
}

public sealed record LateFee(decimal Fine, decimal Interest, int DaysLate)
{
    public decimal Total(decimal installment) => installment + Fine + Interest;
}

/// <summary>
/// Fines and interest: ONE rule for the back office, the batch and the portal (the legacy system has four,
/// see assessment/findings.md R1 to R4). Decided in ADR-005:
///   - no fees when paid up to the next business day after the due date, holidays included
///   - fine = value × fine %; interest = value × monthly % / 30 × days since the due date
///   - half a cent rounds up (away from zero), like SQL Server, the batch and the bank return file
/// </summary>
public sealed class LateFeeCalculator(BusinessCalendar calendar, decimal finePercent = 2m, decimal monthlyInterestPercent = 1m,
                                      MidpointRounding rounding = MidpointRounding.AwayFromZero)
{
    public LateFee Calculate(decimal value, DateTime dueDate, DateTime paidOn)
    {
        var (fine, interest, days) = CalculateExact(value, dueDate, paidOn);
        return new(decimal.Round(fine, 2, rounding), decimal.Round(interest, 2, rounding), days);
    }

    /// <summary>Before rounding: what goes into the rounding record.</summary>
    public (decimal Fine, decimal Interest, int DaysLate) CalculateExact(decimal value, DateTime dueDate, DateTime paidOn)
    {
        if (paidOn.Date <= calendar.NextBusinessDay(dueDate)) return (0, 0, 0);
        var days = (paidOn.Date - dueDate.Date).Days;
        return (value * finePercent / 100m, value * monthlyInterestPercent / 100m / 30m * days, days);
    }

    /// <summary>The last day the installment can be paid without fees.</summary>
    public DateTime FeeFreeUntil(DateTime dueDate) => calendar.NextBusinessDay(dueDate);
}
