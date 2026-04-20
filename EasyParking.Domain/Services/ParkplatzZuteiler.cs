using EasyParking.Domain.Entities;

namespace EasyParking.Domain.Services;

public class ParkplatzZuteiler : IParkplatzZuteiler
{
    public Parkplatz? FindeFreienPlatz(IEnumerable<Stockwerk> stockwerke)
    {
        Stockwerk? bestesStockwerk = null;
        double besterFreiAnteil = -1;

        foreach (var sw in stockwerke)
        {
            var nutzbareP = sw.Parkplaetze
                .Where(p => p.Typ == ParkplatzTyp.Gelegenheitsnutzer)
                .ToList();
            if (nutzbareP.Count == 0) continue;

            var freie = nutzbareP.Count(p => p.Status == ParkplatzStatus.Frei);
            if (freie == 0) continue;

            double anteil = (double)freie / nutzbareP.Count;
            if (anteil > besterFreiAnteil)
            {
                besterFreiAnteil = anteil;
                bestesStockwerk = sw;
            }
        }

        if (bestesStockwerk is null) return null;

        return bestesStockwerk.Parkplaetze
            .Where(p => p.Typ == ParkplatzTyp.Gelegenheitsnutzer && p.Status == ParkplatzStatus.Frei)
            .OrderBy(p => p.Nummer)
            .FirstOrDefault();
    }
}
