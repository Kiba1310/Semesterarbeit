using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyParking.App.Services;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public partial class TarifVerwaltungViewModel : ObservableObject
{
    private readonly TarifVerwaltungService _service;

    [ObservableProperty] private ObservableCollection<ParkhausViewModel> parkhaeuser = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KannSpeichern))]
    private ParkhausViewModel? gewaehltesParkhaus;

    [ObservableProperty] private ObservableCollection<Tarif> tarife = new();
    [ObservableProperty] private Tarif? gewaehlterTarif;

    [ObservableProperty] private TarifTyp neuTyp = TarifTyp.Wochentag;
    [ObservableProperty] private int neuStart;
    [ObservableProperty] private int neuEnd = 23;
    [ObservableProperty] private decimal neuPreis = 2.50m;

    [ObservableProperty] private string? meldung;
    [ObservableProperty] private bool istFehler;

    public static Array TarifTypen { get; } = Enum.GetValues<TarifTyp>();
    public bool KannSpeichern => GewaehltesParkhaus != null;

    public TarifVerwaltungViewModel(TarifVerwaltungService service)
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
        LadeTarife();
    }

    private void LadeTarife()
    {
        Tarife.Clear();
        Meldung = null;
        IstFehler = false;
        if (GewaehltesParkhaus == null) return;
        foreach (var t in _service.LadeTarife(GewaehltesParkhaus.Id))
            Tarife.Add(t);
    }

    [RelayCommand]
    private void Hinzufuegen()
    {
        Tarife.Add(new Tarif
        {
            Typ = NeuTyp,
            StartStunde = NeuStart,
            EndStunde = NeuEnd,
            PreisProStunde = NeuPreis
        });
        Meldung = $"Tarifzeile hinzugefügt ({NeuTyp}, {NeuStart}-{NeuEnd} Uhr, CHF {NeuPreis:F2}).";
        IstFehler = false;
    }

    [RelayCommand]
    private void Entfernen()
    {
        if (GewaehlterTarif == null)
        {
            Meldung = "Bitte Tarifzeile auswählen.";
            IstFehler = true;
            return;
        }
        Tarife.Remove(GewaehlterTarif);
        Meldung = "Tarifzeile entfernt.";
        IstFehler = false;
    }

    [RelayCommand]
    private void Speichern()
    {
        if (GewaehltesParkhaus == null) return;

        var ergebnis = _service.SpeichereTarife(GewaehltesParkhaus.Id, Tarife.ToList());
        if (!ergebnis.Erfolg)
        {
            Meldung = "Validierungsfehler:\n  • " + string.Join("\n  • ", ergebnis.Fehler);
            IstFehler = true;
            return;
        }

        Meldung = $"Tarife für {GewaehltesParkhaus.Name} gespeichert.";
        IstFehler = false;
        LadeTarife();
    }

    [RelayCommand]
    private void Neuladen() => LadeTarife();
}
