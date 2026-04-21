using EasyParking.Domain.Entities;

namespace EasyParking.Domain.Services;

public record TarifValidierungsErgebnis(bool Gueltig, IReadOnlyList<string> Fehler);

public class TarifValidator
{
    public TarifValidierungsErgebnis Validiere(IReadOnlyList<Tarif> tarife)
    {
        var fehler = new List<string>();

        foreach (var typ in new[] { TarifTyp.Wochentag, TarifTyp.WochenendeFeiertag })
        {
            var proTyp = tarife.Where(t => t.Typ == typ).OrderBy(t => t.StartStunde).ToList();

            if (proTyp.Count == 0)
            {
                fehler.Add($"Keine Tarife für {typ} definiert.");
                continue;
            }

            foreach (var t in proTyp)
            {
                if (t.PreisProStunde <= 0)
                    fehler.Add($"{typ}: Preis CHF {t.PreisProStunde:F2} für Stunden {t.StartStunde}-{t.EndStunde} muss > 0 sein.");
                if (t.StartStunde < 0 || t.StartStunde > 23)
                    fehler.Add($"{typ}: StartStunde {t.StartStunde} liegt ausserhalb 0-23.");
                if (t.EndStunde < 0 || t.EndStunde > 23)
                    fehler.Add($"{typ}: EndStunde {t.EndStunde} liegt ausserhalb 0-23.");
                if (t.StartStunde > t.EndStunde)
                    fehler.Add($"{typ}: StartStunde {t.StartStunde} ist grösser als EndStunde {t.EndStunde}.");
            }

            for (var h = 0; h < 24; h++)
            {
                var matches = proTyp.Count(t => h >= t.StartStunde && h <= t.EndStunde);
                if (matches == 0)
                    fehler.Add($"{typ}: Stunde {h} ist nicht abgedeckt (Lücke).");
                else if (matches > 1)
                    fehler.Add($"{typ}: Stunde {h} ist mehrfach abgedeckt (Überlappung).");
            }
        }

        return new TarifValidierungsErgebnis(fehler.Count == 0, fehler);
    }
}
