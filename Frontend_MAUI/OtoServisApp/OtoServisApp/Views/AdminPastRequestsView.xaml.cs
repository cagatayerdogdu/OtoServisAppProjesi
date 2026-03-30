using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminPastRequestsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;

    public AdminPastRequestsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await VerileriYukle();
    }

    private async Task VerileriYukle()
    {
        _tumHizmetler = await _apiService.HizmetleriGetirAsync();
        var talepler = await _apiService.AdminGecmisTalepleriGetirAsync();

        if (talepler != null)
        {
            foreach (var talep in talepler)
            {
                var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
                if (hizmet != null) talep.hizmet_adi = hizmet.ad;
            }
            PastRequestsList.ItemsSource = talepler;
        }
    }
}