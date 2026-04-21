using EasyParking.App.Services;
using EasyParking.Data;
using EasyParking.Domain.Entities;
using EasyParking.Domain.Services;
using EasyParking.Tests.Fakes;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EasyParking.Tests;

public class ParkhausServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EasyParkingDbContext _db;
    private readonly ParkhausService _service;
    private readonly FakeZahlungsdienst _zahlung = new();
    private readonly FakeBuchhaltungsService _buchhaltung = new();
    private readonly int _parkhausId;

    public ParkhausServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<EasyParkingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new EasyParkingDbContext(options);
        Seeder.EnsureSeeded(_db);

        _service = new ParkhausService(
            _db,
            new ParkplatzZuteiler(),
            new TarifRechner(),
            new MietPruefer(),
            _zahlung,
            _buchhaltung,
            new ParkhausValidator());

        _parkhausId = _db.Parkhaeuser.First().Id;
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void Entwerten_Gelegenheitsnutzer_Erfolg_Bezahlt_Platz_Frei_Buchhaltung_Eintrag()
    {
        var eingang = new DateTime(2026, 4, 20, 10, 0, 0);
        var ausgang = eingang.AddHours(2);

        var erstellt = _service.ErstelleGelegenheitsticket(_parkhausId, eingang);
        erstellt.Should().NotBeNull();

        var (ticket, fehler) = _service.EntwerteTicket(erstellt!.TicketNummer, ausgang);

        fehler.Should().BeNull();
        ticket.Should().NotBeNull();
        ticket!.Bezahlt.Should().BeTrue();
        ticket.AusgangsZeit.Should().Be(ausgang);
        ticket.Betrag.Should().BeGreaterThan(0m);

        var platz = _db.Parkplaetze.AsNoTracking().First(p => p.Id == erstellt.ParkplatzId);
        platz.Status.Should().Be(ParkplatzStatus.Frei);

        _zahlung.Aufrufe.Should().HaveCount(1);
        _zahlung.Aufrufe[0].betrag.Should().Be(ticket.Betrag);
        _buchhaltung.Eintraege.Should().HaveCount(1);
        _buchhaltung.Eintraege[0].kategorie.Should().Be("Gelegenheitsnutzer");
        _buchhaltung.Eintraege[0].betrag.Should().Be(ticket.Betrag);
    }

    [Fact]
    public void Entwerten_Gelegenheitsnutzer_Zahlungsfehler_Platz_Besetzt_AusgangsZeit_Null_Keine_Buchung()
    {
        _zahlung.NaechstesErgebnis = new ZahlungsErgebnis(false, null, "Karte abgelehnt");
        var eingang = new DateTime(2026, 4, 20, 10, 0, 0);
        var ausgang = eingang.AddHours(2);

        var erstellt = _service.ErstelleGelegenheitsticket(_parkhausId, eingang);
        erstellt.Should().NotBeNull();

        var (ticket, fehler) = _service.EntwerteTicket(erstellt!.TicketNummer, ausgang);

        ticket.Should().BeNull();
        fehler.Should().NotBeNull();
        fehler!.Should().Contain("Zahlung fehlgeschlagen");
        fehler.Should().Contain("Karte abgelehnt");

        var geladen = _db.Parktickets.AsNoTracking().First(t => t.TicketNummer == erstellt.TicketNummer);
        geladen.AusgangsZeit.Should().BeNull();
        geladen.Bezahlt.Should().BeFalse();

        var platz = _db.Parkplaetze.AsNoTracking().First(p => p.Id == erstellt.ParkplatzId);
        platz.Status.Should().Be(ParkplatzStatus.Besetzt);

        _buchhaltung.Eintraege.Should().BeEmpty();
    }

    [Fact]
    public void Doppelte_Entwertung_Wird_Abgelehnt()
    {
        var eingang = new DateTime(2026, 4, 20, 10, 0, 0);
        var erstellt = _service.ErstelleGelegenheitsticket(_parkhausId, eingang);
        _service.EntwerteTicket(erstellt!.TicketNummer, eingang.AddHours(1));

        var (ticket, fehler) = _service.EntwerteTicket(erstellt.TicketNummer, eingang.AddHours(2));

        ticket.Should().BeNull();
        fehler.Should().Contain("bereits entwertet");
    }

    [Fact]
    public void Ungueltiger_Dauermieter_Code_Wird_Abgelehnt()
    {
        var (ticket, fehler) = _service.ErstelleDauermieterticket(_parkhausId, "XXX-INVALID", DateTime.Now);

        ticket.Should().BeNull();
        fehler.Should().Contain("unbekannt");
    }

    [Fact]
    public void Gesperrter_Dauermieter_Wird_Abgelehnt()
    {
        var mieter = _db.Dauermieter
            .Include(m => m.Mietzahlungen)
            .First(m => m.ParkhausId == _parkhausId);

        _db.Mietzahlungen.RemoveRange(mieter.Mietzahlungen);
        _db.SaveChanges();

        var stichtag = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 20);

        var (ticket, fehler) = _service.ErstelleDauermieterticket(_parkhausId, mieter.Code, stichtag);

        ticket.Should().BeNull();
        fehler.Should().Contain("gesperrt");

        var geladen = _db.Dauermieter.AsNoTracking().First(m => m.Id == mieter.Id);
        geladen.Gesperrt.Should().BeTrue();
    }

    [Fact]
    public void Kein_Freier_Parkplatz_Gibt_Null()
    {
        var ph = _db.Parkhaeuser
            .Include(p => p.Stockwerke)
                .ThenInclude(s => s.Parkplaetze)
            .First(p => p.Id == _parkhausId);

        foreach (var platz in ph.Stockwerke.SelectMany(s => s.Parkplaetze)
                                            .Where(pp => pp.Typ == ParkplatzTyp.Gelegenheitsnutzer))
        {
            platz.Status = ParkplatzStatus.Besetzt;
        }
        _db.SaveChanges();

        var ticket = _service.ErstelleGelegenheitsticket(_parkhausId, DateTime.Now);

        ticket.Should().BeNull();
    }

    [Fact]
    public void Entwerten_Dauermieter_Ohne_Zahlungsdienst_Aufruf()
    {
        var mieter = _db.Dauermieter.First(m => m.ParkhausId == _parkhausId);
        var eingang = DateTime.Now;

        var (eintrittsticket, fehler) = _service.ErstelleDauermieterticket(_parkhausId, mieter.Code, eingang);
        fehler.Should().BeNull();
        eintrittsticket.Should().NotBeNull();

        var (ticket, fehler2) = _service.EntwerteTicket(eintrittsticket!.TicketNummer, eingang.AddHours(3));

        fehler2.Should().BeNull();
        ticket!.AusgangsZeit.Should().NotBeNull();
        _zahlung.Aufrufe.Should().BeEmpty();
        _buchhaltung.Eintraege.Should().BeEmpty();
    }

    [Fact]
    public void SpeichereParkhaus_Neu_Erzeugt_Parkplaetze_Korrekt()
    {
        var stockwerke = new List<StockwerkEingabe>
        {
            new(0, 1, "E1", 5),
            new(0, 2, "E2", 8)
        };
        var (id, fehler) = _service.SpeichereParkhaus(0, "Test-PH", "Zürich", "Test", stockwerke);
        fehler.Should().BeEmpty();
        id.Should().NotBeNull();

        var ph = _db.Parkhaeuser
            .Include(p => p.Stockwerke).ThenInclude(s => s.Parkplaetze)
            .AsNoTracking()
            .First(p => p.Id == id!.Value);

        ph.Stockwerke.Should().HaveCount(2);
        ph.Stockwerke.First(s => s.Nummer == 1).Parkplaetze.Should().HaveCount(5);
        ph.Stockwerke.First(s => s.Nummer == 2).Parkplaetze.Should().HaveCount(8);
        ph.Stockwerke.SelectMany(s => s.Parkplaetze).All(p =>
            p.Typ == ParkplatzTyp.Gelegenheitsnutzer && p.Status == ParkplatzStatus.Frei)
            .Should().BeTrue();

        _db.Tarife.AsNoTracking().Count(t => t.ParkhausId == id!.Value).Should().BeGreaterThan(0);
    }

    [Fact]
    public void SpeichereParkhaus_Ohne_Name_Wird_Abgelehnt()
    {
        var stockwerke = new List<StockwerkEingabe> { new(0, 1, "E1", 5) };
        var (id, fehler) = _service.SpeichereParkhaus(0, "", "Zürich", null, stockwerke);
        id.Should().BeNull();
        fehler.Should().Contain(f => f.Contains("Name"));
    }

    [Fact]
    public void LoescheParkhaus_Mit_Offenem_Ticket_Wird_Abgelehnt()
    {
        _service.ErstelleGelegenheitsticket(_parkhausId, DateTime.Now);

        var (erfolg, fehler) = _service.LoescheParkhaus(_parkhausId);

        erfolg.Should().BeFalse();
        fehler.Should().Contain("offene Tickets");
        _db.Parkhaeuser.AsNoTracking().Any(p => p.Id == _parkhausId).Should().BeTrue();
    }

    [Fact]
    public void LoescheParkhaus_Mit_Dauermieter_Wird_Abgelehnt()
    {
        var (erfolg, fehler) = _service.LoescheParkhaus(_parkhausId);

        erfolg.Should().BeFalse();
        fehler.Should().Contain("Dauermieter");
    }

    [Fact]
    public void LoescheDauermieter_Mit_Offenem_Ticket_Wird_Abgelehnt()
    {
        var mieter = _db.Dauermieter.First(m => m.ParkhausId == _parkhausId);
        _service.ErstelleDauermieterticket(_parkhausId, mieter.Code, DateTime.Now);

        var (erfolg, fehler) = _service.LoescheDauermieter(mieter.Id);

        erfolg.Should().BeFalse();
        fehler.Should().Contain("offenes Ticket");
        _db.Dauermieter.AsNoTracking().Any(m => m.Id == mieter.Id).Should().BeTrue();
    }

    [Fact]
    public void LoescheDauermieter_Setzt_Platz_Zurueck_Auf_Gelegenheitsnutzer()
    {
        var mieter = _db.Dauermieter.Include(m => m.FesterParkplatz).First(m => m.ParkhausId == _parkhausId);
        var platzId = mieter.FesterParkplatzId!.Value;

        var (erfolg, _) = _service.LoescheDauermieter(mieter.Id);

        erfolg.Should().BeTrue();
        var platz = _db.Parkplaetze.AsNoTracking().First(p => p.Id == platzId);
        platz.Typ.Should().Be(ParkplatzTyp.Gelegenheitsnutzer);
        platz.DauermieterId.Should().BeNull();
    }

    [Fact]
    public void NeuerDauermieter_Mit_Doppeltem_Code_Wird_Abgelehnt()
    {
        var bestehender = _db.Dauermieter.First(m => m.ParkhausId == _parkhausId);
        var freierPlatz = _service.FreieParkplaetzeFuerDauermieter(_parkhausId).First();

        var (id, fehler) = _service.NeuerDauermieter(_parkhausId, bestehender.Code, "Hans", "Test", freierPlatz.Id);

        id.Should().BeNull();
        fehler.Should().Contain("bereits vergeben");
    }

    [Fact]
    public void Feiertag_Pro_Parkhaus_Gilt_Nur_Fuer_Dieses()
    {
        var parkhausIds = _db.Parkhaeuser.OrderBy(p => p.Id).Select(p => p.Id).ToList();
        var phA = parkhausIds[0];
        var phB = parkhausIds[1];

        // Montag mittags – normalerweise Wochentag-Tarif
        var eingang = new DateTime(2026, 3, 2, 10, 0, 0);
        var ausgang = eingang.AddHours(1);

        _db.Feiertage.RemoveRange(_db.Feiertage);
        _db.Feiertage.Add(new Feiertag { Datum = eingang.Date, Bezeichnung = "Nur A", ParkhausId = phA });
        _db.SaveChanges();

        var tA = _service.ErstelleGelegenheitsticket(phA, eingang)!;
        var tB = _service.ErstelleGelegenheitsticket(phB, eingang)!;

        var (ergA, _) = _service.EntwerteTicket(tA.TicketNummer, ausgang);
        var (ergB, _) = _service.EntwerteTicket(tB.TicketNummer, ausgang);

        // Standardtarife: Wochentag 9-17 = 3.60, Feiertag 9-17 = 3.20
        ergA!.Betrag.Should().Be(3.20m);
        ergB!.Betrag.Should().Be(3.60m);
    }
}
