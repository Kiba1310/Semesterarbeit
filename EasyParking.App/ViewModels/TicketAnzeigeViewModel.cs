using CommunityToolkit.Mvvm.ComponentModel;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public partial class TicketAnzeigeViewModel : ObservableObject
{
    [ObservableProperty]
    private string titel = string.Empty;

    [ObservableProperty]
    private string ticketNummer = string.Empty;

    [ObservableProperty]
    private string parkhaus = string.Empty;

    [ObservableProperty]
    private string platzInfo = string.Empty;

    [ObservableProperty]
    private string kategorie = string.Empty;

    [ObservableProperty]
    private string eingang = string.Empty;

    [ObservableProperty]
    private string? ausgang;

    [ObservableProperty]
    private string? dauer;

    [ObservableProperty]
    private string? betrag;

    [ObservableProperty]
    private bool istAusgang;

    public void AusTicket(Parkticket t, string parkhausName)
    {
        TicketNummer = t.TicketNummer;
        Parkhaus = parkhausName;
        PlatzInfo = t.Parkplatz != null && t.Parkplatz.Stockwerk != null
            ? $"Etage {t.Parkplatz.Stockwerk.Nummer}, Platz {t.Parkplatz.Nummer}"
            : "-";
        Kategorie = t.Kategorie == Kundenkategorie.Dauermieter ? "Dauermieter" : "Gelegenheitsnutzer";
        Eingang = t.EingangsZeit.ToString("dd.MM.yyyy HH:mm:ss");
        IstAusgang = t.AusgangsZeit.HasValue;
        if (IstAusgang)
        {
            Titel = "Austrittsticket";
            Ausgang = t.AusgangsZeit!.Value.ToString("dd.MM.yyyy HH:mm:ss");
            var d = t.AusgangsZeit.Value - t.EingangsZeit;
            Dauer = $"{(int)d.TotalHours}h {d.Minutes}min";
            Betrag = t.Kategorie == Kundenkategorie.Dauermieter
                ? "Inklusive (Monatsmiete)"
                : $"CHF {t.Betrag:F2}";
        }
        else
        {
            Titel = "Eintrittsticket";
            Ausgang = null;
            Dauer = null;
            Betrag = null;
        }
    }
}
