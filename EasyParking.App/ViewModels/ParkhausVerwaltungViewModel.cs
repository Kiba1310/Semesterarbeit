using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyParking.App.Services;
using EasyParking.Domain.Entities;
using EasyParking.Domain.Services;

namespace EasyParking.App.ViewModels;

public partial class StockwerkDetail : ObservableObject
{
    public int Id { get; set; }
    [ObservableProperty] private int nummer;
    [ObservableProperty] private string bezeichnung = string.Empty;
    [ObservableProperty] private int anzahlPlaetze = 10;
}

public partial class ParkhausDetail : ObservableObject
{
    public int Id { get; set; }
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string stadt = string.Empty;
    [ObservableProperty] private string? gruppe;
    public ObservableCollection<StockwerkDetail> Stockwerke { get; } = new();
    public string Anzeige => Id == 0 ? $"<neu> {Name}" : $"{Name} ({Stadt})";
}

public partial class ParkhausVerwaltungViewModel : ObservableObject
{
    private readonly ParkhausService _service;

    [ObservableProperty] private ObservableCollection<ParkhausDetail> parkhaeuser = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KannBearbeiten))]
    private ParkhausDetail? gewaehltesParkhaus;

    [ObservableProperty] private StockwerkDetail? gewaehltesStockwerk;
    [ObservableProperty] private string? meldung;
    [ObservableProperty] private bool istFehler;

    public bool KannBearbeiten => GewaehltesParkhaus != null;
    public Action? DatenGeaendert { get; set; }

    public ParkhausVerwaltungViewModel(ParkhausService service)
    {
        _service = service;
    }

    public void Lade()
    {
        var aktuelleId = GewaehltesParkhaus?.Id;
        Parkhaeuser.Clear();
        foreach (var ph in _service.LadeParkhaeuser())
        {
            var detail = new ParkhausDetail
            {
                Id = ph.Id,
                Name = ph.Name,
                Stadt = ph.Stadt,
                Gruppe = ph.Gruppe
            };
            foreach (var sw in ph.Stockwerke.OrderBy(s => s.Nummer))
            {
                detail.Stockwerke.Add(new StockwerkDetail
                {
                    Id = sw.Id,
                    Nummer = sw.Nummer,
                    Bezeichnung = sw.Bezeichnung,
                    AnzahlPlaetze = sw.Parkplaetze.Count
                });
            }
            Parkhaeuser.Add(detail);
        }
        GewaehltesParkhaus = aktuelleId.HasValue
            ? Parkhaeuser.FirstOrDefault(p => p.Id == aktuelleId.Value) ?? Parkhaeuser.FirstOrDefault()
            : Parkhaeuser.FirstOrDefault();
    }

    [RelayCommand]
    private void Neu()
    {
        var neu = new ParkhausDetail { Id = 0, Name = "Neues Parkhaus", Stadt = "" };
        neu.Stockwerke.Add(new StockwerkDetail { Nummer = 1, Bezeichnung = "Etage 1", AnzahlPlaetze = 10 });
        Parkhaeuser.Add(neu);
        GewaehltesParkhaus = neu;
        Meldung = "Neues Parkhaus – Felder ausfüllen und Speichern drücken.";
        IstFehler = false;
    }

    [RelayCommand]
    private void StockwerkHinzufuegen()
    {
        if (GewaehltesParkhaus == null) return;
        var maxNr = GewaehltesParkhaus.Stockwerke.Any() ? GewaehltesParkhaus.Stockwerke.Max(s => s.Nummer) : 0;
        var nr = maxNr + 1;
        GewaehltesParkhaus.Stockwerke.Add(new StockwerkDetail
        {
            Nummer = nr,
            Bezeichnung = $"Etage {nr}",
            AnzahlPlaetze = 10
        });
    }

    [RelayCommand]
    private void StockwerkEntfernen()
    {
        if (GewaehltesParkhaus == null || GewaehltesStockwerk == null) return;
        GewaehltesParkhaus.Stockwerke.Remove(GewaehltesStockwerk);
    }

    [RelayCommand]
    private void Speichern()
    {
        if (GewaehltesParkhaus == null) return;
        var eingaben = GewaehltesParkhaus.Stockwerke
            .Select(s => new StockwerkEingabe(s.Id, s.Nummer, s.Bezeichnung, s.AnzahlPlaetze))
            .ToList();

        var (id, fehler) = _service.SpeichereParkhaus(
            GewaehltesParkhaus.Id,
            GewaehltesParkhaus.Name,
            GewaehltesParkhaus.Stadt,
            GewaehltesParkhaus.Gruppe,
            eingaben);

        if (id is null)
        {
            Meldung = "Fehler:\n  • " + string.Join("\n  • ", fehler);
            IstFehler = true;
            return;
        }

        GewaehltesParkhaus.Id = id.Value;
        Meldung = $"Parkhaus '{GewaehltesParkhaus.Name}' gespeichert.";
        IstFehler = false;
        Lade();
        DatenGeaendert?.Invoke();
    }

    [RelayCommand]
    private void Loeschen()
    {
        if (GewaehltesParkhaus == null) return;
        if (GewaehltesParkhaus.Id == 0)
        {
            Parkhaeuser.Remove(GewaehltesParkhaus);
            GewaehltesParkhaus = Parkhaeuser.FirstOrDefault();
            Meldung = "Entwurf verworfen.";
            IstFehler = false;
            return;
        }

        var (erfolg, fehler) = _service.LoescheParkhaus(GewaehltesParkhaus.Id);
        if (!erfolg)
        {
            Meldung = fehler ?? "Fehler.";
            IstFehler = true;
            return;
        }
        Meldung = "Parkhaus gelöscht.";
        IstFehler = false;
        Lade();
        DatenGeaendert?.Invoke();
    }
}
