using OtoServisApp.Models;
using OtoServisApp.Services; // Eksik olan kütüphane eklendi

namespace OtoServisApp.Views;

public partial class DashboardView : ContentPage
{
    private Kullanici _aktifKullanici; // Kullanıcıyı sayfa içinde tutmak için

    // C# tarafında sayfalar arası veri taşımak için Constructor (yapıcı metod) kullanırız
    public DashboardView(Kullanici aktifKullanici)
    {
        InitializeComponent();

        _aktifKullanici = aktifKullanici;
        // Ekrana giris yapan kisinin adini yazdiriyoruz
        WelcomeLabel.Text = aktifKullanici.ad_soyad;
    }

    // ASYNC eklendi! Sayfa her ekranda göründüğünde (geri dönüldüğünde bile) bu fonksiyon otomatik çalışır
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WelcomeLabel.Text = _aktifKullanici.ad_soyad;

        await BildirimRozetiniGuncelle();
    }

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        // Tıklandığında ProfileView sayfasına git ve aktif kullanıcı verisini de yanında götür
        await Navigation.PushAsync(new ProfileView(_aktifKullanici));
    }

    // Araçlarım Kartı Tıklanınca
    private async void OnVehiclesTapped(object sender, EventArgs e)
    {
        if (_aktifKullanici.id == 0)
        {
            await DisplayAlert("Üyelik Gerekiyor", "Araç eklemek ve araçlarınızı yönetmek için lütfen ücretsiz üye olun.", "Tamam");
            // return; // İçeri girmesini engeller
        }
        await Navigation.PushAsync(new VehiclesView(_aktifKullanici));
    }

    private async void OnServiceRequestTapped(object sender, EventArgs e)
    {
        if (_aktifKullanici.id == 0)
        {
            await DisplayAlert("Üyelik Gerekiyor", "Size özel fiyatlar ve hizmetler sunabilmemiz için lütfen ücretsiz üye olun.", "Tamam");
            // return;içeri girebilsin. bunu aktif edersek kapıda uyarı alıp dışarıda kalır.
        }
        await Navigation.PushAsync(new CreateServiceRequestView(_aktifKullanici));
    }

    // "Taleplerim" veya "Servis Taleplerim" kartına tıklandığında çalışacak fonksiyon
    private async void OnMyRequestsTapped(object sender, EventArgs e)
    {
        if (_aktifKullanici.id == 0)
        {
            await DisplayAlert("Üyelik Gerekiyor", "Servis taleplerinizi takip etmek için lütfen ücretsiz üye olun.", "Tamam");
            // return;içeri girebilsin. bunu aktif edersek kapıda uyarı alıp dışarıda kalır.
        }
        await Navigation.PushAsync(new MyServiceRequestsView(_aktifKullanici));
    }

    private async void OnShowcaseTapped(object sender, EventArgs e)
    {
        // Vitrin ekranı herkese açık, misafir kısıtlaması yok!
        await Navigation.PushAsync(new ShowcaseView());
    }

    private async void OnBildirimlerTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new NotificationsView());
    }

    private async Task BildirimRozetiniGuncelle()
    {
        var _apiService = new ApiService();

        // Gerçek kullanıcı ID'si doğrudan modele bağlandı
        int aktifKullaniciId = _aktifKullanici.id;

        int okunmamisSayi = await _apiService.OkunmamisBildirimSayisiGetirAsync(aktifKullaniciId);

        if (okunmamisSayi > 0)
        {
            NotificationCountLabel.Text = okunmamisSayi > 9 ? "9+" : okunmamisSayi.ToString();
            NotificationBadgeBorder.IsVisible = true;
        }
        else
        {
            NotificationBadgeBorder.IsVisible = false;
        }
    }
}