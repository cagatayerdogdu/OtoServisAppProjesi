using OtoServisApp.Services;
using OtoServisApp.Models;
using System;
namespace OtoServisApp.Views;

public partial class LoginView : ContentPage
{
    private readonly ApiService _apiService;

    public LoginView()
    {
        InitializeComponent();
        _apiService = new ApiService(); // Servisimizi başlattık
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim().ToLower(); // Senin değişken isimlerin
        string password = PasswordEntry.Text?.Trim();


        // Sadece Android ve iOS (Mobil) tarafında internet kontrolü yapıyoruz
#if ANDROID || IOS
        if (Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            await DisplayAlert("Bağlantı Hatası", "Lütfen internet bağlantınızı kontrol edin.", "Tamam");
        }
#endif

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Uyarı", "Lütfen e-posta ve şifrenizi giriniz.", "Tamam");
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Text = "GİRİŞ YAPILIYOR...";

        var kullanici = await _apiService.GirisYapAsync(email, password);

        if (kullanici != null)
        {
            // YENİ EKLENEN KOD: Giriş başarılıysa Firebase Token'ı güncelle
            await _apiService.FcmTokenGuncelle(kullanici.id);

            // --- MADDE 25: BENİ HATIRLA ---
            if (BeniHatirlaCheckBox.IsChecked)
            {
                await SecureStorage.Default.SetAsync("kayitli_eposta", email);
                await SecureStorage.Default.SetAsync("kayitli_sifre", password);
            }
            else
            {
                // Tik kaldırılmışsa kasadan sil
                SecureStorage.Default.Remove("kayitli_eposta");
                SecureStorage.Default.Remove("kayitli_sifre");
            }
            // Herkes (Admin dahil) önce ana merkeze (Dashboard) gider!
            /* ESKİ KOD: Rengi siyah bırakan varsayılan sayfa yönlendirmesi
            Application.Current.MainPage = new NavigationPage(new DashboardView(kullanici)); */
            // YENİ EKLENEN REVİZE: Yeni sayfayı oluştururken üst barı turkuaz (#00BCD4) olarak yapılandırıyoruz
            var dashNavPage = new NavigationPage(new DashboardView(kullanici));
            dashNavPage.BarBackgroundColor = Color.FromArgb("#00BCD4");
            dashNavPage.BarTextColor = Colors.White;
            Application.Current.MainPage = dashNavPage;

            /*
            if (kullanici.rol == "Admin")
            {
                Application.Current.MainPage = new NavigationPage(new AdminDashboardView(kullanici));
            }
            else
            {
                Application.Current.MainPage = new NavigationPage(new DashboardView(kullanici));
            }*/
        }
        else
        {
            await DisplayAlert("Hata", "E-posta veya şifre hatalı. Lütfen tekrar deneyin.", "Tamam");

            LoginButton.IsEnabled = true;
            LoginButton.Text = "GİRİŞ YAP";
        }
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        //await DisplayAlert("Bilgi", "Kayıt olma ekranına geçiş yapılacak.", "Tamam");
        // Kayıt sayfasına git
        await Navigation.PushAsync(new RegisterView());
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ForgotPasswordView());
    }

    private void OnGuestContinueTapped(object sender, EventArgs e)
    {
        // Sahte (Dummy) bir misafir nesnesi oluşturuyoruz
        var misafirKullanici = new Kullanici
        {
            id = 0, // 0 ID'si misafir olduğunu belirtir
            ad_soyad = "Misafir Kullanıcı",
            eposta = "misafir",
            araclar = new List<Arac>()
        };

        // Doğrudan Ana Sayfaya (Dashboard) yönlendir
        // Application.Current.MainPage = new NavigationPage(new DashboardView(misafirKullanici));
        var dashNavPage = new NavigationPage(new DashboardView(misafirKullanici));
        dashNavPage.BarBackgroundColor = Color.FromArgb("#00BCD4");
        dashNavPage.BarTextColor = Colors.White;
        Application.Current.MainPage = dashNavPage;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();


        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (100ms) bekleyip thread'i rahatlatıyoruz.
        // await Task.Delay(20);

        // Yükleme işlemini bu rahatlamadan sonra tetikliyoruz.

        // --- YENİ REVİZE BAŞLANGICI: Bildirim İzni İsteme (Madde 48) ---
#if ANDROID
    // Sadece Android 13 (API 33) ve üzeri için bu izin penceresi zorunludur
    if (DeviceInfo.Version.Major >= 13)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
    }
#endif
        // --- YENİ REVİZE BİTİŞİ ---

        // Sadece Android ve iOS (Mobil) tarafında internet kontrolü yapıyoruz
#if ANDROID || IOS
        if (Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            await DisplayAlert("Bağlantı Hatası", "Lütfen internet bağlantınızı kontrol edin.", "Tamam");
        }
#endif
    
        // Cihazın şifreli kasasına bakıyoruz, kayıt var mı?
        string kayitliEposta = await SecureStorage.Default.GetAsync("kayitli_eposta");
        string kayitliSifre = await SecureStorage.Default.GetAsync("kayitli_sifre");

        if (!string.IsNullOrEmpty(kayitliEposta) && !string.IsNullOrEmpty(kayitliSifre))
        {
            // Veri varsa kutuları doldur ve SESSİZCE giriş yap
            EmailEntry.Text = kayitliEposta;
            PasswordEntry.Text = kayitliSifre;
            BeniHatirlaCheckBox.IsChecked = true;

            OtomatikGirisYap(kayitliEposta, kayitliSifre);
        }
    }

    private async void OtomatikGirisYap(string eposta, string sifre)
    {
        // Sadece Android ve iOS (Mobil) tarafında internet kontrolü yapıyoruz
#if ANDROID || IOS
        if (Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            await DisplayAlert("Bağlantı Hatası", "Lütfen internet bağlantınızı kontrol edin.", "Tamam");
        }
#endif

        LoginButton.IsEnabled = false;
        LoginButton.Text = "OTOMATİK GİRİŞ YAPILIYOR...";

        var kullanici = await _apiService.GirisYapAsync(eposta, sifre);

        if (kullanici != null)
        {
            // Senin yöntemin: Kullanıcı nesnesini sayfanın içine parametre olarak gönderiyoruz!
            /* Admin girişi olunca direk Admin Panele gidiyordu ve geri dönemiyordu, bu yüzden bu blok kapatıldı.
             * if (kullanici.rol == "Admin")
            {
                Application.Current.MainPage = new NavigationPage(new AdminDashboardView(kullanici));
            }
            else
            {
                Application.Current.MainPage = new NavigationPage(new DashboardView(kullanici));
            }
            */

            // YENİ EKLENEN KOD: Otomatik giriş başarılıysa Firebase Token'ı güncelle
            await _apiService.FcmTokenGuncelle(kullanici.id);

            /*ESKİ KOD: Rengi siyah bırakan varsayılan sayfa yönlendirmesi
            Application.Current.MainPage = new NavigationPage(new DashboardView(kullanici)); */

            // YENİ EKLENEN REVİZE: Yeni sayfayı oluştururken üst barı turkuaz (#00BCD4) olarak yapılandırıyoruz.
            var dashNavPage = new NavigationPage(new DashboardView(kullanici));
            dashNavPage.BarBackgroundColor = Color.FromArgb("#00BCD4");
            dashNavPage.BarTextColor = Colors.White;
            Application.Current.MainPage = dashNavPage;
        }
        else
        {
            SecureStorage.Default.Remove("kayitli_eposta");
            SecureStorage.Default.Remove("kayitli_sifre");

            LoginButton.IsEnabled = true;
            LoginButton.Text = "GİRİŞ YAP";
        }
    }
}