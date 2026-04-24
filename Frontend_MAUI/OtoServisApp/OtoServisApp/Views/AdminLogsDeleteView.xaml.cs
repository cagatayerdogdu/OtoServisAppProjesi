using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminLogsDeleteView : ContentPage
{
    private readonly ApiService _apiService;
    private List<string> _tarihKriterleri = new() { "Bugün", "Tek Tarih", "İki Tarih Arası", "Seçili Tarihten Önce (<=)" };

    public AdminLogsDeleteView()
    {
        InitializeComponent();
        _apiService = new ApiService();
        TarihKriterListesi.ItemsSource = _tarihKriterleri;
    }

    private void OnTarihKriteriKutusuAcKapat(object sender, EventArgs e)
    {
        TarihSecimKutusu.IsVisible = !TarihSecimKutusu.IsVisible;
    }

    private void OnTarihKriteriSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            SecilenTarihKriteriButonu.Text = secilen;
            TarihSecimKutusu.IsVisible = false;
            TarihKriterListesi.SelectedItem = null;

            // Tarih alanlarının görünürlüğünü ayarla
            BaslangicKutusu.IsVisible = secilen != "Bugün";
            BitisKutusu.IsVisible = secilen == "İki Tarih Arası";
        }
    }

    private void OnSecimTemizleChecked(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            RadioLog.IsChecked = false;
            RadioBildirim.IsChecked = false;
        }
    }

    private async void OnSilClicked(object sender, EventArgs e)
    {
        // Tablo seçimi kontrolü
        string tablo = "";
        if (RadioLog.IsChecked) tablo = "log";
        else if (RadioBildirim.IsChecked) tablo = "bildirim";
        else
        {
            await ModernAlertService.ShowInfoAsync("Lütfen silinecek bir tablo seçiniz.", "Uyarı");
            return;
        }

        // Tarih kriterine göre parametreler
        string kriter = SecilenTarihKriteriButonu.Text switch
        {
            "Bugün" => "bugun",
            "Tek Tarih" => "tek_tarih",
            "İki Tarih Arası" => "iki_tarih_arasi",
            _ => "once"
        };

        DateTime? baslangic = null;
        DateTime? bitis = null;

        if (kriter == "tek_tarih" || kriter == "iki_tarih_arasi" || kriter == "once")
        {
            baslangic = BaslangicDatePicker.Date;
        }
        if (kriter == "iki_tarih_arasi")
        {
            bitis = BitisDatePicker.Date;
        }

        // Silme onayı
        string tabloAdi = tablo == "log" ? "Sistem Logları" : "Sistem Bildirimleri";
        bool onay = await ModernAlertService.ShowConfirmationAsync(
            $"Seçilen kriterlere göre '{tabloAdi}' tablosundan veriler silinecektir. Onaylıyor musunuz?",
            "Silme Onayı");
        if (!onay) return;

        // Loading göster
        LoadingOverlay.IsVisible = true;
        LoadingTitle.Text = "Veriler Siliniyor...";

        try
        {
            int silinenSayi = await _apiService.TopluSilAsync(tablo, kriter, baslangic, bitis);

            if (silinenSayi >= 0)
            {
                await ModernAlertService.ShowInfoAsync($"{silinenSayi} adet kayıt başarıyla silindi.", "Başarılı");
            }
            else
            {
                await ModernAlertService.ShowInfoAsync("Silme işlemi sırasında bir hata oluştu.", "Hata");
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync($"Hata: {ex.Message}", "Hata");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }
}