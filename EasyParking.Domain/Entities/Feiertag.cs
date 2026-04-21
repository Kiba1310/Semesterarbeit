namespace EasyParking.Domain.Entities;

public class Feiertag
{
    public int Id { get; set; }
    public DateTime Datum { get; set; }
    public string Bezeichnung { get; set; } = string.Empty;

    public int? ParkhausId { get; set; }
    public Parkhaus? Parkhaus { get; set; }
}
