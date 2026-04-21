using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public class OffenesTicketViewModel
{
    public string TicketNummer { get; }
    public string PlatzInfo { get; }
    public string Kategorie { get; }
    public DateTime EingangsZeit { get; }
    public string? Kunde { get; }

    public OffenesTicketViewModel(Parkticket t)
    {
        TicketNummer = t.TicketNummer;
        PlatzInfo = t.Parkplatz != null && t.Parkplatz.Stockwerk != null
            ? $"E{t.Parkplatz.Stockwerk.Nummer} / P{t.Parkplatz.Nummer}"
            : "-";
        Kategorie = t.Kategorie == Kundenkategorie.Dauermieter ? "DM" : "Gel.";
        EingangsZeit = t.EingangsZeit;
        Kunde = t.Dauermieter?.Anzeigename;
    }

    public string Anzeige => $"{TicketNummer} | {PlatzInfo} | {Kategorie} | {EingangsZeit:dd.MM HH:mm}{(Kunde != null ? " | " + Kunde : "")}";
}
