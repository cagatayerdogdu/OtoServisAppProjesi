using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class ProfileView : ContentPage
{
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;
    private bool _isActivationMode = false;

    public ProfileView(Kullanici kullanici, bool isActivationMode = false)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _isActivationMode = isActivationMode;
        _apiService = new ApiService();

        // Sayfa açıldığında giriş yapan kullanıcının bilgilerini kutulara doldur
        NameEntry.Text = _aktifKullanici.ad_soyad;
        EmailEntry.Text = _aktifKullanici.eposta;
        PhoneEntry.Text = _aktifKullanici.telefon;
        AddressEditor.Text = _aktifKullanici.adres;

        // AKTİVASYON MODU KONTROLÜ
        if (_isActivationMode)
        {
            Title = "Hesabı Aktifleştir";
            ActivationAlertLabel.IsVisible = true;
            YeniSifreEntry.IsVisible = true;

            // Form alanlarını değiştirilemez yapıyoruz
            NameEntry.IsEnabled = false;
            PhoneEntry.IsEnabled = false;
            AddressEditor.IsEnabled = false;

            // Buton metnini değiştir
            UpdateButton.Text = "KULLANICIMI AKTİF ET";
        }
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        // --- AKTİVASYON MODU İŞLEMLERİ ---
        if (_isActivationMode)
        {
            string yeniSifre = YeniSifreEntry.Text?.Trim();
            if (string.IsNullOrEmpty(yeniSifre) || yeniSifre.Length < 6)
            {
                await ModernAlertService.ShowInfoAsync("Lütfen yeni bir şifre giriniz. Şifreniz en az 6 haneli olmalıdır.", "Uyarı");
                return;
            }

            UpdateButton.IsEnabled = false;
            UpdateButton.Text = "AKTİF EDİLİYOR...";

            try
            {
                var body = new { yeni_sifre = yeniSifre };
                var res = await _apiService.PutAsync($"kullanicilar/aktif-et/{_aktifKullanici.id}", body);

                if (res.IsSuccessStatusCode)
                {
                    await ModernAlertService.ShowInfoAsync("Hesabınız başarıyla aktif edildi. Şimdi giriş yapabilirsiniz.", "Başarılı");
                    Application.Current.MainPage = new NavigationPage(new LoginView());
                }
                else
                {
                    await ModernAlertService.ShowInfoAsync("Aktivasyon sırasında bir sorun oluştu.", "Hata");
                    UpdateButton.IsEnabled = true;
                    UpdateButton.Text = "KULLANICIMI AKTİF ET";
                }
            }
            catch (Exception ex)
            {
                await ModernAlertService.ShowInfoAsync("Bağlantı hatası: " + ex.Message, "Hata");
                UpdateButton.IsEnabled = true;
                UpdateButton.Text = "KULLANICIMI AKTİF ET";
            }
            return; // Aktivasyon modundaysa normal güncelleme kodlarına geçme
        }

        // --- NORMAL PROFİL GÜNCELLEME İŞLEMLERİ ---
        string yeniAd = NameEntry.Text?.Trim();
        string yeniTelefon = PhoneEntry.Text?.Trim();
        string yeniAdres = AddressEditor.Text?.Trim();

        if (string.IsNullOrEmpty(yeniAd) || string.IsNullOrEmpty(yeniTelefon))
        {
            await ModernAlertService.ShowInfoAsync("Ad Soyad ve Telefon alanları zorunludur.", "Uyarı");
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

            await ModernAlertService.ShowInfoAsync("Bilgileriniz başarıyla güncellendi.", "Başarılı");

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
        if (!_isActivationMode && _aktifKullanici != null && _aktifKullanici.rol == "Admin")
        {
            AdminPanelButton.IsVisible = true;
        }
        else
        {
            AdminPanelButton.IsVisible = false;
        }

        // ==============================================================
        // --- YENİ REVİZE: Şalterin DB ile Eşitlenmesi Buraya Taşındı ---
        // ==============================================================
        if (_aktifKullanici != null)
        {
            // API'ye gereksiz istek gitmesin diye kilidi kapatıyoruz
            _isMailSwitchProgrammaticChange = true;
            // Veritabanındaki güncel durumu (1 ise True, 0 ise False) UI'a yansıtıyoruz
            MailIzniSwitch.IsToggled = _aktifKullanici.mail_istiyor_mu;
            // Kullanıcı kendi eliyle değiştirirse API'ye gitsin diye kilidi geri açıyoruz
            _isMailSwitchProgrammaticChange = false;
        }
        // ==============================================================
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
        bool? eminMiSonuc = await ModernAlertService.ShowDeleteConfirmationAsync("Hesabınızı silmek istediğinize emin misiniz? Bu işlem geri alınamaz.", "Dikkat!");
        bool eminMi = eminMiSonuc == true;

        if (eminMi)
        {
            // _aktifKullanici senin kodunda nasıl tanımlıysa onu kullan
            bool basarili = await _apiService.KullaniciSilAsync(_aktifKullanici.id);

            if (basarili)
            {
                await ModernAlertService.ShowInfoAsync("Hesabınız silindi. Sizi özleyeceğiz...", "Başarılı");

                // Kullanıcıyı sildiğimiz için uygulamadan atıp Login ekranına yönlendiriyoruz
                Application.Current.MainPage = new NavigationPage(new LoginView());
            }
            else
            {
                await ModernAlertService.ShowInfoAsync("Silme işlemi sırasında bir sorun oluştu.", "Hata");
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
        //SecureStorage.Default.Remove("kayitli_eposta");
        //SecureStorage.Default.Remove("kayitli_sifre");

        SecureStorageHelper.RemoveSavedEmail();
        SecureStorageHelper.RemoveSavedPassword();

        // (Varsa global kullanıcı değişkenini de temizlemek iyi bir pratiktir)
        // App.AktifKullanici = null;

        // Kullanıcıyı en baştaki Login ekranına geri fırlat ve geri dönmesini engelle
        // Çıkış yapıldığında LoginView'a geçerken üst barın turkuaz kalmasını sağlar
        Application.Current.MainPage = new NavigationPage(new LoginView())
        {
            BarBackgroundColor = Color.FromArgb("#00BCD4"),
            BarTextColor = Colors.White
        };
    }

    // --- YENİ REVİZE BAŞLANGICI: Tema Değiştirme İşlemi (Madde 65) --- TEMA İSTEMİYORUM.
    /*private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            // Switch açıksa Karanlık Modu yapılandır
            Application.Current.UserAppTheme = AppTheme.Dark;
        }
        else
        {
            // Switch kapalıysa Aydınlık Modu yapılandır
            Application.Current.UserAppTheme = AppTheme.Light;
        }
    }*/
    // --- YENİ REVİZE BİTİŞİ ---

    // YENİ EKLENEN DEĞİŞKEN: Switch'in kod tarafından mı yoksa kullanıcı tarafından mı değiştirildiğini anlamak için
    private bool _isMailSwitchProgrammaticChange = false;
    // --- YENİ REVİZE BAŞLANGICI: Nazik Uyarı ile Mail İzni Değiştirme ---
    private async void OnMailIzniToggled(object sender, ToggledEventArgs e)
    {
        // Eğer bu değişiklik kullanıcı tarafından değil de, bizim yazdığımız kod (vazgeçme durumu) tarafından yapıldıysa, metodu çalıştırma ve çık
        if (_isMailSwitchProgrammaticChange) return;

        bool yeniDurum = e.Value;

        // Sadece kullanıcı bildirimleri KAPATMAK istediğinde (False olduğunda) araya girip uyarı veriyoruz
        if (!yeniDurum)
        {
            bool? eminMiSonuc = await ModernAlertService.ShowAsync(
    "E-Posta Bildirimleri",
    "Araç bakımlarınız için size özel hazırladığımız hatırlatmaları ve önemli fırsatları kaçırmanızı istemeyiz. Yine de e-posta bildirimlerini kapatmak istediğinize emin misiniz?",
    "EvetHayir");
            bool eminMi = eminMiSonuc == true;

            if (!eminMi)
            {
                // Kullanıcı "Vazgeç" dediyse, switch'i kod ile tekrar AÇIK (True) hale getiriyoruz
                // Event'in tekrar tetiklenip sonsuz döngüye girmemesi için koruma bayrağını (flag) kullanıyoruz
                _isMailSwitchProgrammaticChange = true;
                MailIzniSwitch.IsToggled = true;
                _isMailSwitchProgrammaticChange = false;

                return; // DB güncellemesi yapmadan işlemi sonlandır
            }
        }

        // Onay verildiyse veya kullanıcı bildirimleri AÇIYORSA (True) doğrudan DB güncellemesine geç
        //var kullaniciIdStr = await SecureStorage.GetAsync("kullanici_id_gizli");
        //if (string.IsNullOrEmpty(kullaniciIdStr)) return;

        //int kullaniciId = int.Parse(kullaniciIdStr);

        // Mail izni değiştirirken:
        var kullaniciIdStr = await SecureStorageHelper.GetUserIdAsync();
        if (string.IsNullOrEmpty(kullaniciIdStr)) return;
        int kullaniciId = int.Parse(kullaniciIdStr);

        try
        {
            var body = new { mail_istiyor_mu = yeniDurum };
            var res = await _apiService.PutAsync($"kullanici/{kullaniciId}/mail-izni", body);

            if (!res.IsSuccessStatusCode)
            {
                // API hatası olursa switch'i sessizce (event'i sonsuz döngüye sokmadan) eski haline al
                _isMailSwitchProgrammaticChange = true;
                MailIzniSwitch.IsToggled = !yeniDurum;
                _isMailSwitchProgrammaticChange = false;

                await ModernAlertService.ShowInfoAsync("Bildirim ayarı güncellenemedi, lütfen bağlantınızı kontrol edin.", "Hata");
            }
        }
        catch (Exception ex)
        {
            _isMailSwitchProgrammaticChange = true;
            MailIzniSwitch.IsToggled = !yeniDurum;
            _isMailSwitchProgrammaticChange = false;

            System.Diagnostics.Debug.WriteLine($"Mail izni güncellenirken hata: {ex.Message}");

            // Kullanıcıya hata mesajını göster
            await ModernAlertService.ShowInfoAsync("Mail izni güncellenirken bir hata oluştu. Lütfen tekrar deneyin.", "Hata");
        }
    }
    // --- YENİ REVİZE BİTİŞİ ---
}