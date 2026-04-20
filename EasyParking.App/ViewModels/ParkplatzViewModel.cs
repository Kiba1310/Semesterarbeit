using CommunityToolkit.Mvvm.ComponentModel;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public partial class ParkplatzViewModel : ObservableObject
{
    public int Id { get; }
    public int Nummer { get; }

    [ObservableProperty]
    private ParkplatzTyp typ;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Farbe))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private ParkplatzStatus status;

    public string? DauermieterName { get; }

    public ParkplatzViewModel(Parkplatz p)
    {
        Id = p.Id;
        Nummer = p.Nummer;
        Typ = p.Typ;
        Status = p.Status;
        DauermieterName = p.Dauermieter?.Anzeigename;
    }

    public string Farbe => (Typ, Status) switch
    {
        (ParkplatzTyp.Dauermieter, ParkplatzStatus.Frei) => "#5B9BD5",
        (ParkplatzTyp.Dauermieter, ParkplatzStatus.Besetzt) => "#1F4E79",
        (_, ParkplatzStatus.Frei) => "#70AD47",
        (_, ParkplatzStatus.Besetzt) => "#C00000",
        _ => "#808080"
    };

    public string StatusText => (Typ, Status) switch
    {
        (ParkplatzTyp.Dauermieter, ParkplatzStatus.Frei) => $"DM #{Nummer}\n{DauermieterName}\nfrei",
        (ParkplatzTyp.Dauermieter, ParkplatzStatus.Besetzt) => $"DM #{Nummer}\n{DauermieterName}\nbesetzt",
        (_, ParkplatzStatus.Frei) => $"#{Nummer}\nfrei",
        (_, ParkplatzStatus.Besetzt) => $"#{Nummer}\nbesetzt",
        _ => $"#{Nummer}"
    };
}
