using System.Collections.ObjectModel;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public class ParkhausViewModel
{
    public int Id { get; }
    public string Name { get; }
    public string Stadt { get; }
    public string? Gruppe { get; }
    public ObservableCollection<StockwerkViewModel> Stockwerke { get; } = new();

    public ParkhausViewModel(Parkhaus p)
    {
        Id = p.Id;
        Name = p.Name;
        Stadt = p.Stadt;
        Gruppe = p.Gruppe;
        foreach (var sw in p.Stockwerke.OrderBy(s => s.Nummer))
            Stockwerke.Add(new StockwerkViewModel(sw));
    }

    public string Titel => $"{Name} ({Stadt})";
}
