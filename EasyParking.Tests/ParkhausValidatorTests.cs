using EasyParking.Domain.Services;
using FluentAssertions;

namespace EasyParking.Tests;

public class ParkhausValidatorTests
{
    private readonly ParkhausValidator _v = new();

    [Fact]
    public void Leerer_Name_Wird_Erkannt()
    {
        var stockwerke = new List<StockwerkEingabe> { new(0, 1, "E1", 10) };
        var r = _v.Validiere("", stockwerke);
        r.Gueltig.Should().BeFalse();
        r.Fehler.Should().Contain(f => f.Contains("Name"));
    }

    [Fact]
    public void Kein_Stockwerk_Wird_Erkannt()
    {
        var r = _v.Validiere("Test", new List<StockwerkEingabe>());
        r.Gueltig.Should().BeFalse();
        r.Fehler.Should().Contain(f => f.Contains("Stockwerk"));
    }

    [Fact]
    public void Doppelte_Stockwerksnummern_Werden_Erkannt()
    {
        var stockwerke = new List<StockwerkEingabe>
        {
            new(0, 1, "E1", 10),
            new(0, 1, "E1-b", 20),
        };
        var r = _v.Validiere("Test", stockwerke);
        r.Gueltig.Should().BeFalse();
        r.Fehler.Should().Contain(f => f.Contains("mehrfach"));
    }

    [Fact]
    public void Null_Parkplaetze_Wird_Erkannt()
    {
        var stockwerke = new List<StockwerkEingabe> { new(0, 1, "E1", 0) };
        var r = _v.Validiere("Test", stockwerke);
        r.Gueltig.Should().BeFalse();
    }

    [Fact]
    public void Gueltiges_Parkhaus_Wird_Akzeptiert()
    {
        var stockwerke = new List<StockwerkEingabe>
        {
            new(0, 1, "E1", 10),
            new(0, 2, "E2", 20),
        };
        var r = _v.Validiere("Test", stockwerke);
        r.Gueltig.Should().BeTrue();
        r.Fehler.Should().BeEmpty();
    }
}
