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
        string sifreTekrar = SifreTekrarEntry.Text?.Trim(); // YENİ: Tekrar alanı okundu

        // --- YENİ REVİZE BAŞLANGICI: Email Doğrulama (Madde 51) ---
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
        // --- YENİ REVİZE BİTİŞİ ---

        // 1. BOŞLUK KONTROLÜ (Tekrar alanı da eklendi)
        if (string.IsNullOrEmpty(adSoyad) || string.IsNullOrEmpty(telefon) || string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(sifreTekrar))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları doldurun.", "Tamam");
            return;
        }

        // 2. ŞİFRE EŞLEŞME KONTROLÜ (MADDE 24)
        if (sifre != sifreTekrar)
        {
            await DisplayAlert("Hata", "Girdiğiniz şifreler birbiriyle eşleşmiyor. Lütfen kontrol edin.", "Tamam");

            // Yanlışsa sadece tekrar kutusunu silip oraya odaklansın (Kullanıcı dostu UX)
            SifreTekrarEntry.Text = string.Empty;
            SifreTekrarEntry.Focus();
            return;
        }

        RegisterButton.IsEnabled = false;
        RegisterButton.Text = "KAYDEDİLİYOR...";

        // API'ye gönderilecek veriyi hazırlıyoruz (KullaniciCreate şemasına uygun)
        var yeniKullanici = new
        {
            ad_soyad = adSoyad,
            telefon = telefon,
            eposta = eposta,
            sifre = sifre
        };

        try
        {
            // Yazdığımız güvenli metodu çağırıyoruz
            string sonuc = await _apiService.KullaniciKayitAsync(yeniKullanici);

            if (sonuc == "OK")
            {
                await DisplayAlert("Başarılı", "Hesabınız oluşturuldu. Şimdi giriş yapabilirsiniz.", "Harika!");
                await Navigation.PopAsync(); // Login ekranına geri döner
            }
            else
            {
                // API'den dönen hatayı ekrana bas
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