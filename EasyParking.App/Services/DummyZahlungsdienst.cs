using EasyParking.Domain.Services;

namespace EasyParking.App.Services;

public class DummyZahlungsdienst : IZahlungsdienst
{
    public ZahlungsErgebnis FuehreZahlungDurch(decimal betrag, string referenz)
    {
        var txId = "TX-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        Console.WriteLine($"[Zahlung] OK  CHF {betrag,8:F2}  ref={referenz}  tx={txId}");
        return new ZahlungsErgebnis(true, txId, null);
    }
}
