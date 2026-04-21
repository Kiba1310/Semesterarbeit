using EasyParking.Data;
using EasyParking.Domain.Entities;
using EasyParking.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyParking.App.Services;

public record TarifSpeichernErgebnis(bool Erfolg, IReadOnlyList<string> Fehler);

public class TarifVerwaltungService
{
    private readonly EasyParkingDbContext _db;
    private readonly TarifValidator _validator;

    public TarifVerwaltungService(EasyParkingDbContext db, TarifValidator validator)
    {
        _db = db;
        _validator = validator;
    }

    public List<Tarif> LadeTarife(int parkhausId) =>
        _db.Tarife.AsNoTracking().Where(t => t.ParkhausId == parkhausId).OrderBy(t => t.Typ).ThenBy(t => t.StartStunde).ToList();

    public TarifSpeichernErgebnis SpeichereTarife(int parkhausId, IReadOnlyList<Tarif> neue)
    {
        var ergebnis = _validator.Validiere(neue);
        if (!ergebnis.Gueltig)
            return new TarifSpeichernErgebnis(false, ergebnis.Fehler);

        var alte = _db.Tarife.Where(t => t.ParkhausId == parkhausId).ToList();
        _db.Tarife.RemoveRange(alte);

        foreach (var t in neue)
        {
            _db.Tarife.Add(new Tarif
            {
                ParkhausId = parkhausId,
                Typ = t.Typ,
                StartStunde = t.StartStunde,
                EndStunde = t.EndStunde,
                PreisProStunde = t.PreisProStunde
            });
        }

        _db.SaveChanges();
        return new TarifSpeichernErgebnis(true, Array.Empty<string>());
    }
}
