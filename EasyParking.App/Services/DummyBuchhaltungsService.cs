using EasyParking.Domain.Services;

namespace EasyParking.App.Services;

public class DummyBuchhaltungsService : IBuchhaltungsService
{
    public void BucheUmsatz(string referenz, decimal betrag, DateTime datum, string kategorie)
    {
        Console.WriteLine($"[Buchhaltung] {datum:dd.MM.yyyy}  {kategorie,-20}  CHF {betrag,8:F2}  ref={referenz}");
    }
}
