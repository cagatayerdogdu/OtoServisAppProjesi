using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Net.Http.Json;

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
        string adSoyad = NameEntry.Text?.Trim();
        string telefon = PhoneEntry.Text?.Trim();
        string eposta = EmailEntry.Text?.Trim().ToLower();
        string sifre = PasswordEntry.Text?.Trim();
        string sifreTekrar = SifreTekrarEntry.Text?.Trim(); 

        if (!string.IsNullOrEmpty(sifre) && sifre.Length < 6)
        {
            await ModernAlertService.ShowInfoAsync("Güvenliğiniz için şifreniz en az 6 karakterden oluşmalıdır.", "Uyarı");
            PasswordEntry.Text = string.Empty;
            SifreTekrarEntry.Text = string.Empty;
            PasswordEntry.Focus();
            return;
        }

        try
        {
            var addr = new System.Net.Mail.MailAddress(EmailEntry.Text);
            if (addr.Address != EmailEntry.Text)
            {
                await ModernAlertService.ShowInfoAsync("Lütfen geçerli bir e-posta adresi giriniz.", "Hata");
                return;
            }
        }
        catch
        {
            await ModernAlertService.ShowInfoAsync("Hata", "Lütfen geçerli bir e-posta adresi giriniz.");

            return;
        }

        if (string.IsNullOrEmpty(adSoyad) || string.IsNullOrEmpty(telefon) || string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(sifreTekrar))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen tüm alanları doldurun.", "Hata");
            return;
        }

        if (sifre != sifreTekrar)
        {
            await ModernAlertService.ShowInfoAsync("Girdiğiniz şifreler birbiriyle eşleşmiyor. Lütfen kontrol edin.", "Hata");
            SifreTekrarEntry.Text = string.Empty;
            SifreTekrarEntry.Focus();
            return;
        }

        RegisterButton.IsEnabled = false;
        RegisterButton.Text = "KAYDEDİLİYOR...";

        // YENİ EKLENEN: Checkbox durumunu oku
        bool mailIstiyorMu = MailIzniCheckBox.IsChecked;

        // API'ye gönderilecek veriye ekle
        var yeniKullanici = new
        {
            ad_soyad = adSoyad,
            telefon = telefon,
            eposta = eposta,
            sifre = sifre,
            mail_istiyor_mu = mailIstiyorMu // YENİ EKLENDİ
        };

        try
        {
            string sonuc = await _apiService.KullaniciKayitAsync(yeniKullanici);

            if (sonuc == "OK")
            {
                await ModernAlertService.ShowInfoAsync("Hesabınız oluşturuldu. Şimdi giriş yapabilirsiniz.", "Başarılı");
                await Navigation.PopAsync(); 
            }
            else
            {
                await ModernAlertService.ShowInfoAsync(sonuc, "Kayıt İşlemi Durduruldu");
            }
        }
        catch (Exception)
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