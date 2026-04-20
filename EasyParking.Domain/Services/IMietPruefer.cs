using EasyParking.Domain.Entities;

namespace EasyParking.Domain.Services;

public interface IMietPruefer
{
    bool IstFaellig(Dauermieter dauermieter, DateTime stichtag);
    void AktualisiereSperrung(Dauermieter dauermieter, DateTime stichtag);
}
