using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class ViewPhotosView : ContentPage
{
    private readonly ApiService _apiService;
    private ServisTalebi _secilenTalep;

    public ViewPhotosView(ServisTalebi talep)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _secilenTalep = talep;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await FotograflariYukle();
    }

    private async Task FotograflariYukle()
    {
        var fotolar = await _apiService.TalepFotograflariniGetirAsync(_secilenTalep.id);

        if (fotolar != null && fotolar.Any())
        {
            BindableLayout.SetItemsSource(PhotosLayout, fotolar);
        }
        else
        {
            await DisplayAlert("Bilgi", "Bu talebe ait fotoğraf bulunamadı.", "Tamam");
            await Navigation.PopAsync();
        }
    }

    // Fotoğrafın üzerine tıklandığında geçici bir tam ekran görüntüleyici açar
    private async void OnPhotoTapped(object sender, EventArgs e)
    {
        var gesture = sender as TapGestureRecognizer;
        string tamUrl = gesture?.CommandParameter as string;

        if (!string.IsNullOrEmpty(tamUrl))
        {
            // İsteğe bağlı: Telefondaki varsayılan tarayıcı/fotoğraf görüntüleyicide açtırır
            await Launcher.OpenAsync(tamUrl);
        }
    }
}