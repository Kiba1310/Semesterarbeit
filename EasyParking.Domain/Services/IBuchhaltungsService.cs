namespace EasyParking.Domain.Services;

public interface IBuchhaltungsService
{
    void BucheUmsatz(string referenz, decimal betrag, DateTime datum, string kategorie);
}
