using System.Collections.ObjectModel;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public class StockwerkViewModel
{
    public int Id { get; }
    public int Nummer { get; }
    public string Bezeichnung { get; }
    public ObservableCollection<ParkplatzViewModel> Parkplaetze { get; } = new();

    public int Gesamt => Parkplaetze.Count;
    public int Frei => Parkplaetze.Count(p => p.Status == ParkplatzStatus.Frei);
    public int Besetzt => Parkplaetze.Count(p => p.Status == ParkplatzStatus.Besetzt);

    public string Zusammenfassung => $"{Bezeichnung}  –  {Frei} frei / {Gesamt} total";

    public StockwerkViewModel(Stockwerk sw)
    {
        Id = sw.Id;
        Nummer = sw.Nummer;
        Bezeichnung = string.IsNullOrWhiteSpace(sw.Bezeichnung) ? $"Etage {sw.Nummer}" : sw.Bezeichnung;
        foreach (var p in sw.Parkplaetze.OrderBy(p => p.Nummer))
            Parkplaetze.Add(new ParkplatzViewModel(p));
    }
}
