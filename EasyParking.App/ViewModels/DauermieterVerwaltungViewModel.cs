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
    [ObservableProperty] private bool istFehler;

    [ObservableProperty] private bool bearbeitenAktiv;
    [ObservableProperty] private int bearbeiteterId;
    [ObservableProperty] private string editCode = string.Empty;
    [ObservableProperty] private string editVorname = string.Empty;
    [ObservableProperty] private string editNachname = string.Empty;
    [ObservableProperty] private ParkhausViewModel? editParkhaus;
    [ObservableProperty] private ParkplatzAuswahl? editParkplatz;
    [ObservableProperty] private ObservableCollection<ParkplatzAuswahl> verfuegbarePlaetze = new();

    public Action? DatenGeaendert { get; set; }

    public DauermieterVerwaltungViewModel(ParkhausService service)
    {
        _service = service;
    }

    public void LadeParkhaeuser(List<ParkhausViewModel> liste)
    {
        var aktuellId = GewaehltesParkhaus?.Id;
        Parkhaeuser.Clear();
        foreach (var p in liste) Parkhaeuser.Add(p);
        GewaehltesParkhaus = aktuellId.HasValue
            ? Parkhaeuser.FirstOrDefault(p => p.Id == aktuellId.Value) ?? Parkhaeuser.FirstOrDefault()
            : Parkhaeuser.FirstOrDefault();
    }

    partial void OnGewaehltesParkhausChanged(ParkhausViewModel? value)
    {
        AktualisiereListe();
    }

    partial void OnEditParkhausChanged(ParkhausViewModel? value)
    {
        AktualisiereFreiePlaetze();
    }

    private void AktualisiereFreiePlaetze()
    {
        VerfuegbarePlaetze.Clear();
        if (EditParkhaus == null) return;
        var id = BearbeitenAktiv && BearbeiteterId != 0 ? (int?)BearbeiteterId : null;
        foreach (var p in _service.FreieParkplaetzeFuerDauermieter(EditParkhaus.Id, id))
            VerfuegbarePlaetze.Add(new ParkplatzAuswahl(p));
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
            IstFehler = true;
            return;
        }
        _service.ErfasseMietzahlung(GewaehlterMieter.Id, DateTime.Now.Year, DateTime.Now.Month, 200.00m);
        Meldung = $"Zahlung für {GewaehlterMieter.Anzeigename} erfasst.";
        IstFehler = false;
        AktualisiereListe();
        DatenGeaendert?.Invoke();
    }

    [RelayCommand]
    private void Aktualisieren() => AktualisiereListe();

    [RelayCommand]
    private void Neu()
    {
        BearbeitenAktiv = true;
        BearbeiteterId = 0;
        EditCode = "";
        EditVorname = "";
        EditNachname = "";
        EditParkhaus = GewaehltesParkhaus;
        AktualisiereFreiePlaetze();
        Meldung = null;
        IstFehler = false;
    }

    [RelayCommand]
    private void Bearbeiten()
    {
        if (GewaehlterMieter == null)
        {
            Meldung = "Bitte Dauermieter auswählen.";
            IstFehler = true;
            return;
        }
        BearbeitenAktiv = true;
        BearbeiteterId = GewaehlterMieter.Id;
        EditCode = GewaehlterMieter.Code;
        EditVorname = GewaehlterMieter.Vorname;
        EditNachname = GewaehlterMieter.Nachname;
        EditParkhaus = GewaehltesParkhaus;
        AktualisiereFreiePlaetze();
        EditParkplatz = VerfuegbarePlaetze.FirstOrDefault(p => p.Id == GewaehlterMieter.FesterParkplatzId);
        Meldung = null;
        IstFehler = false;
    }

    [RelayCommand]
    private void Abbrechen()
    {
        BearbeitenAktiv = false;
        Meldung = null;
        IstFehler = false;
    }

    [RelayCommand]
    private void EditSpeichern()
    {
        if (EditParkhaus == null || EditParkplatz == null)
        {
            Meldung = "Parkhaus und Parkplatz müssen gewählt sein.";
            IstFehler = true;
            return;
        }

        if (BearbeiteterId == 0)
        {
            var (id, fehler) = _service.NeuerDauermieter(EditParkhaus.Id, EditCode, EditVorname, EditNachname, EditParkplatz.Id);
            if (id is null)
            {
                Meldung = fehler ?? "Fehler.";
                IstFehler = true;
                return;
            }
            Meldung = "Dauermieter angelegt.";
        }
        else
        {
            var (erfolg, fehler) = _service.BearbeiteDauermieter(BearbeiteterId, EditCode, EditVorname, EditNachname, EditParkplatz.Id);
            if (!erfolg)
            {
                Meldung = fehler ?? "Fehler.";
                IstFehler = true;
                return;
            }
            Meldung = "Dauermieter aktualisiert.";
        }

        IstFehler = false;
        BearbeitenAktiv = false;
        AktualisiereListe();
        DatenGeaendert?.Invoke();
    }

    [RelayCommand]
    private void Loeschen()
    {
        if (GewaehlterMieter == null)
        {
            Meldung = "Bitte Dauermieter auswählen.";
            IstFehler = true;
            return;
        }
        var (erfolg, fehler) = _service.LoescheDauermieter(GewaehlterMieter.Id);
        if (!erfolg)
        {
            Meldung = fehler ?? "Fehler.";
            IstFehler = true;
            return;
        }
        Meldung = "Dauermieter gelöscht.";
        IstFehler = false;
        AktualisiereListe();
        DatenGeaendert?.Invoke();
    }
}

public class DauermieterZeile
{
    public int Id { get; }
    public string Code { get; }
    public string Vorname { get; }
    public string Nachname { get; }
    public int? FesterParkplatzId { get; }
    public string Anzeigename { get; }
    public string PlatzInfo { get; }
    public bool Gesperrt { get; }
    public string LetzteZahlung { get; }
    public string Status => Gesperrt ? "GESPERRT" : "aktiv";

    public DauermieterZeile(Dauermieter m)
    {
        Id = m.Id;
        Code = m.Code;
        Vorname = m.Vorname;
        Nachname = m.Nachname;
        FesterParkplatzId = m.FesterParkplatzId;
        Anzeigename = m.Anzeigename;
        PlatzInfo = m.FesterParkplatz != null && m.FesterParkplatz.Stockwerk != null
            ? $"Etage {m.FesterParkplatz.Stockwerk.Nummer}, Platz {m.FesterParkplatz.Nummer}"
            : "-";
        Gesperrt = m.Gesperrt;
        var letzte = m.Mietzahlungen.OrderByDescending(z => z.Jahr).ThenByDescending(z => z.Monat).FirstOrDefault();
        LetzteZahlung = letzte != null ? $"{letzte.Jahr}-{letzte.Monat:D2} (CHF {letzte.Betrag:F2})" : "keine";
    }
}

public class ParkplatzAuswahl
{
    public int Id { get; }
    public string Anzeige { get; }

    public ParkplatzAuswahl(Parkplatz p)
    {
        Id = p.Id;
        Anzeige = p.Stockwerk != null
            ? $"Etage {p.Stockwerk.Nummer} / Platz {p.Nummer}"
            : $"Platz {p.Nummer}";
    }
}
