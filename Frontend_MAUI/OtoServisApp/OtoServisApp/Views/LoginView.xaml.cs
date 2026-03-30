using OtoServisApp.Services;
using OtoServisApp.Models;
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
            Application.Current.MainPage = new NavigationPage(new DashboardView(kullanici));

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
        Application.Current.MainPage = new NavigationPage(new DashboardView(misafirKullanici));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

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

            Application.Current.MainPage = new NavigationPage(new DashboardView(kullanici));
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