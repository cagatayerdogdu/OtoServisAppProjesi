using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminLogsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<SistemLog> _sonCekilenLoglar;

    // SAYFALAMA DEĞİŞKENLERİ
    private int _mevcutSayfa = 1;
    private int _toplamSayfa = 1;

    public AdminLogsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        TarihKriteriPicker.SelectedIndex = 0; // Bugün
        SeviyePicker.SelectedIndex = 1; // ERROR
        AramaBar.Text = string.Empty;

        _mevcutSayfa = 1;
        await SorgulamaYap();
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

    // --- ANA SORGULAMA MOTORU ---
    private async Task SorgulamaYap()
    {
        string seviye = "Tümü";
        if (SeviyePicker.SelectedIndex >= 0)
        {
            seviye = SeviyePicker.Items[SeviyePicker.SelectedIndex];
        }

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
        var response = await _apiService.AdminLoglariGetirAsync(seviye, baslangic, bitis, _mevcutSayfa, 50);

        if (response != null)
        {
            _sonCekilenLoglar = response.loglar;
            _toplamSayfa = response.toplam_sayfa;
            _mevcutSayfa = response.mevcut_sayfa;

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
            // .NET 8 Standart Arama Yapısı (Hatasız)
            var metin = AramaBar.Text;
            liste = liste.Where(l =>
                (l.kullanici_ad_soyad != null && l.kullanici_ad_soyad.Contains(metin, StringComparison.OrdinalIgnoreCase)) ||
                (l.detay != null && l.detay.Contains(metin, StringComparison.OrdinalIgnoreCase)) ||
                (l.islem != null && l.islem.Contains(metin, StringComparison.OrdinalIgnoreCase))
            );
        }

        LogsList.ItemsSource = liste.ToList();
    }
}