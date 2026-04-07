using OtoServisApp.Models;
using OtoServisApp.Services; // Eksik olan kütüphane eklendi

namespace OtoServisApp.Views;

public partial class DashboardView : ContentPage
{
    private Kullanici _aktifKullanici; // Kullanıcıyı sayfa içinde tutmak için

    private readonly ApiService _apiService;

    // C# tarafında sayfalar arası veri taşımak için Constructor (yapıcı metod) kullanırız
    public DashboardView(Kullanici aktifKullanici)
    {
        InitializeComponent();
        //BindingContext = this;

        _aktifKullanici = aktifKullanici;
        // Ekrana giris yapan kisinin adini yazdiriyoruz
        WelcomeLabel.Text = aktifKullanici.ad_soyad;

        // Sayfa yüklendiğinde servisimizi yapılandırıyoruz
        _apiService = new ApiService();
    }

    // ASYNC eklendi! Sayfa her ekranda göründüğünde (geri dönüldüğünde bile) bu fonksiyon otomatik çalışır
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WelcomeLabel.Text = _aktifKullanici.ad_soyad;
        await BildirimRozetiniGuncelle();
        // Sayfa her açıldığında verileri tazelemek için
        await IstatistikleriGetir();

    }
    // Hesabım / Profil Kartı Tıklanınca:
    private async void OnProfileTapped(object sender, EventArgs e)
    {
        // Tıklandığında ProfileView sayfasına git ve aktif kullanıcı verisini de yanında götür
        // await Navigation.PushAsync(new ProfileView(_aktifKullanici));

        if (Application.Current.MainPage is TabbedPage tabbedPage)
        {
            // 3. index (Yani 4. Sekme: Profil)
            tabbedPage.CurrentPage = tabbedPage.Children[3];
        }
    }

    // Araçlarım Kartı Tıklanınca
    private async void OnVehiclesTapped(object sender, EventArgs e)
    {
        if (_aktifKullanici.id == 0)
        {
            await DisplayAlert("Üyelik Gerekiyor", "Araç eklemek ve araçlarınızı yönetmek için lütfen ücretsiz üye olun.", "Tamam");
            // return; // İçeri girmesini engeller
        }
        // await Navigation.PushAsync(new VehiclesView(_aktifKullanici));
        if (Application.Current.MainPage is TabbedPage tabbedPage)
        {
            // 2. index (Yani 3. Sekme: Araçlarım)
            tabbedPage.CurrentPage = tabbedPage.Children[2];
        }
    }

    // Servis Talebi Kartı Tıklanınca:
    private async void OnServiceRequestTapped(object sender, EventArgs e)
    {
        if (_aktifKullanici.id == 0)
        {
            await DisplayAlert("Üyelik Gerekiyor", "Size özel fiyatlar ve hizmetler sunabilmemiz için lütfen ücretsiz üye olun.", "Tamam");
            // return;içeri girebilsin. bunu aktif edersek kapıda uyarı alıp dışarıda kalır.
        }
        await Navigation.PushAsync(new CreateServiceRequestView(_aktifKullanici));

    }

    // "Durum Takibi / Taleplerim" kartına tıklandığında çalışacak fonksiyon
    private async void OnMyRequestsTapped(object sender, EventArgs e)
    {
        if (_aktifKullanici.id == 0)
        {
            await DisplayAlert("Üyelik Gerekiyor", "Servis taleplerinizi takip etmek için lütfen ücretsiz üye olun.", "Tamam");
            // return;içeri girebilsin. bunu aktif edersek kapıda uyarı alıp dışarıda kalır.
        }
        //await Navigation.PushAsync(new MyServiceRequestsView(_aktifKullanici));

        if (Application.Current.MainPage is TabbedPage tabbedPage)
        {
            // 1. index (Yani 2. Sekme: Taleplerim)
            tabbedPage.CurrentPage = tabbedPage.Children[1];
        }
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
        // var _apiService = new ApiService(); en üstte tanımlandı.

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

    private async void OnWhatsappTapped(object sender, EventArgs e)
    {
        string telNo = "905365854024"; // BURAYA KENDİ NUMARANI YAZ ABİ
        string mesaj = "Selamlar, Oto Servis Bakım üzerinden ulaşıyorum.";
        await Launcher.Default.OpenAsync($"whatsapp://send?phone={telNo}&text={mesaj}");
    }

    private async void OnPhoneTapped(object sender, EventArgs e)
    {
        string telNo = "05365854024"; // BURAYA TELEFONUNU YAZ
        if (PhoneDialer.Default.IsSupported)
            PhoneDialer.Default.Open(telNo);
    }

    private async void OnInstagramTapped(object sender, EventArgs e)
    {
        string instaAdres = "https://www.instagram.com/erdogducagatay"; // INSTAGRAM LİNKİN
        await Launcher.Default.OpenAsync(instaAdres);
    }

    private async Task IstatistikleriGetir()
    {
        try
        {
            // NOT: Endpoint adını main.py'deki isme göre kontrol et ("admin/dashboard-istatistik" vs "dashboard_istatistik")
            var response = await _apiService.GetAsync("admin/dashboard-istatistik");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<DashboardIstatistikResponse>(content);

                if (data != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LblToplamMusteri.Text = data.toplam_musteri.ToString();
                        LblToplamTalep.Text = data.toplam_talep.ToString();
                        LblToplamArac.Text = data.toplam_arac.ToString();
                    });
                }
            }
            else
            {
                // API'den dönen hatayı ekrana basıyoruz
                var error = await response.Content.ReadAsStringAsync();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DisplayAlert("API Hatası", $"Durum: {response.StatusCode}\nMesaj: {error}", "Tamam");
                });
            }
        }
        catch (Exception ex)
        {
            // Bağlantı kopması veya kod hatasını ekrana basıyoruz
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                DisplayAlert("Bağlantı Hatası", $"Detay: {ex.Message}", "Tamam");
            });
        }
    }

    // Gelen JSON verisini karşılayacak sınıfımız
    public class DashboardIstatistikResponse
    {
        public int toplam_musteri { get; set; }
        public int toplam_talep { get; set; }
        public int toplam_arac { get; set; }
    }
}