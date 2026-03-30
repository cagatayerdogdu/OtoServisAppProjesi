using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class ForgotPasswordView : ContentPage
{
    private readonly ApiService _apiService;

    public ForgotPasswordView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        string eposta = EmailEntry.Text?.Trim().ToLower();

        if (string.IsNullOrEmpty(eposta))
        {
            await DisplayAlert("Uyarı", "Lütfen e-posta adresinizi girin.", "Tamam");
            return;
        }

        ResetButton.IsEnabled = false;
        ResetButton.Text = "GÖNDERİLİYOR...";

        string sonuc = await _apiService.SifreSifirlamaTalepEtAsync(eposta);
        // Bu kısım mail tanımları olduktan sonra hayata geçecek
        if (sonuc == "OK")
        {
            await DisplayAlert("Başarılı", "Şifre sıfırlama talimatları e-posta adresinize gönderildi.", "Tamam");
            // Direkt ana giriş ekranına geri dön
            await Navigation.PopAsync();
        }
        /* Bu kısım mail tanımları öncesinde geçici olarak kullandığım uygulama içinden şifre sıfırlama ekranı içindi.
        if (sonuc == "OK")
        {
            //await DisplayAlert("Bilgi", "Normalde burada mail atılacaktı. Test aşamasında olduğumuz için sizi doğrudan şifre yenileme ekranına yönlendiriyoruz.", "Devam Et");
            await DisplayAlert("Bilgi", "Şifre yenileme ekranına yönlendiriyoruz.", "Devam Et");
            // E-posta adresini yeni ekrana parametre olarak gönderiyoruz
            await Navigation.PushAsync(new ResetPasswordView(eposta));
        }
        */
        else
        {
            await DisplayAlert("Hata", sonuc, "Tamam");
        }

        ResetButton.IsEnabled = true;
        ResetButton.Text = "SIFIRLAMA BAĞLANTISI GÖNDER";
    }
}