using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyParking.App.Services;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public partial class ZeitAuswertungViewModel : ObservableObject
{
    private readonly StatistikService _service;

    [ObservableProperty] private DateTime von = DateTime.Now.Date.AddDays(-30);
    [ObservableProperty] private DateTime bis = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
    [ObservableProperty] private Kundenkategorie? gewaehlteKategorie;
    [ObservableProperty] private ParkhausViewModel? gewaehltesParkhaus;
    [ObservableProperty] private ObservableCollection<ZeitAuswertungZeile> ergebnis = new();
    [ObservableProperty] private ObservableCollection<ParkhausViewModel> parkhaeuser = new();

    public Array Kategorien { get; } = Enum.GetValues(typeof(Kundenkategorie));

    public ZeitAuswertungViewModel(StatistikService service)
    {
        _service = service;
    }

    public void LadeParkhaeuser(List<ParkhausViewModel> liste)
    {
        Parkhaeuser.Clear();
        foreach (var p in liste) Parkhaeuser.Add(p);
    }

    [RelayCommand]
    private void Auswerten()
    {
        Ergebnis.Clear();
        foreach (var z in _service.ZeitAuswertung(Von, Bis, GewaehlteKategorie, GewaehltesParkhaus?.Id))
            Ergebnis.Add(z);
    }
}
