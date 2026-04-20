namespace EasyParking.Domain.Entities;

public enum TarifTyp
{
    Wochentag = 0,
    WochenendeFeiertag = 1
}

public class Tarif
{
    public int Id { get; set; }
    public int ParkhausId { get; set; }
    public Parkhaus? Parkhaus { get; set; }

    public TarifTyp Typ { get; set; }
    public int StartStunde { get; set; }
    public int EndStunde { get; set; }
    public decimal PreisProStunde { get; set; }
}
