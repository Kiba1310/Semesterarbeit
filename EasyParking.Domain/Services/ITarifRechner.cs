using EasyParking.Domain.Entities;

namespace EasyParking.Domain.Services;

public interface ITarifRechner
{
    decimal BerechneBetrag(DateTime eingang, DateTime ausgang, IReadOnlyList<Tarif> tarife, IReadOnlyList<DateTime> feiertage);
}
