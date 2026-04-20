using EasyParking.Domain.Entities;

namespace EasyParking.Domain.Services;

public class MietPruefer : IMietPruefer
{
    public const decimal MonatsMiete = 200.00m;
    public const int FaelligkeitsTag = 15;

    public bool IstFaellig(Dauermieter dauermieter, DateTime stichtag)
    {
        if (stichtag.Day < FaelligkeitsTag) return false;
        var bezahlt = dauermieter.Mietzahlungen
            .Any(z => z.Jahr == stichtag.Year && z.Monat == stichtag.Month);
        return !bezahlt;
    }

    public void AktualisiereSperrung(Dauermieter dauermieter, DateTime stichtag)
    {
        dauermieter.Gesperrt = IstFaellig(dauermieter, stichtag);
    }
}
