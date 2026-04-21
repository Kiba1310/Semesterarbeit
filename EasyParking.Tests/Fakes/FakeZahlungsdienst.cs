using EasyParking.Domain.Services;

namespace EasyParking.Tests.Fakes;

internal class FakeZahlungsdienst : IZahlungsdienst
{
    public ZahlungsErgebnis NaechstesErgebnis { get; set; } = new(true, "TX-TEST", null);
    public List<(decimal betrag, string referenz)> Aufrufe { get; } = new();

    public ZahlungsErgebnis FuehreZahlungDurch(decimal betrag, string referenz)
    {
        Aufrufe.Add((betrag, referenz));
        return NaechstesErgebnis;
    }
}
