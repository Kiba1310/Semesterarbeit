namespace EasyParking.Domain.Services;

public record ParkhausValidierungsErgebnis(bool Gueltig, IReadOnlyList<string> Fehler);

public record StockwerkEingabe(int Id, int Nummer, string Bezeichnung, int AnzahlPlaetze);

public class ParkhausValidator
{
    public ParkhausValidierungsErgebnis Validiere(string name, IReadOnlyList<StockwerkEingabe> stockwerke)
    {
        var fehler = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            fehler.Add("Name darf nicht leer sein.");

        if (stockwerke.Count == 0)
            fehler.Add("Mindestens ein Stockwerk ist erforderlich.");

        foreach (var sw in stockwerke)
        {
            if (sw.AnzahlPlaetze < 1)
                fehler.Add($"Stockwerk {sw.Nummer}: mindestens 1 Parkplatz erforderlich.");
        }

        var doppelte = stockwerke.GroupBy(s => s.Nummer).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var nr in doppelte)
            fehler.Add($"Stockwerk-Nummer {nr} ist mehrfach vergeben.");

        return new ParkhausValidierungsErgebnis(fehler.Count == 0, fehler);
    }
}
