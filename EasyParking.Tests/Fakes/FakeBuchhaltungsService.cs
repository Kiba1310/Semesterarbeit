using EasyParking.Domain.Services;

namespace EasyParking.Tests.Fakes;

internal class FakeBuchhaltungsService : IBuchhaltungsService
{
    public List<(string referenz, decimal betrag, DateTime datum, string kategorie)> Eintraege { get; } = new();

    public void BucheUmsatz(string referenz, decimal betrag, DateTime datum, string kategorie)
    {
        Eintraege.Add((referenz, betrag, datum, kategorie));
    }
}
