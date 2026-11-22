// Legacy oracle: reads cases from stdin and answers with what the legacy code does, one line per case.
//   B|yyyy-mm-dd|value|nossoNumero   -> typeable line (SGC.Negocio.Boleto)
//   E|value|yyyy-mm-dd|yyyy-mm-dd     -> fine;interest (SGC.Batch.Encargos, a Friend module)
// All cases go in one process: 300,000 cases take about 2 seconds.
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

static class Oracle
{
    static void Main()
    {
        var inv = CultureInfo.InvariantCulture;
        var lateFees = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SGC.Batch.exe"))
                               .GetType("SGC.Batch.Encargos", true);   // batch modules are Friend
        var fine = lateFees.GetMethod("Multa", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var interest = lateFees.GetMethod("Juros", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var c = line.Split('|');
            if (c[0] == "B")
            {
                var barcode = SGC.Negocio.Boleto.CodigoBarras(DateTime.Parse(c[1], inv), decimal.Parse(c[2], inv), "0001", "17", c[3], "123456");
                Console.WriteLine(SGC.Negocio.Boleto.LinhaDigitavel(barcode));
            }
            else
            {
                var args = new object[] { decimal.Parse(c[1], inv), DateTime.Parse(c[2], inv), DateTime.Parse(c[3], inv) };
                Console.WriteLine(((decimal)fine.Invoke(null, args)).ToString(inv) + ";" + ((decimal)interest.Invoke(null, args)).ToString(inv));
            }
        }
    }
}
