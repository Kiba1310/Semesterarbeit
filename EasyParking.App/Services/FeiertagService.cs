using EasyParking.Data;
using EasyParking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyParking.App.Services;

public record FeiertagEingabe(int Id, DateTime Datum, string Bezeichnung, int? ParkhausId);

public class FeiertagService
{
    private readonly EasyParkingDbContext _db;

    public FeiertagService(EasyParkingDbContext db) => _db = db;

    public List<Feiertag> LadeFeiertage() =>
        _db.Feiertage
            .Include(f => f.Parkhaus)
            .AsNoTracking()
            .OrderBy(f => f.Datum).ThenBy(f => f.ParkhausId)
            .ToList();

    public (bool erfolg, string? fehler) Speichere(FeiertagEingabe eingabe)
    {
        if (string.IsNullOrWhiteSpace(eingabe.Bezeichnung))
            return (false, "Bezeichnung darf nicht leer sein.");
        if (eingabe.Datum == default)
            return (false, "Datum muss gesetzt sein.");

        var datumOnly = eingabe.Datum.Date;

        var duplikat = _db.Feiertage.Any(f =>
            f.Id != eingabe.Id &&
            f.Datum == datumOnly &&
            f.ParkhausId == eingabe.ParkhausId);
        if (duplikat)
            return (false, "Für dieses Datum und Parkhaus existiert bereits ein Feiertag.");

        if (eingabe.Id == 0)
        {
            _db.Feiertage.Add(new Feiertag
            {
                Datum = datumOnly,
                Bezeichnung = eingabe.Bezeichnung.Trim(),
                ParkhausId = eingabe.ParkhausId
            });
        }
        else
        {
            var f = _db.Feiertage.FirstOrDefault(x => x.Id == eingabe.Id);
            if (f is null) return (false, "Feiertag nicht gefunden.");
            f.Datum = datumOnly;
            f.Bezeichnung = eingabe.Bezeichnung.Trim();
            f.ParkhausId = eingabe.ParkhausId;
        }

        _db.SaveChanges();
        return (true, null);
    }

    public void Loesche(int id)
    {
        var f = _db.Feiertage.FirstOrDefault(x => x.Id == id);
        if (f is null) return;
        _db.Feiertage.Remove(f);
        _db.SaveChanges();
    }
}
