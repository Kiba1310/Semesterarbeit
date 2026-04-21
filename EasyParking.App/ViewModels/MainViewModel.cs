using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyParking.App.Services;
using EasyParking.Domain.Entities;

namespace EasyParking.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ParkhausService _parkhausService;
    private readonly StatistikService _statistikService;

    [ObservableProperty]
    private ObservableCollection<ParkhausViewModel> parkhaeuser = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AktuellesParkhausTitel))]
    private ParkhausViewModel? aktuellesParkhaus;

    [ObservableProperty]
    private ObservableCollection<OffenesTicketViewModel> offeneTickets = new();

    [ObservableProperty]
    private OffenesTicketViewModel? gewaehltesOffenesTicket;

    [ObservableProperty]
    private string dauermieterCode = string.Empty;

    [ObservableProperty]
    private TicketAnzeigeViewModel? letztesTicket;

    [ObservableProperty]
    private string? meldung;

    public StatistikViewModel Statistik { get; }
    public ZeitAuswertungViewModel ZeitAuswertung { get; }
    public DauermieterVerwaltungViewModel Dauermieter { get; }
    public TarifVerwaltungViewModel Tarife { get; }
    public ParkhausVerwaltungViewModel ParkhausVerwaltung { get; }
    public FeiertagVerwaltungViewModel FeiertagVerwaltung { get; }

    public string AktuellesParkhausTitel => AktuellesParkhaus?.Titel ?? "Kein Parkhaus ausgewählt";

    public MainViewModel(
        ParkhausService parkhausService,
        StatistikService statistikService,
        TarifVerwaltungService tarifService,
        FeiertagService feiertagService)
    {
        _parkhausService = parkhausService;
        _statistikService = statistikService;
        Statistik = new StatistikViewModel(statistikService);
        ZeitAuswertung = new ZeitAuswertungViewModel(statistikService);
        Dauermieter = new DauermieterVerwaltungViewModel(parkhausService);
        Tarife = new TarifVerwaltungViewModel(tarifService);
        ParkhausVerwaltung = new ParkhausVerwaltungViewModel(parkhausService);
        FeiertagVerwaltung = new FeiertagVerwaltungViewModel(feiertagService);

        ParkhausVerwaltung.DatenGeaendert = Lade;
        FeiertagVerwaltung.DatenGeaendert = Lade;
        Dauermieter.DatenGeaendert = Lade;

        Lade();
    }

    public void Lade()
    {
        Parkhaeuser.Clear();
        foreach (var ph in _parkhausService.LadeParkhaeuser())
            Parkhaeuser.Add(new ParkhausViewModel(ph));
        AktuellesParkhaus = Parkhaeuser.FirstOrDefault();
        Statistik.LadeParkhaeuser(Parkhaeuser.ToList());
        ZeitAuswertung.LadeParkhaeuser(Parkhaeuser.ToList());
        Dauermieter.LadeParkhaeuser(Parkhaeuser.ToList());
        Tarife.LadeParkhaeuser(Parkhaeuser.ToList());
        ParkhausVerwaltung.Lade();
        FeiertagVerwaltung.LadeParkhaeuser(Parkhaeuser.ToList());
        AktualisiereOffeneTickets();
    }

    partial void OnAktuellesParkhausChanged(ParkhausViewModel? value)
    {
        AktualisiereOffeneTickets();
    }

    private void AktualisiereOffeneTickets()
    {
        OffeneTickets.Clear();
        if (AktuellesParkhaus == null) return;
        foreach (var t in _parkhausService.OffeneTickets(AktuellesParkhaus.Id))
            OffeneTickets.Add(new OffenesTicketViewModel(t));
    }

    private void AktualisiereParkhausView()
    {
        if (AktuellesParkhaus == null) return;
        var id = AktuellesParkhaus.Id;
        var frisch = _parkhausService.LadeParkhaus(id);
        if (frisch == null) return;
        var index = Parkhaeuser.IndexOf(AktuellesParkhaus);
        var neu = new ParkhausViewModel(frisch);
        Parkhaeuser[index] = neu;
        AktuellesParkhaus = neu;
    }

    [RelayCommand]
    private void EingangGelegenheit()
    {
        if (AktuellesParkhaus == null) return;
        var ticket = _parkhausService.ErstelleGelegenheitsticket(AktuellesParkhaus.Id, DateTime.Now);
        if (ticket is null)
        {
            Meldung = "Kein freier Parkplatz verfügbar.";
            return;
        }
        Meldung = $"Ticket {ticket.TicketNummer} erstellt.";
        var vm = new TicketAnzeigeViewModel();
        vm.AusTicket(ticket, AktuellesParkhaus.Name);
        LetztesTicket = vm;
        AktualisiereParkhausView();
        AktualisiereOffeneTickets();
    }

    [RelayCommand]
    private void EingangDauermieter()
    {
        if (AktuellesParkhaus == null) return;
        if (string.IsNullOrWhiteSpace(DauermieterCode))
        {
            Meldung = "Bitte Code eingeben.";
            return;
        }
        var (ticket, fehler) = _parkhausService.ErstelleDauermieterticket(AktuellesParkhaus.Id, DauermieterCode.Trim(), DateTime.Now);
        if (ticket is null)
        {
            Meldung = fehler ?? "Fehler.";
            return;
        }
        Meldung = $"Dauermieterticket {ticket.TicketNummer} erstellt.";
        var vm = new TicketAnzeigeViewModel();
        vm.AusTicket(ticket, AktuellesParkhaus.Name);
        LetztesTicket = vm;
        DauermieterCode = string.Empty;
        AktualisiereParkhausView();
        AktualisiereOffeneTickets();
    }

    [RelayCommand]
    private void Ausgang()
    {
        if (AktuellesParkhaus == null || GewaehltesOffenesTicket == null)
        {
            Meldung = "Bitte offenes Ticket auswählen.";
            return;
        }
        var (ticket, fehler) = _parkhausService.EntwerteTicket(GewaehltesOffenesTicket.TicketNummer, DateTime.Now);
        if (ticket is null)
        {
            Meldung = fehler ?? "Fehler.";
            return;
        }
        Meldung = $"Ticket entwertet. Betrag: CHF {ticket.Betrag:F2}.";
        var vm = new TicketAnzeigeViewModel();
        vm.AusTicket(ticket, AktuellesParkhaus.Name);
        LetztesTicket = vm;
        AktualisiereParkhausView();
        AktualisiereOffeneTickets();
    }

    [RelayCommand]
    private void Aktualisieren()
    {
        Lade();
        Meldung = "Daten aktualisiert.";
    }
}
