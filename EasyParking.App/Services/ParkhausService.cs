using EasyParking.Data;
using EasyParking.Domain.Entities;
using EasyParking.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyParking.App.Services;

public class ParkhausService
{
    private readonly EasyParkingDbContext _db;
    private readonly IParkplatzZuteiler _zuteiler;
    private readonly ITarifRechner _rechner;
    private readonly IMietPruefer _mietPruefer;
    private readonly IZahlungsdienst _zahlung;
    private readonly IBuchhaltungsService _buchhaltung;
    private readonly ParkhausValidator _parkhausValidator;

    public ParkhausService(
        EasyParkingDbContext db,
        IParkplatzZuteiler zuteiler,
        ITarifRechner rechner,
        IMietPruefer mietPruefer,
        IZahlungsdienst zahlung,
        IBuchhaltungsService buchhaltung,
        ParkhausValidator parkhausValidator)
    {
        _db = db;
        _zuteiler = zuteiler;
        _rechner = rechner;
        _mietPruefer = mietPruefer;
        _zahlung = zahlung;
        _buchhaltung = buchhaltung;
        _parkhausValidator = parkhausValidator;
    }

    public List<Parkhaus> LadeParkhaeuser()
    {
        return _db.Parkhaeuser
            .Include(p => p.Stockwerke)
                .ThenInclude(s => s.Parkplaetze)
                    .ThenInclude(pp => pp.Dauermieter)
            .AsNoTracking()
            .ToList();
    }

    public Parkhaus? LadeParkhaus(int id)
    {
        return _db.Parkhaeuser
            .Include(p => p.Stockwerke)
                .ThenInclude(s => s.Parkplaetze)
                    .ThenInclude(pp => pp.Dauermieter)
            .FirstOrDefault(p => p.Id == id);
    }

    public Parkticket? ErstelleGelegenheitsticket(int parkhausId, DateTime jetzt)
    {
        var ph = _db.Parkhaeuser
            .Include(p => p.Stockwerke)
                .ThenInclude(s => s.Parkplaetze)
            .FirstOrDefault(p => p.Id == parkhausId);
        if (ph is null) return null;

        var platz = _zuteiler.FindeFreienPlatz(ph.Stockwerke);
        if (platz is null) return null;

        platz.Status = ParkplatzStatus.Besetzt;

        var ticket = new Parkticket
        {
            TicketNummer = $"T-{jetzt:yyyyMMddHHmmss}-{platz.Id}",
            ParkhausId = parkhausId,
            ParkplatzId = platz.Id,
            Kategorie = Kundenkategorie.Gelegenheitsnutzer,
            EingangsZeit = jetzt,
            Bezahlt = false
        };
        _db.Parktickets.Add(ticket);
        _db.SaveChanges();

        _db.Entry(ticket).Reference(t => t.Parkplatz).Load();
        if (ticket.Parkplatz != null)
            _db.Entry(ticket.Parkplatz).Reference(pp => pp.Stockwerk).Load();
        return ticket;
    }

    public (Parkticket? ticket, string? fehler) ErstelleDauermieterticket(int parkhausId, string code, DateTime jetzt)
    {
        var mieter = _db.Dauermieter
            .Include(m => m.Mietzahlungen)
            .Include(m => m.FesterParkplatz!)
                .ThenInclude(pp => pp.Stockwerk)
            .FirstOrDefault(m => m.ParkhausId == parkhausId && m.Code == code);
        if (mieter is null) return (null, $"Code '{code}' unbekannt.");

        _mietPruefer.AktualisiereSperrung(mieter, jetzt);
        if (mieter.Gesperrt)
        {
            _db.SaveChanges();
            return (null, $"Kunde {mieter.Anzeigename} ist wegen nicht bezahlter Miete gesperrt.");
        }

        if (mieter.FesterParkplatz is null) return (null, "Diesem Dauermieter ist kein Platz zugewiesen.");
        if (mieter.FesterParkplatz.Status == ParkplatzStatus.Besetzt)
            return (null, "Ihr Platz ist bereits in Benutzung (offenes Ticket vorhanden).");

        mieter.FesterParkplatz.Status = ParkplatzStatus.Besetzt;

        var ticket = new Parkticket
        {
            TicketNummer = $"DM-{jetzt:yyyyMMddHHmmss}-{mieter.Id}",
            ParkhausId = parkhausId,
            ParkplatzId = mieter.FesterParkplatzId!.Value,
            DauermieterId = mieter.Id,
            Kategorie = Kundenkategorie.Dauermieter,
            EingangsZeit = jetzt,
            Bezahlt = true
        };
        _db.Parktickets.Add(ticket);
        _db.SaveChanges();

        return (ticket, null);
    }

    public (Parkticket? ticket, string? fehler) EntwerteTicket(string ticketNummer, DateTime jetzt)
    {
        var ticket = _db.Parktickets
            .Include(t => t.Parkplatz)
            .Include(t => t.Parkhaus!).ThenInclude(p => p.Tarife)
            .Include(t => t.Dauermieter)
            .FirstOrDefault(t => t.TicketNummer == ticketNummer);

        if (ticket is null) return (null, "Ticket nicht gefunden.");
        if (ticket.AusgangsZeit is not null) return (null, "Ticket wurde bereits entwertet.");

        // (a) AusgangsZeit setzen
        ticket.AusgangsZeit = jetzt;

        if (ticket.Kategorie == Kundenkategorie.Dauermieter)
        {
            if (ticket.Parkplatz is not null)
                ticket.Parkplatz.Status = ParkplatzStatus.Frei;
            _db.SaveChanges();
            return (ticket, null);
        }

        // Gelegenheitsnutzer
        // (b) Betrag berechnen (global + parkhaus-spezifische Feiertage)
        var parkhausId = ticket.ParkhausId;
        var feiertage = _db.Feiertage
            .Where(f => f.ParkhausId == null || f.ParkhausId == parkhausId)
            .Select(f => f.Datum)
            .ToList();
        ticket.Betrag = _rechner.BerechneBetrag(ticket.EingangsZeit, jetzt, ticket.Parkhaus!.Tarife, feiertage);

        // (c) Zahlung anstossen
        var ergebnis = _zahlung.FuehreZahlungDurch(ticket.Betrag, ticket.TicketNummer);

        if (!ergebnis.Erfolg)
        {
            // (e) Rollback: tracked Änderungen verwerfen, Platz bleibt besetzt
            _db.Entry(ticket).Reload();
            var fehlerText = ergebnis.Fehler ?? "Unbekannter Fehler.";
            return (null, $"Zahlung fehlgeschlagen: {fehlerText}");
        }

        // (d) Nur bei Erfolg: Ticket bezahlt, Platz frei, Buchhaltung
        ticket.Bezahlt = true;
        if (ticket.Parkplatz is not null)
            ticket.Parkplatz.Status = ParkplatzStatus.Frei;
        _db.SaveChanges();

        _buchhaltung.BucheUmsatz(ticket.TicketNummer, ticket.Betrag, jetzt, "Gelegenheitsnutzer");

        return (ticket, null);
    }

    public List<Parkticket> OffeneTickets(int parkhausId)
    {
        return _db.Parktickets
            .Include(t => t.Parkplatz!).ThenInclude(pp => pp.Stockwerk)
            .Include(t => t.Dauermieter)
            .Where(t => t.ParkhausId == parkhausId && t.AusgangsZeit == null)
            .OrderBy(t => t.EingangsZeit)
            .ToList();
    }

    public List<Dauermieter> LadeDauermieter(int parkhausId)
    {
        return _db.Dauermieter
            .Include(m => m.FesterParkplatz!).ThenInclude(p => p.Stockwerk)
            .Include(m => m.Mietzahlungen)
            .Where(m => m.ParkhausId == parkhausId)
            .ToList();
    }

    public void ErfasseMietzahlung(int dauermieterId, int jahr, int monat, decimal betrag)
    {
        var mieter = _db.Dauermieter.Include(m => m.Mietzahlungen).First(m => m.Id == dauermieterId);
        mieter.Mietzahlungen.Add(new Mietzahlung
        {
            Jahr = jahr,
            Monat = monat,
            Betrag = betrag,
            Zahldatum = DateTime.Now
        });
        _mietPruefer.AktualisiereSperrung(mieter, DateTime.Now);
        _db.SaveChanges();
    }

    public (int? id, IReadOnlyList<string> fehler) SpeichereParkhaus(
        int id,
        string name,
        string stadt,
        string? gruppe,
        IReadOnlyList<StockwerkEingabe> stockwerke)
    {
        var val = _parkhausValidator.Validiere(name, stockwerke);
        if (!val.Gueltig) return (null, val.Fehler);

        Parkhaus? ph;
        if (id == 0)
        {
            ph = new Parkhaus { Name = name, Stadt = stadt, Gruppe = gruppe };
            _db.Parkhaeuser.Add(ph);
            _db.SaveChanges();
            ph.Tarife = StandardTarife().Select(t => { t.ParkhausId = ph.Id; return t; }).ToList();
            _db.Tarife.AddRange(ph.Tarife);
            _db.SaveChanges();
        }
        else
        {
            ph = _db.Parkhaeuser
                .Include(p => p.Stockwerke).ThenInclude(s => s.Parkplaetze)
                .FirstOrDefault(p => p.Id == id);
            if (ph is null) return (null, new[] { $"Parkhaus mit Id {id} nicht gefunden." });
            ph.Name = name;
            ph.Stadt = stadt;
            ph.Gruppe = gruppe;
        }

        var bestehendeStockwerke = ph.Stockwerke.ToList();
        var eingabeIds = stockwerke.Where(s => s.Id != 0).Select(s => s.Id).ToHashSet();

        foreach (var alt in bestehendeStockwerke)
        {
            if (!eingabeIds.Contains(alt.Id))
            {
                var hatBelegtePlaetze = alt.Parkplaetze.Any(p => p.Status == ParkplatzStatus.Besetzt);
                if (hatBelegtePlaetze)
                    return (null, new[] { $"Stockwerk {alt.Nummer} kann nicht entfernt werden: Plätze sind besetzt." });
                _db.Stockwerke.Remove(alt);
            }
        }

        foreach (var sw in stockwerke)
        {
            if (sw.Id == 0)
            {
                var neu = new Stockwerk
                {
                    ParkhausId = ph.Id,
                    Nummer = sw.Nummer,
                    Bezeichnung = string.IsNullOrWhiteSpace(sw.Bezeichnung) ? $"Etage {sw.Nummer}" : sw.Bezeichnung
                };
                for (var p = 1; p <= sw.AnzahlPlaetze; p++)
                {
                    neu.Parkplaetze.Add(new Parkplatz
                    {
                        Nummer = p,
                        Typ = ParkplatzTyp.Gelegenheitsnutzer,
                        Status = ParkplatzStatus.Frei
                    });
                }
                _db.Stockwerke.Add(neu);
            }
            else
            {
                var existing = bestehendeStockwerke.First(x => x.Id == sw.Id);
                existing.Nummer = sw.Nummer;
                existing.Bezeichnung = sw.Bezeichnung;

                var aktuelleAnzahl = existing.Parkplaetze.Count;
                if (sw.AnzahlPlaetze > aktuelleAnzahl)
                {
                    var maxNr = existing.Parkplaetze.Any() ? existing.Parkplaetze.Max(p => p.Nummer) : 0;
                    for (var p = maxNr + 1; p <= maxNr + (sw.AnzahlPlaetze - aktuelleAnzahl); p++)
                    {
                        existing.Parkplaetze.Add(new Parkplatz
                        {
                            Nummer = p,
                            Typ = ParkplatzTyp.Gelegenheitsnutzer,
                            Status = ParkplatzStatus.Frei
                        });
                    }
                }
                else if (sw.AnzahlPlaetze < aktuelleAnzahl)
                {
                    var zuEntfernen = existing.Parkplaetze
                        .OrderByDescending(p => p.Nummer)
                        .Take(aktuelleAnzahl - sw.AnzahlPlaetze)
                        .ToList();
                    if (zuEntfernen.Any(p => p.Status == ParkplatzStatus.Besetzt || p.Typ == ParkplatzTyp.Dauermieter))
                        return (null, new[] { $"Stockwerk {existing.Nummer}: höchste Plätze sind belegt/Dauermieter, Reduktion nicht möglich." });
                    foreach (var p in zuEntfernen) _db.Parkplaetze.Remove(p);
                }
            }
        }

        _db.SaveChanges();
        return (ph.Id, Array.Empty<string>());
    }

    public (bool erfolg, string? fehler) LoescheParkhaus(int parkhausId)
    {
        var ph = _db.Parkhaeuser
            .Include(p => p.Stockwerke).ThenInclude(s => s.Parkplaetze)
            .Include(p => p.Dauermieter)
            .Include(p => p.Parktickets)
            .FirstOrDefault(p => p.Id == parkhausId);
        if (ph is null) return (false, "Parkhaus nicht gefunden.");

        if (ph.Parktickets.Any(t => t.AusgangsZeit == null))
            return (false, "Parkhaus hat offene Tickets und kann nicht gelöscht werden.");
        if (ph.Dauermieter.Any())
            return (false, "Parkhaus hat zugewiesene Dauermieter und kann nicht gelöscht werden.");

        _db.Parktickets.RemoveRange(ph.Parktickets);
        _db.Parkhaeuser.Remove(ph);
        _db.SaveChanges();
        return (true, null);
    }

    public List<Parkplatz> FreieParkplaetzeFuerDauermieter(int parkhausId, int? einschliessenDauermieterId = null)
    {
        var query = _db.Parkplaetze
            .Include(p => p.Stockwerk)
            .Include(p => p.Dauermieter)
            .Where(p => p.Stockwerk!.ParkhausId == parkhausId);

        var liste = query.ToList();
        return liste
            .Where(p =>
                (p.Typ == ParkplatzTyp.Gelegenheitsnutzer && p.Status == ParkplatzStatus.Frei) ||
                (einschliessenDauermieterId.HasValue && p.DauermieterId == einschliessenDauermieterId.Value))
            .OrderBy(p => p.Stockwerk!.Nummer).ThenBy(p => p.Nummer)
            .ToList();
    }

    public (int? id, string? fehler) NeuerDauermieter(int parkhausId, string code, string vorname, string nachname, int parkplatzId)
    {
        if (string.IsNullOrWhiteSpace(code)) return (null, "Code darf nicht leer sein.");
        if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(nachname))
            return (null, "Vorname und Nachname dürfen nicht leer sein.");

        if (_db.Dauermieter.Any(m => m.ParkhausId == parkhausId && m.Code == code))
            return (null, $"Code '{code}' ist bereits vergeben.");

        var platz = _db.Parkplaetze.Include(p => p.Stockwerk).FirstOrDefault(p => p.Id == parkplatzId);
        if (platz is null || platz.Stockwerk?.ParkhausId != parkhausId)
            return (null, "Ungültiger Parkplatz.");
        if (platz.Typ == ParkplatzTyp.Dauermieter)
            return (null, "Platz ist bereits einem anderen Dauermieter zugewiesen.");
        if (platz.Status == ParkplatzStatus.Besetzt)
            return (null, "Platz ist aktuell besetzt.");

        var mieter = new Dauermieter
        {
            ParkhausId = parkhausId,
            Code = code.Trim(),
            Vorname = vorname.Trim(),
            Nachname = nachname.Trim(),
            FesterParkplatzId = platz.Id
        };
        _db.Dauermieter.Add(mieter);
        platz.Typ = ParkplatzTyp.Dauermieter;
        _db.SaveChanges();
        return (mieter.Id, null);
    }

    public (bool erfolg, string? fehler) BearbeiteDauermieter(int id, string code, string vorname, string nachname, int parkplatzId)
    {
        if (string.IsNullOrWhiteSpace(code)) return (false, "Code darf nicht leer sein.");
        if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(nachname))
            return (false, "Vorname und Nachname dürfen nicht leer sein.");

        var mieter = _db.Dauermieter
            .Include(m => m.FesterParkplatz)
            .FirstOrDefault(m => m.Id == id);
        if (mieter is null) return (false, "Dauermieter nicht gefunden.");

        if (_db.Dauermieter.Any(m => m.Id != id && m.ParkhausId == mieter.ParkhausId && m.Code == code))
            return (false, $"Code '{code}' ist bereits vergeben.");

        var neuerPlatz = _db.Parkplaetze.Include(p => p.Stockwerk).FirstOrDefault(p => p.Id == parkplatzId);
        if (neuerPlatz is null || neuerPlatz.Stockwerk?.ParkhausId != mieter.ParkhausId)
            return (false, "Ungültiger Parkplatz.");

        if (parkplatzId != mieter.FesterParkplatzId)
        {
            if (neuerPlatz.Typ == ParkplatzTyp.Dauermieter)
                return (false, "Zielplatz ist bereits einem anderen Dauermieter zugewiesen.");
            if (neuerPlatz.Status == ParkplatzStatus.Besetzt)
                return (false, "Zielplatz ist aktuell besetzt.");

            if (mieter.FesterParkplatz is not null)
            {
                mieter.FesterParkplatz.Typ = ParkplatzTyp.Gelegenheitsnutzer;
                mieter.FesterParkplatzId = null;
            }
            neuerPlatz.Typ = ParkplatzTyp.Dauermieter;
            mieter.FesterParkplatzId = neuerPlatz.Id;
        }

        mieter.Code = code.Trim();
        mieter.Vorname = vorname.Trim();
        mieter.Nachname = nachname.Trim();
        _db.SaveChanges();
        return (true, null);
    }

    public (bool erfolg, string? fehler) LoescheDauermieter(int id)
    {
        var mieter = _db.Dauermieter
            .Include(m => m.FesterParkplatz)
            .FirstOrDefault(m => m.Id == id);
        if (mieter is null) return (false, "Dauermieter nicht gefunden.");

        var hatOffenesTicket = _db.Parktickets.Any(t => t.DauermieterId == id && t.AusgangsZeit == null);
        if (hatOffenesTicket) return (false, "Dauermieter hat ein offenes Ticket und kann nicht gelöscht werden.");

        if (mieter.FesterParkplatz is not null)
        {
            mieter.FesterParkplatz.Typ = ParkplatzTyp.Gelegenheitsnutzer;
            mieter.FesterParkplatzId = null;
        }

        _db.Dauermieter.Remove(mieter);
        _db.SaveChanges();
        return (true, null);
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
}
