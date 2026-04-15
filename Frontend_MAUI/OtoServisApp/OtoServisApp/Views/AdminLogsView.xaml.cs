using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminLogsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<SistemLog> _sonCekilenLoglar;

    // MADDE 82: Sayfalama ve Kayıt Parametreleri (Parametrik yapıldı)
    private int _mevcutSayfa = 1;
    private int _toplamSayfa = 1;
    private int _sayfaBasinaKayit = 20; // İstediğin zaman buradan değiştirebilirsin Abi

    public AdminLogsView()
    {
        InitializeComponent();
        _apiService = new ApiService();

        // MADDE 81: Dropdown İçerikleri (Talepleri Yönet ekranındaki gibi)
        SeviyeListesi.ItemsSource = new List<string> { "Tümü", "ERROR", "WARNING", "INFO" };
        TarihKriterListesi.ItemsSource = new List<string> { "Bugün", "Tek Tarih", "İki Tarih Arası", "Seçili Tarihten Sonra (>=)", "Seçili Tarihten Önce (<=)", "Tüm Zamanlar" };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Eski çalışan varsayılanların:
        SecilenTarihKriteriButonu.Text = "Bugün";
        SecilenSeviyeButonu.Text = "ERROR"; // Senin orijinal kodunda Index 1 yani ERROR'du
        AramaBar.Text = string.Empty;

        _mevcutSayfa = 1;
        await SorgulamaYap();
    }

    // --- MADDE 81: DROPDOWN AÇ/KAPAT VE SEÇİM MANTIKLARI ---

    private void OnSeviyeKutusuAcKapat(object sender, EventArgs e)
    {
        SeviyeSecimKutusu.IsVisible = !SeviyeSecimKutusu.IsVisible;
        TarihSecimKutusu.IsVisible = false; // Diğeri kapansın
    }

    private void OnSeviyeSecildi(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string seviye)
        {
            SecilenSeviyeButonu.Text = seviye;
            SeviyeSecimKutusu.IsVisible = false;
            SeviyeListesi.SelectedItem = null;
            // Seçim değişince otomatik sorgula dersen buraya SorgulamaYap() ekleyebiliriz.
        }
    }

    private void OnTarihKriteriKutusuAcKapat(object sender, EventArgs e)
    {
        TarihSecimKutusu.IsVisible = !TarihSecimKutusu.IsVisible;
        SeviyeSecimKutusu.IsVisible = false; // Diğeri kapansın
    }

    private void OnTarihKriteriSecildi(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string kriter)
        {
            SecilenTarihKriteriButonu.Text = kriter;
            TarihSecimKutusu.IsVisible = false;
            TarihKriterListesi.SelectedItem = null;

            // Eski Picker index mantığını buraya taşıdım (DatePicker görünürlüğü için)
            int index = ((List<string>)TarihKriterListesi.ItemsSource).IndexOf(kriter);
            BaslangicKutusu.IsVisible = index == 1 || index == 2 || index == 3 || index == 4;
            BitisKutusu.IsVisible = index == 2;
        }
    }

    // --- ESKİ ÇALIŞAN SORGULAMA VE SAYFALAMA MANTIKLARI ---

    private async void OnSorgulaClicked(object sender, EventArgs e)
    {
        _mevcutSayfa = 1;
        await SorgulamaYap();
    }

    private async void OnOncekiSayfaClicked(object sender, EventArgs e)
    {
        if (_mevcutSayfa > 1)
        {
            _mevcutSayfa--;
            await SorgulamaYap();
        }
    }

    private async void OnSonrakiSayfaClicked(object sender, EventArgs e)
    {
        if (_mevcutSayfa < _toplamSayfa)
        {
            _mevcutSayfa++;
            await SorgulamaYap();
        }
    }

    private async Task SorgulamaYap()
    {
        string seviye = SecilenSeviyeButonu.Text;

        DateTime? baslangic = null;
        DateTime? bitis = null;

        // Tarih kriteri indexini bulup eski mantığını uyguluyoruz
        int kriterIndex = ((List<string>)TarihKriterListesi.ItemsSource).IndexOf(SecilenTarihKriteriButonu.Text);

        if (kriterIndex == 0) { baslangic = DateTime.Today; bitis = DateTime.Today; }
        else if (kriterIndex == 1) { baslangic = BaslangicDatePicker.Date; bitis = BaslangicDatePicker.Date; }
        else if (kriterIndex == 2) { baslangic = BaslangicDatePicker.Date; bitis = BitisDatePicker.Date; }
        else if (kriterIndex == 3) { baslangic = BaslangicDatePicker.Date; }
        else if (kriterIndex == 4) { bitis = BaslangicDatePicker.Date; }

        OncekiSayfaBtn.IsEnabled = false;
        SonrakiSayfaBtn.IsEnabled = false;

        // API'YE GİT VE PAKETİ ÇEK (Sayfa başı kayıt artık 30 oldu)
        var response = await _apiService.AdminLoglariGetirAsync(seviye, baslangic, bitis, _mevcutSayfa, _sayfaBasinaKayit);

        if (response != null)
        {
            _sonCekilenLoglar = response.loglar;
            _toplamSayfa = response.toplam_sayfa;
            _mevcutSayfa = response.mevcut_sayfa;

            // ESKİDEN ÇALIŞAN BİLGİ ETİKETİ (Aynen korundu)
            KayitBilgiLabel.Text = $"DB Toplam Kayıt: {response.toplam_kayit} | Filtrelenen: {response.filtreli_kayit}";
            SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";

            OncekiSayfaBtn.IsEnabled = _mevcutSayfa > 1;
            SonrakiSayfaBtn.IsEnabled = _mevcutSayfa < _toplamSayfa;

            YerelAramaUygula();
        }
    }

    private void OnAramaDegisti(object sender, TextChangedEventArgs e)
    {
        YerelAramaUygula();
    }

    private void YerelAramaUygula()
    {
        if (_sonCekilenLoglar == null) return;

        var liste = _sonCekilenLoglar.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            // .NET 8 Standart Arama Yapısı (Hatasız ve Eski Kodunla Aynı)
            var metin = AramaBar.Text;
            liste = liste.Where(l =>
                (l.kullanici_ad_soyad != null && l.kullanici_ad_soyad.Contains(metin, StringComparison.OrdinalIgnoreCase)) ||
                (l.detay != null && l.detay.Contains(metin, StringComparison.OrdinalIgnoreCase)) ||
                (l.islem != null && l.islem.Contains(metin, StringComparison.OrdinalIgnoreCase))
            );
        }

        LogsList.ItemsSource = liste.ToList();
    }

    private async void OnLogTapped(object sender, EventArgs e)
    {
        var border = sender as Border;
        var log = border?.BindingContext as SistemLog;

        if (log != null && !string.IsNullOrEmpty(log.detay))
        {
            // Log detayını telefonun panosuna kopyalar
            await Clipboard.Default.SetTextAsync(log.detay);

            // Kullanıcıya kopyalandığına dair ufak bir bildirim verelim
            await DisplayAlert("Kopyalandı", "Hata detayı panoya kopyalandı. Şimdi dilediğiniz yere yapıştırıp aratabilirsiniz.", "Tamam");
        }
    }
}