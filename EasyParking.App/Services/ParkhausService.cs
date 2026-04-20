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

    public ParkhausService(EasyParkingDbContext db, IParkplatzZuteiler zuteiler, ITarifRechner rechner, IMietPruefer mietPruefer)
    {
        _db = db;
        _zuteiler = zuteiler;
        _rechner = rechner;
        _mietPruefer = mietPruefer;
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

        ticket.AusgangsZeit = jetzt;

        if (ticket.Kategorie == Kundenkategorie.Gelegenheitsnutzer)
        {
            var feiertage = _db.Feiertage.Select(f => f.Datum).ToList();
            ticket.Betrag = _rechner.BerechneBetrag(ticket.EingangsZeit, jetzt, ticket.Parkhaus!.Tarife, feiertage);
            ticket.Bezahlt = true;
        }

        if (ticket.Parkplatz is not null)
            ticket.Parkplatz.Status = ParkplatzStatus.Frei;

        _db.SaveChanges();
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
}
