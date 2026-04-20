namespace EasyParking.Domain.Entities;

public class Stockwerk
{
    public int Id { get; set; }
    public int ParkhausId { get; set; }
    public Parkhaus? Parkhaus { get; set; }

    public int Nummer { get; set; }
    public string Bezeichnung { get; set; } = string.Empty;

    public List<Parkplatz> Parkplaetze { get; set; } = new();
}
