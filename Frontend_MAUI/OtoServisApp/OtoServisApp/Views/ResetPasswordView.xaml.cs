using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class ResetPasswordView : ContentPage
{
    private readonly ApiService _apiService;
    private string _eposta;

    public ResetPasswordView(string eposta)
    {
        InitializeComponent();
        _eposta = eposta;
        EmailEntry.Text = _eposta; // Önceki sayfadan gelen e-postayı ekrana yazdırıyoruz
        _apiService = new ApiService();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string sifre1 = NewPasswordEntry.Text?.Trim();
        string sifre2 = ConfirmPasswordEntry.Text?.Trim();

        // --- YENİ REVİZE BAŞLANGICI: Şifre Uzunluk Kontrolü (Madde 71) ---
        if (!string.IsNullOrEmpty(sifre1) && sifre1.Length < 6)
        {
            await DisplayAlert("Uyarı", "Güvenliğiniz için yeni şifreniz en az 6 karakterden oluşmalıdır.", "Tamam");
            NewPasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
            NewPasswordEntry.Focus();
            return;
        }
        // --- YENİ REVİZE BİTİŞİ ---

        if (string.IsNullOrEmpty(sifre1) || string.IsNullOrEmpty(sifre2))
        {
            await DisplayAlert("Hata", "Lütfen şifre alanlarını doldurun.", "Tamam");
            return;
        }

        if (sifre1 != sifre2)
        {
            await DisplayAlert("Hata", "Şifreler uyuşmuyor, lütfen kontrol edin.", "Tamam");
            return;
        }

        SaveButton.IsEnabled = false;
        SaveButton.Text = "GÜNCELLENİYOR...";

        bool basarili = await _apiService.YeniSifreKaydetAsync(_eposta, sifre1);

        if (basarili)
        {
            await DisplayAlert("Başarılı", "Şifreniz güncellendi! Artık yeni şifrenizle giriş yapabilirsiniz.", "Harika");
            // Kullanıcıyı en baştaki Giriş sayfasına geri yolluyoruz
            await Navigation.PopToRootAsync();
        }
        else
        {
            await DisplayAlert("Hata", "Şifre güncellenirken bir sorun oluştu.", "Tamam");
            SaveButton.IsEnabled = true;
            SaveButton.Text = "ŞİFREYİ GÜNCELLE";
        }
    }
}