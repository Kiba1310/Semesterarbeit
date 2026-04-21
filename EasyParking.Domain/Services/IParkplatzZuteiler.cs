using EasyParking.Domain.Entities;

namespace EasyParking.Domain.Services;

public interface IParkplatzZuteiler
{
    Parkplatz? FindeFreienPlatz(IEnumerable<Stockwerk> stockwerke);
}
