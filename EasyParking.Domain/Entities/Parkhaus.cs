namespace EasyParking.Domain.Entities;

public class Parkhaus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Stadt { get; set; } = string.Empty;
    public string? Gruppe { get; set; }

    public List<Stockwerk> Stockwerke { get; set; } = new();
    public List<Tarif> Tarife { get; set; } = new();
    public List<Parkticket> Parktickets { get; set; } = new();
    public List<Dauermieter> Dauermieter { get; set; } = new();
}
