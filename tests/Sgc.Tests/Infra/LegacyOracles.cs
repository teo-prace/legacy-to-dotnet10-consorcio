using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Sgc.Tests.Infra;

/// <summary>The legacy VB.NET code, executed by tools/oracle on .NET Framework 4.8.</summary>
public static class VbNetOracle
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string[] Run(IReadOnlyList<string> cases)
    {
        var exe = TestEnvironment.PathFromRoot("tools", "oracle", "bin", "Debug", "net48", "Oracle.exe");
        if (!File.Exists(exe)) throw new InvalidOperationException("Build the oracle first: dotnet build tools\\oracle");
        var input = Path.GetTempFileName();
        File.WriteAllLines(input, cases);
        var psi = new ProcessStartInfo("cmd.exe", $"/c \"\"{exe}\" < \"{input}\"\"")
        { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        p.WaitForExit();
        File.Delete(input);
        Assert.Equal(cases.Count, output.Length);
        return output;
    }

    public static string BoletoCase(DateTime due, decimal value, string nossoNumero) =>
        $"B|{due:yyyy-MM-dd}|{value.ToString(Inv)}|{nossoNumero}";

    public static string LateFeeCase(decimal value, DateTime due, DateTime paid) =>
        $"E|{value.ToString(Inv)}|{due:yyyy-MM-dd}|{paid:yyyy-MM-dd}";

    public static (decimal Fine, decimal Interest) ParseLateFee(string s)
    {
        var x = s.Split(';');
        return (decimal.Parse(x[0], Inv), decimal.Parse(x[1], Inv));
    }
}

/// <summary>
/// The portal's VB6 component (SGCBoleto.dll) through COM.
/// Needs `powershell legacy\vb6\SGCBoleto\compilar.ps1` once (builds and registers it).
/// </summary>
public sealed class Vb6Oracle
{
    readonly object _boleto = Activator.CreateInstance(Type.GetTypeFromProgID("SGCBoleto.clsBoleto", true)!)!;
    readonly object _lateFees = Activator.CreateInstance(Type.GetTypeFromProgID("SGCBoleto.clsEncargos", true)!)!;

    static object? Call(object o, string method, params object[] args) =>
        o.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, o, args);

    public string TypeableLine(DateTime due, decimal value, string nossoNumero) =>
        (string)Call(_boleto, "LinhaDigitavel", (string)Call(_boleto, "CodigoBarras", due, value, "0001", "17", nossoNumero, "123456")!)!;

    public (decimal Fine, decimal Interest) LateFee(decimal value, DateTime due, DateTime paid) =>
        (Convert.ToDecimal(Call(_lateFees, "Multa", value, due, paid)), Convert.ToDecimal(Call(_lateFees, "Juros", value, due, paid)));
}
