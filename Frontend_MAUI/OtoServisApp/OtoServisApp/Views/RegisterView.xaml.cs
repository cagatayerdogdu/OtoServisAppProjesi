using OtoServisApp.Services;
using System.Net.Http;

namespace OtoServisApp.Views;

public partial class RegisterView : ContentPage
{
    private readonly ApiService _apiService;
    private string _verifiedEmail = null;

    public RegisterView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnSendVerificationCodeClicked(object sender, TappedEventArgs e)
    {
        string eposta = EmailEntry.Text?.Trim().ToLower();
        if (string.IsNullOrEmpty(eposta))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen e-posta adresinizi girin.", "Uyarı");
            return;
        }

        try { new System.Net.Mail.MailAddress(eposta); }
        catch { await ModernAlertService.ShowInfoAsync("Geçerli bir e-posta adresi giriniz.", "Hata"); return; }

        SendCodeLabel.Text = "Gönderiliyor...";

        try
        {
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("eposta", eposta) });
            var response = await _apiService.PostAsync("kayit/eposta-dogrulama-kodu", content);

            if (response.IsSuccessStatusCode)
            {
                _verifiedEmail = eposta;
                VerificationCodeEntry.IsVisible = true;
                VerificationCodeEntry.Focus();
                await ModernAlertService.ShowInfoAsync("Doğrulama kodu e-posta adresinize gönderildi.", "Bilgi");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await ModernAlertService.ShowInfoAsync(error, "Hata");
            }
        }
        catch
        {
            await ModernAlertService.ShowInfoAsync("Kod gönderilemedi, lütfen tekrar deneyin.", "Hata");
        }
        finally
        {
            SendCodeLabel.Text = "Kod Gönder";
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Temel alanları al
        string adSoyad = NameEntry.Text?.Trim();
        string telefon = PhoneEntry.Text?.Trim();
        string eposta = EmailEntry.Text?.Trim().ToLower();
        string sifre = PasswordEntry.Text?.Trim();
        string sifreTekrar = SifreTekrarEntry.Text?.Trim();
        bool mailIstiyorMu = MailIzniCheckBox.IsChecked;

        // --- Doğrulamalar ---
        if (string.IsNullOrEmpty(adSoyad) || string.IsNullOrEmpty(telefon) || string.IsNullOrEmpty(eposta) ||
            string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(sifreTekrar))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen tüm alanları doldurun.", "Hata");
            return;
        }

        if (sifre.Length < 6)
        {
            await ModernAlertService.ShowInfoAsync("Güvenliğiniz için şifreniz en az 6 karakterden oluşmalıdır.", "Uyarı");
            PasswordEntry.Text = string.Empty;
            SifreTekrarEntry.Text = string.Empty;
            PasswordEntry.Focus();
            return;
        }

        if (sifre != sifreTekrar)
        {
            await ModernAlertService.ShowInfoAsync("Girdiğiniz şifreler birbiriyle eşleşmiyor.", "Hata");
            SifreTekrarEntry.Text = string.Empty;
            SifreTekrarEntry.Focus();
            return;
        }

        try { new System.Net.Mail.MailAddress(eposta); }
        catch { await ModernAlertService.ShowInfoAsync("Geçerli bir e-posta adresi giriniz.", "Hata"); return; }

        // Doğrulama kodu kontrolü
        if (string.IsNullOrEmpty(_verifiedEmail) || _verifiedEmail != eposta)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen önce e-posta adresinizi doğrulayın.", "Uyarı");
            return;
        }

        string dogrulamaKodu = VerificationCodeEntry.Text?.Trim();
        if (string.IsNullOrEmpty(dogrulamaKodu))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen doğrulama kodunu girin.", "Uyarı");
            return;
        }

        // --- Kayıt İşlemi ---
        RegisterButton.IsEnabled = false;
        RegisterButton.Text = "KAYDEDİLİYOR...";

        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ad_soyad", adSoyad),
                new KeyValuePair<string, string>("telefon", telefon),
                new KeyValuePair<string, string>("eposta", eposta),
                new KeyValuePair<string, string>("sifre", sifre),
                new KeyValuePair<string, string>("mail_istiyor_mu", mailIstiyorMu.ToString()),
                new KeyValuePair<string, string>("dogrulama_kodu", dogrulamaKodu)
            });

            var response = await _apiService.PostAsync("kayit/dogrula-ve-kaydet", content);

            if (response.IsSuccessStatusCode)
            {
                await ModernAlertService.ShowInfoAsync("Hesabınız başarıyla oluşturuldu. Şimdi giriş yapabilirsiniz.", "Başarılı");
                await Navigation.PopAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await ModernAlertService.ShowInfoAsync(error, "Kayıt İşlemi Durduruldu");
            }
        }
        catch
        {
            await ModernAlertService.ShowInfoAsync("Beklenmeyen bir hata oluştu.", "Hata");
        }
        finally
        {
            RegisterButton.IsEnabled = true;
            RegisterButton.Text = "KAYIT OL";
        }
    }
}