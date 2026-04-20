namespace EasyParking.Domain.Entities;

public class Dauermieter
{
    public int Id { get; set; }
    public int ParkhausId { get; set; }
    public Parkhaus? Parkhaus { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;

    public int? FesterParkplatzId { get; set; }
    public Parkplatz? FesterParkplatz { get; set; }

    public bool Gesperrt { get; set; }

    public List<Mietzahlung> Mietzahlungen { get; set; } = new();

    public string Anzeigename => $"{Vorname} {Nachname} ({Code})";
}
