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
            await DisplayAlert("Uyarı", "Güvenliğiniz için şifreniz en az 6 karakterden oluşmalıdır.", "Tamam");
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
                await DisplayAlert("Hata", "Lütfen geçerli bir e-posta adresi giriniz.", "Tamam");
                return;
            }
        }
        catch
        {
            await DisplayAlert("Hata", "Lütfen geçerli bir e-posta adresi giriniz.", "Tamam");
            return;
        }

        if (string.IsNullOrEmpty(adSoyad) || string.IsNullOrEmpty(telefon) || string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(sifreTekrar))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları doldurun.", "Tamam");
            return;
        }

        if (sifre != sifreTekrar)
        {
            await DisplayAlert("Hata", "Girdiğiniz şifreler birbiriyle eşleşmiyor. Lütfen kontrol edin.", "Tamam");
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
                await DisplayAlert("Başarılı", "Hesabınız oluşturuldu. Şimdi giriş yapabilirsiniz.", "Harika!");
                await Navigation.PopAsync(); 
            }
            else
            {
                await DisplayAlert("Kayıt İşlemi Durduruldu", sonuc, "Tamam");
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Hata", "Beklenmeyen bir hata oluştu.", "Tamam");
        }
        finally
        {
            RegisterButton.IsEnabled = true;
            RegisterButton.Text = "KAYIT OL";
        }
    }
}