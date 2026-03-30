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
        _apiService = new ApiService();
        _aktifKullanici = kullanici;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string eskiSifre = OldPasswordEntry.Text?.Trim();
        string yeniSifre = NewPasswordEntry.Text?.Trim();
        string yeniSifreTekrar = ConfirmPasswordEntry.Text?.Trim();

        if (string.IsNullOrEmpty(eskiSifre) || string.IsNullOrEmpty(yeniSifre) || string.IsNullOrEmpty(yeniSifreTekrar))
        {
            await DisplayAlert("Uyarı", "Lütfen tüm alanları doldurun.", "Tamam");
            return;
        }

        if (yeniSifre != yeniSifreTekrar)
        {
            await DisplayAlert("Hata", "Yeni şifreler birbiriyle uyuşmuyor.", "Tamam");
            return;
        }

        SaveButton.IsEnabled = false;
        SaveButton.Text = "GÜNCELLENİYOR...";

        string sonuc = await _apiService.SifreDegistirAsync(_aktifKullanici.id, eskiSifre, yeniSifre);

        if (sonuc == "OK")
        {
            await DisplayAlert("Başarılı", "Şifreniz güvenli bir şekilde güncellendi.", "Harika");

            // Kullanıcıyı dışarı atmıyoruz, sadece bir önceki ekrana (Profile) döndürüyoruz!
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("İşlem Başarısız", sonuc, "Tamam");
            SaveButton.IsEnabled = true;
            SaveButton.Text = "ŞİFREYİ GÜNCELLE";
        }
    }
}