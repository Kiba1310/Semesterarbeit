using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyParking.App.Services;

namespace EasyParking.App.ViewModels;

public partial class StatistikViewModel : ObservableObject
{
    private readonly StatistikService _service;

    [ObservableProperty] private int jahr = DateTime.Now.Year;
    [ObservableProperty] private int monat = DateTime.Now.Month;
    [ObservableProperty] private ParkhausViewModel? gewaehltesParkhaus;
    [ObservableProperty] private string? gewaehlteGruppe;
    [ObservableProperty] private ObservableCollection<UmsatzZeile> monatsUmsatz = new();
    [ObservableProperty] private ObservableCollection<UmsatzZeile> jahresUmsatz = new();
    [ObservableProperty] private ObservableCollection<ParkhausViewModel> parkhaeuser = new();
    [ObservableProperty] private ObservableCollection<string> gruppen = new();

    public StatistikViewModel(StatistikService service)
    {
        _service = service;
    }

    public void LadeParkhaeuser(List<ParkhausViewModel> liste)
    {
        Parkhaeuser.Clear();
        foreach (var p in liste) Parkhaeuser.Add(p);
        Gruppen.Clear();
        foreach (var g in _service.AlleGruppen()) Gruppen.Add(g);
    }

    [RelayCommand]
    private void Berechnen()
    {
        MonatsUmsatz.Clear();
        foreach (var z in _service.MonatsUmsatz(Jahr, Monat, GewaehltesParkhaus?.Id, GewaehlteGruppe))
            MonatsUmsatz.Add(z);

        JahresUmsatz.Clear();
        foreach (var z in _service.JahresUmsatzProMonat(Jahr, GewaehltesParkhaus?.Id, GewaehlteGruppe))
            JahresUmsatz.Add(z);
    }
}
