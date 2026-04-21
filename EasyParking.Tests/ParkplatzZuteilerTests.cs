using EasyParking.Domain.Entities;
using EasyParking.Domain.Services;
using FluentAssertions;

namespace EasyParking.Tests;

public class ParkplatzZuteilerTests
{
    private readonly ParkplatzZuteiler _zuteiler = new();

    private static Stockwerk StockwerkMit(int nummer, int gesamt, int besetzt)
    {
        var sw = new Stockwerk { Nummer = nummer };
        for (var i = 0; i < gesamt; i++)
        {
            sw.Parkplaetze.Add(new Parkplatz
            {
                Nummer = i + 1,
                Typ = ParkplatzTyp.Gelegenheitsnutzer,
                Status = i < besetzt ? ParkplatzStatus.Besetzt : ParkplatzStatus.Frei
            });
        }
        return sw;
    }

    [Fact]
    public void Leere_Liste_Gibt_Null()
    {
        _zuteiler.FindeFreienPlatz(Array.Empty<Stockwerk>()).Should().BeNull();
    }

    [Fact]
    public void Alles_Belegt_Gibt_Null()
    {
        var sw = StockwerkMit(1, 5, 5);
        _zuteiler.FindeFreienPlatz(new[] { sw }).Should().BeNull();
    }

    [Fact]
    public void Waehlt_Stockwerk_Mit_Groesstem_Freianteil()
    {
        var sw1 = StockwerkMit(1, 10, 9);
        var sw2 = StockwerkMit(2, 10, 2);
        var sw3 = StockwerkMit(3, 10, 5);

        var ergebnis = _zuteiler.FindeFreienPlatz(new[] { sw1, sw2, sw3 });

        ergebnis.Should().NotBeNull();
        ergebnis!.Stockwerk.Should().BeNull();
        sw2.Parkplaetze.Should().Contain(ergebnis);
    }

    [Fact]
    public void Ignoriert_Dauermieter_Plaetze()
    {
        var sw = new Stockwerk { Nummer = 1 };
        sw.Parkplaetze.Add(new Parkplatz { Nummer = 1, Typ = ParkplatzTyp.Dauermieter, Status = ParkplatzStatus.Frei });
        sw.Parkplaetze.Add(new Parkplatz { Nummer = 2, Typ = ParkplatzTyp.Gelegenheitsnutzer, Status = ParkplatzStatus.Frei });

        var ergebnis = _zuteiler.FindeFreienPlatz(new[] { sw });

        ergebnis!.Nummer.Should().Be(2);
    }

    [Fact]
    public void Waehlt_Platz_Mit_Niedrigster_Nummer_Im_Gewaehlten_Stockwerk()
    {
        var sw = StockwerkMit(1, 5, 0);

        var ergebnis = _zuteiler.FindeFreienPlatz(new[] { sw });

        ergebnis!.Nummer.Should().Be(1);
    }
}
