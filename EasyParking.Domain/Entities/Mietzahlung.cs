namespace EasyParking.Domain.Entities;

public class Mietzahlung
{
    public int Id { get; set; }
    public int DauermieterId { get; set; }
    public Dauermieter? Dauermieter { get; set; }

    public int Jahr { get; set; }
    public int Monat { get; set; }
    public decimal Betrag { get; set; }
    public DateTime Zahldatum { get; set; }
}
