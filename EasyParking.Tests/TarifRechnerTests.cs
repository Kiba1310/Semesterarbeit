using EasyParking.Domain.Entities;
using EasyParking.Domain.Services;
using FluentAssertions;

namespace EasyParking.Tests;

public class TarifRechnerTests
{
    private static IReadOnlyList<Tarif> StandardTarife() => new List<Tarif>
    {
        new() { Typ = TarifTyp.Wochentag, StartStunde = 0, EndStunde = 5, PreisProStunde = 2.50m },
        new() { Typ = TarifTyp.Wochentag, StartStunde = 6, EndStunde = 8, PreisProStunde = 2.80m },
        new() { Typ = TarifTyp.Wochentag, StartStunde = 9, EndStunde = 17, PreisProStunde = 3.60m },
        new() { Typ = TarifTyp.Wochentag, StartStunde = 18, EndStunde = 20, PreisProStunde = 2.80m },
        new() { Typ = TarifTyp.Wochentag, StartStunde = 21, EndStunde = 23, PreisProStunde = 2.40m },
        new() { Typ = TarifTyp.WochenendeFeiertag, StartStunde = 0, EndStunde = 8, PreisProStunde = 2.40m },
        new() { Typ = TarifTyp.WochenendeFeiertag, StartStunde = 9, EndStunde = 17, PreisProStunde = 3.20m },
        new() { Typ = TarifTyp.WochenendeFeiertag, StartStunde = 18, EndStunde = 23, PreisProStunde = 2.40m }
    };

    private readonly TarifRechner _rechner = new();

    [Fact]
    public void Dauer_Null_Gibt_Null()
    {
        var t = new DateTime(2026, 4, 20, 10, 0, 0);
        _rechner.BerechneBetrag(t, t, StandardTarife(), Array.Empty<DateTime>())
            .Should().Be(0m);
    }

    [Fact]
    public void Viertelstunde_Wochentag_Vormittag_Ist_Ein_Viertel_Stundentarif()
    {
        var eingang = new DateTime(2026, 4, 20, 10, 0, 0);
        var ausgang = eingang.AddMinutes(15);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(0.90m);
    }

    [Fact]
    public void Angebrochene_Viertelstunde_Wird_Ganz_Verrechnet()
    {
        var eingang = new DateTime(2026, 4, 20, 10, 0, 0);
        var ausgang = eingang.AddMinutes(16);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(1.80m);
    }

    [Fact]
    public void Eine_Stunde_Vormittag_Wochentag_Ergibt_3_60()
    {
        var eingang = new DateTime(2026, 4, 20, 10, 0, 0);
        var ausgang = eingang.AddHours(1);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(3.60m);
    }

    [Fact]
    public void Samstag_Vormittag_Nutzt_Wochenendtarif()
    {
        var eingang = new DateTime(2026, 4, 18, 10, 0, 0);
        var ausgang = eingang.AddHours(1);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(3.20m);
    }

    [Fact]
    public void Uebergang_ueber_Tarifgrenze_wird_korrekt_abgerechnet()
    {
        var eingang = new DateTime(2026, 4, 20, 8, 30, 0);
        var ausgang = new DateTime(2026, 4, 20, 9, 30, 0);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(3.20m);
    }

    [Fact]
    public void Feiertag_an_Montag_Nutzt_Feiertagstarif()
    {
        var eingang = new DateTime(2026, 4, 6, 10, 0, 0);
        var ausgang = eingang.AddHours(1);
        var feiertage = new[] { new DateTime(2026, 4, 6) };

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), feiertage);

        betrag.Should().Be(3.20m);
    }

    [Fact]
    public void Parkdauer_Genau_24_Stunden_Noch_Viertelstundentarif()
    {
        var eingang = new DateTime(2026, 4, 20, 0, 0, 0);
        var ausgang = eingang.AddHours(24);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().BeGreaterThan(0m);
        betrag.Should().NotBe(35.00m);
    }

    [Fact]
    public void Parkdauer_Ueber_24_Stunden_Nutzt_Tagespauschale()
    {
        var eingang = new DateTime(2026, 4, 20, 0, 0, 0);
        var ausgang = eingang.AddHours(25);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(70.00m);
    }

    [Fact]
    public void Parkdauer_48_Stunden_Ergibt_Zwei_Tage()
    {
        var eingang = new DateTime(2026, 4, 20, 0, 0, 0);
        var ausgang = eingang.AddHours(48);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(70.00m);
    }

    [Fact]
    public void Parkdauer_49_Stunden_Ergibt_Drei_Tagespauschalen()
    {
        var eingang = new DateTime(2026, 4, 20, 0, 0, 0);
        var ausgang = eingang.AddHours(49);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(105.00m);
    }

    [Fact]
    public void Tarif_am_Anfang_der_Viertelstunde_gilt_fuer_ganze_Viertelstunde()
    {
        var eingang = new DateTime(2026, 4, 20, 8, 45, 0);
        var ausgang = new DateTime(2026, 4, 20, 9, 0, 0);

        var betrag = _rechner.BerechneBetrag(eingang, ausgang, StandardTarife(), Array.Empty<DateTime>());

        betrag.Should().Be(0.70m);
    }
}
