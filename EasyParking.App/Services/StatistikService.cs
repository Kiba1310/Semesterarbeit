using EasyParking.Data;
using EasyParking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyParking.App.Services;

public record UmsatzZeile(string Titel, decimal Gelegenheit, decimal Dauermiete, decimal Total);

public record ZeitAuswertungZeile(DateTime Zeitpunkt, string Typ, string ParkhausName, string PlatzInfo, string Kunde);

public class StatistikService
{
    private readonly EasyParkingDbContext _db;
    public StatistikService(EasyParkingDbContext db) => _db = db;

    public List<UmsatzZeile> MonatsUmsatz(int jahr, int monat, int? parkhausId, string? gruppe)
    {
        var query = _db.Parktickets.AsQueryable().Where(t => t.AusgangsZeit != null &&
            t.AusgangsZeit!.Value.Year == jahr && t.AusgangsZeit.Value.Month == monat);
        if (parkhausId.HasValue) query = query.Where(t => t.ParkhausId == parkhausId.Value);
        if (gruppe != null) query = query.Where(t => t.Parkhaus!.Gruppe == gruppe);

        var gelegenheit = query.Where(t => t.Kategorie == Kundenkategorie.Gelegenheitsnutzer).Sum(t => (decimal?)t.Betrag) ?? 0m;

        var dauermieteQuery = _db.Mietzahlungen.AsQueryable().Where(z => z.Jahr == jahr && z.Monat == monat);
        if (parkhausId.HasValue) dauermieteQuery = dauermieteQuery.Where(z => z.Dauermieter!.ParkhausId == parkhausId.Value);
        if (gruppe != null) dauermieteQuery = dauermieteQuery.Where(z => z.Dauermieter!.Parkhaus!.Gruppe == gruppe);
        var dauermiete = dauermieteQuery.Sum(z => (decimal?)z.Betrag) ?? 0m;

        var titel = $"{jahr}-{monat:D2}";
        return new List<UmsatzZeile>
        {
            new(titel, gelegenheit, dauermiete, gelegenheit + dauermiete)
        };
    }

    public List<UmsatzZeile> JahresUmsatzProMonat(int jahr, int? parkhausId, string? gruppe)
    {
        var result = new List<UmsatzZeile>();
        for (int m = 1; m <= 12; m++)
        {
            var zeile = MonatsUmsatz(jahr, m, parkhausId, gruppe).First();
            result.Add(new UmsatzZeile(zeile.Titel, zeile.Gelegenheit, zeile.Dauermiete, zeile.Total));
        }
        return result;
    }

    public List<ZeitAuswertungZeile> ZeitAuswertung(DateTime von, DateTime bis, Kundenkategorie? kategorie, int? parkhausId)
    {
        var query = _db.Parktickets
            .Include(t => t.Parkhaus)
            .Include(t => t.Parkplatz!).ThenInclude(p => p.Stockwerk)
            .Include(t => t.Dauermieter)
            .Where(t => t.EingangsZeit >= von && t.EingangsZeit <= bis);
        if (kategorie.HasValue) query = query.Where(t => t.Kategorie == kategorie.Value);
        if (parkhausId.HasValue) query = query.Where(t => t.ParkhausId == parkhausId.Value);

        var tickets = query.OrderBy(t => t.EingangsZeit).ToList();
        var zeilen = new List<ZeitAuswertungZeile>();
        foreach (var t in tickets)
        {
            var platzInfo = t.Parkplatz != null && t.Parkplatz.Stockwerk != null
                ? $"Etage {t.Parkplatz.Stockwerk.Nummer}, Platz {t.Parkplatz.Nummer}"
                : "-";
            var kunde = t.Dauermieter?.Anzeigename ?? "Gelegenheitsnutzer";

            zeilen.Add(new ZeitAuswertungZeile(t.EingangsZeit, "Eingang", t.Parkhaus!.Name, platzInfo, kunde));
            if (t.AusgangsZeit.HasValue && t.AusgangsZeit.Value >= von && t.AusgangsZeit.Value <= bis)
            {
                zeilen.Add(new ZeitAuswertungZeile(t.AusgangsZeit.Value, "Ausgang", t.Parkhaus.Name, platzInfo, kunde));
            }
        }
        return zeilen.OrderBy(z => z.Zeitpunkt).ToList();
    }

    public List<string> AlleGruppen() =>
        _db.Parkhaeuser.Where(p => p.Gruppe != null).Select(p => p.Gruppe!).Distinct().ToList();
}
