using EasyParking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyParking.Data;

public static class Seeder
{
    public static void EnsureSeeded(EasyParkingDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Parkhaeuser.Any()) return;

        var ph1 = new Parkhaus
        {
            Name = "Zürich City",
            Stadt = "Zürich",
            Gruppe = "Deutschschweiz",
            Stockwerke = BaueStockwerke(new[] { 20, 30, 25 }),
            Tarife = StandardTarife()
        };

        var ph2 = new Parkhaus
        {
            Name = "Bern Bahnhof",
            Stadt = "Bern",
            Gruppe = "Deutschschweiz",
            Stockwerke = BaueStockwerke(new[] { 15, 25, 20 }),
            Tarife = StandardTarife()
        };

        db.Parkhaeuser.AddRange(ph1, ph2);
        db.SaveChanges();

        BaueDauermieter(db, ph1, new[]
        {
            ("DM-ZH-001", "Hans", "Müller"),
            ("DM-ZH-002", "Anna", "Meier"),
            ("DM-ZH-003", "Peter", "Weber")
        });

        BaueDauermieter(db, ph2, new[]
        {
            ("DM-BE-001", "Maria", "Schmid"),
            ("DM-BE-002", "Thomas", "Keller"),
            ("DM-BE-003", "Laura", "Huber")
        });

        db.Feiertage.AddRange(
            new Feiertag { Datum = new DateTime(DateTime.Now.Year, 1, 1), Bezeichnung = "Neujahr" },
            new Feiertag { Datum = new DateTime(DateTime.Now.Year, 8, 1), Bezeichnung = "Bundesfeier" },
            new Feiertag { Datum = new DateTime(DateTime.Now.Year, 12, 25), Bezeichnung = "Weihnachten" },
            new Feiertag { Datum = new DateTime(DateTime.Now.Year, 12, 26), Bezeichnung = "Stephanstag" }
        );

        db.SaveChanges();
    }

    private static List<Stockwerk> BaueStockwerke(int[] plaetzeProEtage)
    {
        var liste = new List<Stockwerk>();
        for (var i = 0; i < plaetzeProEtage.Length; i++)
        {
            var nr = i + 1;
            var sw = new Stockwerk { Nummer = nr, Bezeichnung = $"Etage {nr}" };
            for (var p = 1; p <= plaetzeProEtage[i]; p++)
            {
                sw.Parkplaetze.Add(new Parkplatz
                {
                    Nummer = p,
                    Typ = ParkplatzTyp.Gelegenheitsnutzer,
                    Status = ParkplatzStatus.Frei
                });
            }
            liste.Add(sw);
        }
        return liste;
    }

    private static List<Tarif> StandardTarife() => new()
    {
        new Tarif { Typ = TarifTyp.Wochentag, StartStunde = 0, EndStunde = 5, PreisProStunde = 2.50m },
        new Tarif { Typ = TarifTyp.Wochentag, StartStunde = 6, EndStunde = 8, PreisProStunde = 2.80m },
        new Tarif { Typ = TarifTyp.Wochentag, StartStunde = 9, EndStunde = 17, PreisProStunde = 3.60m },
        new Tarif { Typ = TarifTyp.Wochentag, StartStunde = 18, EndStunde = 20, PreisProStunde = 2.80m },
        new Tarif { Typ = TarifTyp.Wochentag, StartStunde = 21, EndStunde = 23, PreisProStunde = 2.40m },
        new Tarif { Typ = TarifTyp.WochenendeFeiertag, StartStunde = 0, EndStunde = 8, PreisProStunde = 2.40m },
        new Tarif { Typ = TarifTyp.WochenendeFeiertag, StartStunde = 9, EndStunde = 17, PreisProStunde = 3.20m },
        new Tarif { Typ = TarifTyp.WochenendeFeiertag, StartStunde = 18, EndStunde = 23, PreisProStunde = 2.40m }
    };

    private static void BaueDauermieter(EasyParkingDbContext db, Parkhaus ph, (string code, string vn, string nn)[] daten)
    {
        var stockwerk = ph.Stockwerke.First();
        db.Entry(stockwerk).Collection(s => s.Parkplaetze).Load();

        var freiePlaetze = stockwerk.Parkplaetze.OrderBy(p => p.Nummer).Take(daten.Length).ToList();

        for (var i = 0; i < daten.Length; i++)
        {
            var (code, vn, nn) = daten[i];
            var platz = freiePlaetze[i];
            platz.Typ = ParkplatzTyp.Dauermieter;

            var mieter = new Dauermieter
            {
                ParkhausId = ph.Id,
                Code = code,
                Vorname = vn,
                Nachname = nn,
                FesterParkplatzId = platz.Id,
                Gesperrt = false
            };
            db.Dauermieter.Add(mieter);
            db.SaveChanges();

            mieter.Mietzahlungen.Add(new Mietzahlung
            {
                DauermieterId = mieter.Id,
                Jahr = DateTime.Now.Year,
                Monat = DateTime.Now.Month,
                Betrag = 200.00m,
                Zahldatum = DateTime.Now.AddDays(-10)
            });
        }
        db.SaveChanges();
    }
}
