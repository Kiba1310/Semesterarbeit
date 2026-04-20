using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyParking.App.Services;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public partial class DauermieterVerwaltungViewModel : ObservableObject
{
    private readonly ParkhausService _service;

    [ObservableProperty] private ObservableCollection<ParkhausViewModel> parkhaeuser = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AktuelleMieter))]
    private ParkhausViewModel? gewaehltesParkhaus;

    [ObservableProperty] private ObservableCollection<DauermieterZeile> aktuelleMieter = new();
    [ObservableProperty] private DauermieterZeile? gewaehlterMieter;
    [ObservableProperty] private string? meldung;

    public DauermieterVerwaltungViewModel(ParkhausService service)
    {
        _service = service;
    }

    public void LadeParkhaeuser(List<ParkhausViewModel> liste)
    {
        Parkhaeuser.Clear();
        foreach (var p in liste) Parkhaeuser.Add(p);
        GewaehltesParkhaus = Parkhaeuser.FirstOrDefault();
    }

    partial void OnGewaehltesParkhausChanged(ParkhausViewModel? value)
    {
        AktualisiereListe();
    }

    private void AktualisiereListe()
    {
        AktuelleMieter.Clear();
        if (GewaehltesParkhaus == null) return;
        foreach (var m in _service.LadeDauermieter(GewaehltesParkhaus.Id))
            AktuelleMieter.Add(new DauermieterZeile(m));
    }

    [RelayCommand]
    private void ZahlungErfassen()
    {
        if (GewaehlterMieter == null)
        {
            Meldung = "Bitte Dauermieter auswählen.";
            return;
        }
        _service.ErfasseMietzahlung(GewaehlterMieter.Id, DateTime.Now.Year, DateTime.Now.Month, 200.00m);
        Meldung = $"Zahlung für {GewaehlterMieter.Anzeigename} erfasst.";
        AktualisiereListe();
    }

    [RelayCommand]
    private void Aktualisieren() => AktualisiereListe();
}

public class DauermieterZeile
{
    public int Id { get; }
    public string Code { get; }
    public string Anzeigename { get; }
    public string PlatzInfo { get; }
    public bool Gesperrt { get; }
    public string LetzteZahlung { get; }
    public string Status => Gesperrt ? "GESPERRT" : "aktiv";

    public DauermieterZeile(Dauermieter m)
    {
        Id = m.Id;
        Code = m.Code;
        Anzeigename = m.Anzeigename;
        PlatzInfo = m.FesterParkplatz != null && m.FesterParkplatz.Stockwerk != null
            ? $"Etage {m.FesterParkplatz.Stockwerk.Nummer}, Platz {m.FesterParkplatz.Nummer}"
            : "-";
        Gesperrt = m.Gesperrt;
        var letzte = m.Mietzahlungen.OrderByDescending(z => z.Jahr).ThenByDescending(z => z.Monat).FirstOrDefault();
        LetzteZahlung = letzte != null ? $"{letzte.Jahr}-{letzte.Monat:D2} (CHF {letzte.Betrag:F2})" : "keine";
    }
}
