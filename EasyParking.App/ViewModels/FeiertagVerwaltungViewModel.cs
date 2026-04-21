using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyParking.App.Services;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public partial class FeiertagDetail : ObservableObject
{
    public int Id { get; set; }
    [ObservableProperty] private DateTime datum = DateTime.Now.Date;
    [ObservableProperty] private string bezeichnung = string.Empty;
    [ObservableProperty] private ParkhausViewModel? parkhaus;

    public string AnzeigeParkhaus => Parkhaus?.Name ?? "(alle)";
}

public partial class FeiertagVerwaltungViewModel : ObservableObject
{
    private readonly FeiertagService _service;

    [ObservableProperty] private ObservableCollection<FeiertagDetail> feiertage = new();
    [ObservableProperty] private FeiertagDetail? gewaehlterFeiertag;
    [ObservableProperty] private ObservableCollection<ParkhausViewModel?> parkhausOptionen = new();
    [ObservableProperty] private string? meldung;
    [ObservableProperty] private bool istFehler;

    public Action? DatenGeaendert { get; set; }

    public FeiertagVerwaltungViewModel(FeiertagService service)
    {
        _service = service;
    }

    public void LadeParkhaeuser(List<ParkhausViewModel> liste)
    {
        ParkhausOptionen.Clear();
        ParkhausOptionen.Add(null);
        foreach (var p in liste) ParkhausOptionen.Add(p);
        Lade();
    }

    public void Lade()
    {
        Feiertage.Clear();
        foreach (var f in _service.LadeFeiertage())
        {
            var ph = f.ParkhausId.HasValue
                ? ParkhausOptionen.FirstOrDefault(o => o != null && o.Id == f.ParkhausId.Value)
                : null;
            Feiertage.Add(new FeiertagDetail
            {
                Id = f.Id,
                Datum = f.Datum,
                Bezeichnung = f.Bezeichnung,
                Parkhaus = ph
            });
        }
    }

    [RelayCommand]
    private void Neu()
    {
        var neu = new FeiertagDetail { Id = 0, Datum = DateTime.Now.Date, Bezeichnung = "Neuer Feiertag" };
        Feiertage.Add(neu);
        GewaehlterFeiertag = neu;
    }

    [RelayCommand]
    private void Entfernen()
    {
        if (GewaehlterFeiertag == null) return;
        if (GewaehlterFeiertag.Id != 0)
            _service.Loesche(GewaehlterFeiertag.Id);
        Feiertage.Remove(GewaehlterFeiertag);
        Meldung = "Eintrag entfernt.";
        IstFehler = false;
        DatenGeaendert?.Invoke();
    }

    [RelayCommand]
    private void Speichern()
    {
        var fehler = new List<string>();
        foreach (var f in Feiertage)
        {
            var eingabe = new FeiertagEingabe(f.Id, f.Datum, f.Bezeichnung, f.Parkhaus?.Id);
            var (erfolg, text) = _service.Speichere(eingabe);
            if (!erfolg)
                fehler.Add($"{f.Datum:dd.MM.yyyy} {f.Bezeichnung}: {text}");
        }

        if (fehler.Count > 0)
        {
            Meldung = "Fehler:\n  • " + string.Join("\n  • ", fehler);
            IstFehler = true;
            Lade();
            return;
        }

        Meldung = $"{Feiertage.Count} Feiertag(e) gespeichert.";
        IstFehler = false;
        Lade();
        DatenGeaendert?.Invoke();
    }

    [RelayCommand]
    private void Neuladen() => Lade();
}
