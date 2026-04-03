using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class NotificationsView : ContentPage
{
    private readonly ApiService _apiService;

    public NotificationsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (100ms) bekleyip thread'i rahatlatıyoruz.
        // await Task.Delay(20);

        // Yükleme işlemini bu rahatlamadan sonra tetikliyoruz.
        await BildirimleriYukle();
    }

    private async Task BildirimleriYukle()
    {
        // NOT: Kendi sistemindeki Kullanıcı ID'yi buraya çek (Örn: Preferences.Get("kullanici_id", 0))
        int aktifKullaniciId = 1;

        var bildirimler = await _apiService.KullaniciBildirimleriniGetirAsync(aktifKullaniciId);
        NotificationsList.ItemsSource = bildirimler;
    }

    private async void OnNotificationTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var bildirim = border?.BindingContext as BildirimResponse;

        if (bildirim != null && !bildirim.okundu_mu)
        {
            bool basarili = await _apiService.BildirimOkunduIsaretleAsync(bildirim.id);
            if (basarili)
            {
                bildirim.okundu_mu = true;
                await BildirimleriYukle();
            }
        }
    }
}