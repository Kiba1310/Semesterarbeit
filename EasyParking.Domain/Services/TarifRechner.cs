using EasyParking.Domain.Entities;

namespace EasyParking.Domain.Services;

public class TarifRechner : ITarifRechner
{
    public const decimal TagesPauschale = 35.00m;
    public static readonly TimeSpan TagesSchwelle = TimeSpan.FromHours(24);
    public static readonly TimeSpan ViertelStunde = TimeSpan.FromMinutes(15);

    public decimal BerechneBetrag(
        DateTime eingang,
        DateTime ausgang,
        IReadOnlyList<Tarif> tarife,
        IReadOnlyList<DateTime> feiertage)
    {
        if (ausgang <= eingang) return 0m;

        var dauer = ausgang - eingang;

        if (dauer > TagesSchwelle)
        {
            var totalMinuten = dauer.TotalMinutes;
            var angefangeneTage = (int)Math.Ceiling(totalMinuten / (24 * 60));
            return angefangeneTage * TagesPauschale;
        }

        var feiertagsSet = feiertage.Select(f => f.Date).ToHashSet();
        var viertelStunden = (int)Math.Ceiling(dauer.TotalMinutes / 15.0);

        decimal total = 0m;
        for (var i = 0; i < viertelStunden; i++)
        {
            var zeitpunkt = eingang.AddMinutes(i * 15);
            var istWochenendeOderFeiertag = IstWochenendeOderFeiertag(zeitpunkt, feiertagsSet);
            var tarifTyp = istWochenendeOderFeiertag ? TarifTyp.WochenendeFeiertag : TarifTyp.Wochentag;
            var stundePreis = FindePreis(zeitpunkt.Hour, tarifTyp, tarife);
            total += stundePreis / 4m;
        }

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private static bool IstWochenendeOderFeiertag(DateTime zeitpunkt, HashSet<DateTime> feiertage)
    {
        if (zeitpunkt.DayOfWeek == DayOfWeek.Saturday || zeitpunkt.DayOfWeek == DayOfWeek.Sunday)
            return true;
        return feiertage.Contains(zeitpunkt.Date);
    }

    private static decimal FindePreis(int stunde, TarifTyp typ, IReadOnlyList<Tarif> tarife)
    {
        var tarif = tarife.FirstOrDefault(t => t.Typ == typ && stunde >= t.StartStunde && stunde <= t.EndStunde);
        if (tarif is null)
            throw new InvalidOperationException($"Kein Tarif konfiguriert für Stunde {stunde}, Typ {typ}.");
        return tarif.PreisProStunde;
    }
}
