using EasyParking.Domain.Entities;
using EasyParking.Domain.Services;
using FluentAssertions;

namespace EasyParking.Tests;

public class MietPrueferTests
{
    private readonly MietPruefer _pruefer = new();

    [Fact]
    public void Vor_dem_15_Nicht_faellig()
    {
        var mieter = new Dauermieter();
        var stichtag = new DateTime(2026, 4, 14);
        _pruefer.IstFaellig(mieter, stichtag).Should().BeFalse();
    }

    [Fact]
    public void Am_15_Ohne_Zahlung_Faellig()
    {
        var mieter = new Dauermieter();
        var stichtag = new DateTime(2026, 4, 15);
        _pruefer.IstFaellig(mieter, stichtag).Should().BeTrue();
    }

    [Fact]
    public void Am_15_Mit_Zahlung_Nicht_Faellig()
    {
        var mieter = new Dauermieter();
        mieter.Mietzahlungen.Add(new Mietzahlung { Jahr = 2026, Monat = 4, Betrag = 200m, Zahldatum = new DateTime(2026, 4, 5) });
        var stichtag = new DateTime(2026, 4, 15);
        _pruefer.IstFaellig(mieter, stichtag).Should().BeFalse();
    }

    [Fact]
    public void AktualisiereSperrung_Setzt_Gesperrt_Korrekt()
    {
        var mieter = new Dauermieter();
        _pruefer.AktualisiereSperrung(mieter, new DateTime(2026, 4, 20));
        mieter.Gesperrt.Should().BeTrue();

        mieter.Mietzahlungen.Add(new Mietzahlung { Jahr = 2026, Monat = 4, Betrag = 200m, Zahldatum = DateTime.Now });
        _pruefer.AktualisiereSperrung(mieter, new DateTime(2026, 4, 20));
        mieter.Gesperrt.Should().BeFalse();
    }
}
