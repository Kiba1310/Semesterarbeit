using EasyParking.Domain.Entities;
using EasyParking.Domain.Services;
using FluentAssertions;

namespace EasyParking.Tests;

public class TarifValidatorTests
{
    private readonly TarifValidator _validator = new();

    private static List<Tarif> VollstaendigGueltig() => new()
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

    [Fact]
    public void Vollstaendige_Tarifliste_Ist_Gueltig()
    {
        var result = _validator.Validiere(VollstaendigGueltig());
        result.Gueltig.Should().BeTrue();
        result.Fehler.Should().BeEmpty();
    }

    [Fact]
    public void Luecke_Wird_Erkannt()
    {
        var tarife = VollstaendigGueltig();
        tarife.RemoveAll(t => t.Typ == TarifTyp.Wochentag && t.StartStunde == 9);

        var result = _validator.Validiere(tarife);

        result.Gueltig.Should().BeFalse();
        result.Fehler.Should().Contain(f => f.Contains("nicht abgedeckt") && f.Contains("Wochentag"));
    }

    [Fact]
    public void Ueberlappung_Wird_Erkannt()
    {
        var tarife = VollstaendigGueltig();
        tarife.Add(new Tarif { Typ = TarifTyp.Wochentag, StartStunde = 10, EndStunde = 12, PreisProStunde = 4.00m });

        var result = _validator.Validiere(tarife);

        result.Gueltig.Should().BeFalse();
        result.Fehler.Should().Contain(f => f.Contains("mehrfach abgedeckt"));
    }

    [Fact]
    public void Negativer_Preis_Wird_Erkannt()
    {
        var tarife = VollstaendigGueltig();
        tarife[0].PreisProStunde = -1m;

        var result = _validator.Validiere(tarife);

        result.Gueltig.Should().BeFalse();
        result.Fehler.Should().Contain(f => f.Contains("muss > 0 sein"));
    }

    [Fact]
    public void Null_Preis_Wird_Erkannt()
    {
        var tarife = VollstaendigGueltig();
        tarife[0].PreisProStunde = 0m;

        var result = _validator.Validiere(tarife);

        result.Gueltig.Should().BeFalse();
    }

    [Fact]
    public void Fehlender_Tariftyp_Wird_Erkannt()
    {
        var tarife = VollstaendigGueltig();
        tarife.RemoveAll(t => t.Typ == TarifTyp.WochenendeFeiertag);

        var result = _validator.Validiere(tarife);

        result.Gueltig.Should().BeFalse();
        result.Fehler.Should().Contain(f => f.Contains("Keine Tarife") && f.Contains("WochenendeFeiertag"));
    }

    [Fact]
    public void StartStunde_Groesser_EndStunde_Wird_Erkannt()
    {
        var tarife = new List<Tarif>
        {
            new() { Typ = TarifTyp.Wochentag, StartStunde = 12, EndStunde = 5, PreisProStunde = 2.50m }
        };
        var result = _validator.Validiere(tarife);
        result.Gueltig.Should().BeFalse();
        result.Fehler.Should().Contain(f => f.Contains("grösser"));
    }
}
