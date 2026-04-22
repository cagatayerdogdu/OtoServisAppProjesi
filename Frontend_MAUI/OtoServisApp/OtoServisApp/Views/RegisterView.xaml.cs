using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class RegisterView : ContentPage
{
    private readonly ApiService _apiService;

    public RegisterView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Temel bilgileri al
        string adSoyad = NameEntry.Text?.Trim();
        string telefon = PhoneEntry.Text?.Trim();
        string eposta = EmailEntry.Text?.Trim().ToLower();
        string sifre = PasswordEntry.Text?.Trim();
        string sifreTekrar = SifreTekrarEntry.Text?.Trim();
        bool mailIstiyorMu = MailIzniCheckBox.IsChecked;

        // Zorunlu alanlar
        if (string.IsNullOrEmpty(adSoyad) || string.IsNullOrEmpty(telefon) ||
            string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(sifreTekrar))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen tüm alanları doldurun.", "Uyarı");
            return;
        }

        // Şifre kuralları
        if (sifre.Length < 6)
        {
            await ModernAlertService.ShowInfoAsync("Şifreniz en az 6 karakter olmalıdır.", "Uyarı");
            PasswordEntry.Text = SifreTekrarEntry.Text = string.Empty;
            PasswordEntry.Focus();
            return;
        }

        if (sifre != sifreTekrar)
        {
            await ModernAlertService.ShowInfoAsync("Şifreler uyuşmuyor, lütfen kontrol edin.", "Uyarı");
            SifreTekrarEntry.Text = string.Empty;
            SifreTekrarEntry.Focus();
            return;
        }

        // E-posta formatı
        if (!IsValidEmail(eposta))
        {
            await ModernAlertService.ShowInfoAsync("Geçerli bir e-posta adresi giriniz.", "Hata");
            return;
        }

        // 1. Aşama: Doğrulama kodu gönder
        RegisterButton.IsEnabled = false;
        RegisterButton.Text = "KOD GÖNDERİLİYOR...";

        bool sent = await _apiService.EpostaDogrulamaKoduGonderAsync(eposta);
        RegisterButton.IsEnabled = true;
        RegisterButton.Text = "KAYIT OL";

        if (!sent)
        {
            await ModernAlertService.ShowInfoAsync("Doğrulama kodu gönderilemedi. Lütfen tekrar deneyin.", "Hata");
            return;
        }

        // 2. Aşama: Kullanıcıdan kodu al
        string? dogrulamaKodu = await DisplayPromptAsync(
            "E-posta Doğrulama",
            $"Lütfen {eposta} adresine gönderilen 6 haneli doğrulama kodunu giriniz:",
            "Onayla",
            "İptal",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrEmpty(dogrulamaKodu))
        {
            await ModernAlertService.ShowInfoAsync("Doğrulama kodu girmediniz. Kayıt iptal edildi.", "Bilgi");
            return;
        }

        // 3. Aşama: Kaydı tamamla
        RegisterButton.IsEnabled = false;
        RegisterButton.Text = "KAYDEDİLİYOR...";

        string sonuc = await _apiService.DogrulaVeKaydetAsync(adSoyad, telefon, eposta, sifre, mailIstiyorMu, dogrulamaKodu);

        if (sonuc == "OK")
        {
            await ModernAlertService.ShowInfoAsync("Hesabınız başarıyla oluşturuldu. Şimdi giriş yapabilirsiniz.", "Başarılı");
            await Navigation.PopAsync();
        }
        else
        {
            await ModernAlertService.ShowInfoAsync(sonuc, "Kayıt İşlemi Durduruldu");
            RegisterButton.IsEnabled = true;
            RegisterButton.Text = "KAYIT OL";
        }
    }

    private static bool IsValidEmail(string email)
    {
        try { _ = new System.Net.Mail.MailAddress(email); return true; }
        catch { return false; }
    }
}