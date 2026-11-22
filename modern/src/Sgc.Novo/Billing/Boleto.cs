using System.Globalization;

namespace Sgc.Novo.Billing;

/// <summary>
/// Boleto for bank 999 (FEBRABAN layout). One implementation to replace the two legacy ones
/// (SGC.Negocio.Boleto in VB.NET and SGCBoleto.clsBoleto in VB6), proven equal to the VB.NET one over every
/// issued installment (tests/Sgc.Tests/Modern).
/// Free field: branch(4) portfolio(2) nosso número(11) account(7) "0".
/// </summary>
public static class Boleto
{
    public const string Bank = "999";
    const string Currency = "9";
    static readonly DateTime FactorBase = new(1997, 10, 7);

    /// <summary>Days since 07/10/1997; from 10000 on it restarts at 1000 (22/02/2025 = 1000), in 9000-day cycles.</summary>
    public static string DueDateFactor(DateTime dueDate)
    {
        var days = (dueDate.Date - FactorBase).Days;
        if (days >= 10000) days = (days - 1000) % 9000 + 1000;
        return days.ToString("0000", CultureInfo.InvariantCulture);
    }

    public static string FreeField(string branch, string portfolio, string nossoNumero, string account) =>
        branch.PadLeft(4, '0') + portfolio.PadLeft(2, '0') + nossoNumero.PadLeft(11, '0') + account.PadLeft(7, '0') + "0";

    public static string Barcode(DateTime dueDate, decimal value, string branch, string portfolio, string nossoNumero, string account)
    {
        var cents = ((long)decimal.Round(value * 100, 0)).ToString("0000000000", CultureInfo.InvariantCulture);
        var withoutCheckDigit = Bank + Currency + DueDateFactor(dueDate) + cents + FreeField(branch, portfolio, nossoNumero, account);
        return withoutCheckDigit[..4] + GeneralCheckDigit(withoutCheckDigit) + withoutCheckDigit[4..];
    }

    /// <summary>AAAAA.AAAAA BBBBB.BBBBBB CCCCC.CCCCCC D EEEEEEEEEEEEEE</summary>
    public static string TypeableLine(string barcode)
    {
        var f1 = barcode[..4] + barcode.Substring(19, 5);
        var f2 = barcode.Substring(24, 10);
        var f3 = barcode.Substring(34, 10);
        f1 += Modulo10(f1); f2 += Modulo10(f2); f3 += Modulo10(f3);
        return $"{f1[..5]}.{f1[5..]} {f2[..5]}.{f2[5..]} {f3[..5]}.{f3[5..]} {barcode[4]} {barcode.Substring(5, 14)}";
    }

    public static char Modulo10(string number)
    {
        int sum = 0, weight = 2;
        for (var i = number.Length - 1; i >= 0; i--)
        {
            var p = (number[i] - '0') * weight;
            sum += p > 9 ? p - 9 : p;
            weight = weight == 2 ? 1 : 2;
        }
        return (char)('0' + (10 - sum % 10) % 10);
    }

    /// <summary>Modulo 11, weights 2 to 9; 0, 10 and 11 become 1.</summary>
    public static char GeneralCheckDigit(string number)
    {
        int sum = 0, weight = 2;
        for (var i = number.Length - 1; i >= 0; i--)
        {
            sum += (number[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }
        var dv = 11 - sum % 11;
        return dv is 0 or 10 or 11 ? '1' : (char)('0' + dv);
    }
}
