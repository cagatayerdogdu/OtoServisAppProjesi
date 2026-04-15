using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminLogsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<SistemLog> _hamCekilenLoglar; // API'den gelen ham veri
    private List<SistemLog> _filtrelenmisLoglar; // Yerel arama sonrası liste

    // SAYFALAMA DEĞİŞKENLERİ
    private int _mevcutSayfa = 1;
    private int _toplamSayfa = 1;
    private int _sayfaBasinaKayit = 50;

    public AdminLogsView()
    {
        InitializeComponent();
        _apiService = new ApiService();

        // MADDE 81: Yeni Dropdown listesi
        SeviyeListesi.ItemsSource = new List<string> { "Tümü", "ERROR", "WARNING", "INFO" };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        TarihKriteriPicker.SelectedIndex = 0; // Bugün
        SecilenSeviyeButonu.Text = "Tümü"; // Varsayılan Dropdown Text
        AramaBar.Text = string.Empty;

        _mevcutSayfa = 1;
        await SorgulamaYap();
    }

    // --- MADDE 81: YENİ SEVİYE DROPDOWN KODLARI ---
    private void OnSeviyeKutusuAcKapat(object sender, EventArgs e)
    {
        SeviyeSecimKutusu.IsVisible = !SeviyeSecimKutusu.IsVisible;
    }

    private void OnSeviyeSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            SecilenSeviyeButonu.Text = secilen;
            SeviyeSecimKutusu.IsVisible = false;
            SeviyeListesi.SelectedItem = null; // Seçimi temizle ki tekrar seçilebilsin
        }
    }

    private void OnTarihKriteriDegisti(object sender, EventArgs e)
    {
        var secim = TarihKriteriPicker.SelectedIndex;
        BaslangicKutusu.IsVisible = secim == 1 || secim == 2 || secim == 3 || secim == 4;
        BitisKutusu.IsVisible = secim == 2;
    }

    private async void OnSorgulaClicked(object sender, EventArgs e)
    {
        _mevcutSayfa = 1;
        await SorgulamaYap();
    }

    // --- SAYFALAMA BUTONLARI ---
    private void OnOncekiSayfaClicked(object sender, EventArgs e)
    {
        if (_mevcutSayfa > 1)
        {
            _mevcutSayfa--;
            SayfayiCiz(); // Artık API'ye gitmiyoruz, yerel listeyi kaydırıyoruz
        }
    }

    private void OnSonrakiSayfaClicked(object sender, EventArgs e)
    {
        if (_mevcutSayfa < _toplamSayfa)
        {
            _mevcutSayfa++;
            SayfayiCiz(); // Artık API'ye gitmiyoruz, yerel listeyi kaydırıyoruz
        }
    }

    // --- ANA SORGULAMA MOTORU ---
    private async Task SorgulamaYap()
    {
        string seviye = SecilenSeviyeButonu.Text; // Yeni yapıdan alıyoruz

        DateTime? baslangic = null;
        DateTime? bitis = null;
        var kriter = TarihKriteriPicker.SelectedIndex;

        if (kriter == 0) { baslangic = DateTime.Today; bitis = DateTime.Today; }
        else if (kriter == 1) { baslangic = BaslangicDatePicker.Date; bitis = BaslangicDatePicker.Date; }
        else if (kriter == 2) { baslangic = BaslangicDatePicker.Date; bitis = BitisDatePicker.Date; }
        else if (kriter == 3) { baslangic = BaslangicDatePicker.Date; }
        else if (kriter == 4) { bitis = BaslangicDatePicker.Date; }

        OncekiSayfaBtn.IsEnabled = false;
        SonrakiSayfaBtn.IsEnabled = false;

        // API'YE GİT VE PAKETİ ÇEK
        var response = await _apiService.AdminLoglariGetirAsync(seviye, baslangic, bitis, 1, 9999); // API limit eziyorsa biz de hepsini isteriz

        if (response != null && response.loglar != null)
        {
            _hamCekilenLoglar = response.loglar;
            YerelAramaUygula(); // Bu metod sayfalamayı da halledecek
        }
    }

    private void OnAramaDegisti(object sender, TextChangedEventArgs e)
    {
        _mevcutSayfa = 1; // Arama değiştiğinde ilk sayfaya dön
        YerelAramaUygula();
    }

    // MADDE 82: FİLTRELEME VE ZORUNLU YEREL SAYFALAMA
    private void YerelAramaUygula()
    {
        if (_hamCekilenLoglar == null) return;

        var liste = _hamCekilenLoglar.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            var metin = AramaBar.Text;
            liste = liste.Where(l =>
                (l.kullanici_ad_soyad != null && l.kullanici_ad_soyad.Contains(metin, StringComparison.OrdinalIgnoreCase)) ||
                (l.detay != null && l.detay.Contains(metin, StringComparison.OrdinalIgnoreCase)) ||
                (l.islem != null && l.islem.Contains(metin, StringComparison.OrdinalIgnoreCase))
            );
        }

        _filtrelenmisLoglar = liste.ToList();

        // Backend'in yapamadığı sayfalamayı biz manuel hesaplıyoruz
        _toplamSayfa = (int)Math.Ceiling((double)_filtrelenmisLoglar.Count / _sayfaBasinaKayit);
        if (_toplamSayfa == 0) _toplamSayfa = 1;

        SayfayiCiz();
    }

    // Veriyi 50'şer 50'şer kesip arayüze basan yama metodumuz
    private void SayfayiCiz()
    {
        if (_filtrelenmisLoglar == null) return;

        var gosterilecekListe = _filtrelenmisLoglar
                                .Skip((_mevcutSayfa - 1) * _sayfaBasinaKayit)
                                .Take(_sayfaBasinaKayit)
                                .ToList();

        LogsList.ItemsSource = gosterilecekListe;

        KayitBilgiLabel.Text = $"Bulunan Toplam Kayıt: {_filtrelenmisLoglar.Count}";
        SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";

        OncekiSayfaBtn.IsEnabled = _mevcutSayfa > 1;
        SonrakiSayfaBtn.IsEnabled = _mevcutSayfa < _toplamSayfa;
    }
}