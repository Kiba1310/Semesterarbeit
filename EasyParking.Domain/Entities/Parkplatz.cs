namespace EasyParking.Domain.Entities;

public enum ParkplatzTyp
{
    Gelegenheitsnutzer = 0,
    Dauermieter = 1
}

public enum ParkplatzStatus
{
    Frei = 0,
    Besetzt = 1
}

public class Parkplatz
{
    public int Id { get; set; }
    public int StockwerkId { get; set; }
    public Stockwerk? Stockwerk { get; set; }

    public int Nummer { get; set; }
    public ParkplatzTyp Typ { get; set; } = ParkplatzTyp.Gelegenheitsnutzer;
    public ParkplatzStatus Status { get; set; } = ParkplatzStatus.Frei;

    public int? DauermieterId { get; set; }
    public Dauermieter? Dauermieter { get; set; }
}
