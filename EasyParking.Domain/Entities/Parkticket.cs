namespace EasyParking.Domain.Entities;

public enum Kundenkategorie
{
    Gelegenheitsnutzer = 0,
    Dauermieter = 1
}

public class Parkticket
{
    public int Id { get; set; }
    public string TicketNummer { get; set; } = string.Empty;

    public int ParkhausId { get; set; }
    public Parkhaus? Parkhaus { get; set; }

    public int ParkplatzId { get; set; }
    public Parkplatz? Parkplatz { get; set; }

    public int? DauermieterId { get; set; }
    public Dauermieter? Dauermieter { get; set; }

    public Kundenkategorie Kategorie { get; set; }

    public DateTime EingangsZeit { get; set; }
    public DateTime? AusgangsZeit { get; set; }

    public decimal Betrag { get; set; }
    public bool Bezahlt { get; set; }
}
