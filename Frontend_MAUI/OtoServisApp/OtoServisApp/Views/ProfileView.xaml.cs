using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class ProfileView : ContentPage
{
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;

    public ProfileView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();

        // Sayfa açıldığında giriş yapan kullanıcının bilgilerini kutulara doldur
        NameEntry.Text = _aktifKullanici.ad_soyad;
        EmailEntry.Text = _aktifKullanici.eposta;
        PhoneEntry.Text = _aktifKullanici.telefon;
        AddressEditor.Text = _aktifKullanici.adres;
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        string yeniAd = NameEntry.Text?.Trim();
        string yeniTelefon = PhoneEntry.Text?.Trim();
        string yeniAdres = AddressEditor.Text?.Trim();

        if (string.IsNullOrEmpty(yeniAd) || string.IsNullOrEmpty(yeniTelefon))
        {
            await DisplayAlert("Uyarı", "Ad Soyad ve Telefon alanları zorunludur.", "Tamam");
            return;
        }

        UpdateButton.IsEnabled = false;
        UpdateButton.Text = "GÜNCELLENİYOR...";

        var guncelVeri = new KullaniciUpdate
        {
            ad_soyad = yeniAd,
            telefon = yeniTelefon,
            adres = yeniAdres
        };

        // API'ye güncelleme isteği atıyoruz
        var sonuc = await _apiService.KullaniciGuncelleAsync(_aktifKullanici.id, guncelVeri);
        /*
        if (sonuc != null)
        {
            _aktifKullanici = sonuc; // Güncel veriyi lokale de kaydet
            await DisplayAlert("Başarılı", "Bilgileriniz başarıyla güncellendi.", "Tamam");
        }
        else
        {
            await DisplayAlert("Hata", "Güncelleme başarısız oldu. Lütfen tekrar deneyin.", "Tamam");
        }
        */
        if (sonuc != null)
        {
            // Ana sayfadaki karşılama yazısının da değişmesi için elimizdeki referansı güncelliyoruz
            _aktifKullanici.ad_soyad = sonuc.ad_soyad;
            _aktifKullanici.telefon = sonuc.telefon;
            _aktifKullanici.adres = sonuc.adres;

            await DisplayAlert("Başarılı", "Bilgileriniz başarıyla güncellendi.", "Tamam");

            // İşlem bitince Ana Sayfaya (bir önceki sayfaya) yumuşak bir geçişle geri dön
            await Navigation.PopAsync();
        }


        UpdateButton.IsEnabled = true;
        UpdateButton.Text = "BİLGİLERİMİ GÜNCELLE";
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        // Az önce oluşturduğumuz sayfaya yönlendiriyoruz
        await Navigation.PushAsync(new ChangePasswordView(_aktifKullanici));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Sadece giriş yapan kullanıcının rolü "Admin" ise butonu göster
        if (_aktifKullanici != null && _aktifKullanici.rol == "Admin")
        {
            AdminPanelButton.IsVisible = true;
        }
        else
        {
            AdminPanelButton.IsVisible = false;
        }
    }

    // Butona tıklanınca Admin sayfasına gidecek (Sayfayı birazdan yapacağız)
    private async void OnAdminPanelClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AdminDashboardView(_aktifKullanici)); 
        //await DisplayAlert("Bilgi", "Yönetim Paneli çok yakında burada olacak!", "Tamam");
    }

    private async void OnHesapSilClicked(object sender, EventArgs e)
    {
        // Kullanıcıya son bir kez emin misin diye soruyoruz
        bool eminMi = await DisplayAlert("Dikkat!", "Hesabınızı silmek istediğinize emin misiniz? Bu işlem geri alınamaz.", "Evet, Sil", "Vazgeç");

        if (eminMi)
        {
            // _aktifKullanici senin kodunda nasıl tanımlıysa onu kullan
            bool basarili = await _apiService.KullaniciSilAsync(_aktifKullanici.id);

            if (basarili)
            {
                await DisplayAlert("Başarılı", "Hesabınız silindi. Sizi özleyeceğiz...", "Tamam");

                // Kullanıcıyı sildiğimiz için uygulamadan atıp Login ekranına yönlendiriyoruz
                Application.Current.MainPage = new NavigationPage(new LoginView());
            }
            else
            {
                await DisplayAlert("Hata", "Silme işlemi sırasında bir sorun oluştu.", "Tamam");
            }
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        /*
            Preferences: Cihazda verileri şifresiz, düz metin (plain text) olarak saklar. Tema rengi, "uygulamayı ilk kez açtı" bilgisi gibi önemsiz şeyler için kullanılır. Oraya şifre kaydetmek, evinin anahtarını paspasın üstüne bırakmak demektir.

            SecureStorage: Bizim yazdığımız yeni kod ise veriyi Android'in donanımsal Kasasına (Keystore) ve iOS'un Anahtarlığına (Keychain) askeri standartlarda (AES-256) şifreleyerek koyar.
         */

        // Varsa kaydedilmiş oturum bilgilerini (Preferences) temizle
        //Preferences.Remove("user_email");
        //Preferences.Remove("user_password");

        // 1. DÖNER KAPIYI KIRAN KOD: Beni Hatırla hafızasını siliyoruz!
        SecureStorage.Default.Remove("kayitli_eposta");
        SecureStorage.Default.Remove("kayitli_sifre");

        // (Varsa global kullanıcı değişkenini de temizlemek iyi bir pratiktir)
        // App.AktifKullanici = null;

        // Kullanıcıyı en baştaki Login ekranına geri fırlat ve geri dönmesini engelle
        Application.Current.MainPage = new NavigationPage(new LoginView());
    }
}