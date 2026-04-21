namespace EasyParking.Domain.Services;

public record ZahlungsErgebnis(bool Erfolg, string? TransaktionsId, string? Fehler);

public interface IZahlungsdienst
{
    ZahlungsErgebnis FuehreZahlungDurch(decimal betrag, string referenz);
}
