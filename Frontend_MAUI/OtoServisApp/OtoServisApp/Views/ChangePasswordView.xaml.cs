using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class ChangePasswordView : ContentPage
{
    private readonly ApiService _apiService;
    private Kullanici _aktifKullanici;

    public ChangePasswordView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string eskiSifre = OldPasswordEntry.Text?.Trim();
        string yeniSifre = NewPasswordEntry.Text?.Trim();
        string yeniSifreTekrar = ConfirmPasswordEntry.Text?.Trim();

        if (string.IsNullOrEmpty(eskiSifre) || string.IsNullOrEmpty(yeniSifre) || string.IsNullOrEmpty(yeniSifreTekrar))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen tüm alanları doldurun.", "Uyarı");
            return;
        }

        // --- YENİ REVİZE BAŞLANGICI: Şifre Uzunluk Kontrolü (Madde 71) ---
        if (yeniSifre.Length < 6)
        {
            await ModernAlertService.ShowInfoAsync("Güvenliğiniz için yeni şifreniz en az 6 karakterden oluşmalıdır.", "Uyarı");
            NewPasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
            NewPasswordEntry.Focus();
            return;
        }
        // --- YENİ REVİZE BİTİŞİ ---

        if (yeniSifre != yeniSifreTekrar)
        {
            await ModernAlertService.ShowInfoAsync("Yeni şifreler birbiriyle uyuşmuyor.", "Hata");

            // Kullanıcı dostu UX: Sadece tekrar kutusunu temizleyip oraya odaklan
            ConfirmPasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Focus();
            return;
        }

        SaveButton.IsEnabled = false;
        SaveButton.Text = "GÜNCELLENİYOR...";

        string sonuc = await _apiService.SifreDegistirAsync(_aktifKullanici.id, eskiSifre, yeniSifre);

        if (sonuc == "OK")
        {
            await ModernAlertService.ShowInfoAsync("Şifreniz güvenli bir şekilde güncellendi.", "Başarılı");

            // Kullanıcıyı dışarı atmıyoruz, sadece bir önceki ekrana (Profile) döndürüyoruz!
            await Navigation.PopAsync();
        }
        else
        {
            await ModernAlertService.ShowInfoAsync(sonuc, "İşlem Başarısız");
            SaveButton.IsEnabled = true;
            SaveButton.Text = "ŞİFREYİ GÜNCELLE";
        }
    }
}