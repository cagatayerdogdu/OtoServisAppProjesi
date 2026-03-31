using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminRequestsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<ServisTalebi> _orijinalTalepler;

    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde", "Tamamlandı", "İptal Edildi" };
    private string _secilenDurum = "Tümü";

    public AdminRequestsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DurumListesi.ItemsSource = _durumFiltreleri;
        await VerileriYukle();
    }

    private async Task VerileriYukle()
    {
        // Veriler yüklenirken mevcut listeyi temizleyebiliriz
        _tumHizmetler = await _apiService.HizmetleriGetirAsync();
        _orijinalTalepler = await _apiService.AdminAktifTalepleriGetirAsync();
        var markalar = await _apiService.MarkalariGetirAsync();

        if (_orijinalTalepler != null)
        {
            foreach (var talep in _orijinalTalepler)
            {
                // 1. Hizmet Adı Eşleştirme
                if (_tumHizmetler != null)
                {
                    var h = _tumHizmetler.FirstOrDefault(x => x.id == talep.hizmet_id);
                    if (h != null) talep.hizmet_adi = h.ad;
                }

                // 2. Araç Bilgilerini Detaylandırma (Kritik Blok Geri Eklendi)
                var arac = await _apiService.AracGetirAsync(talep.arac_id);
                if (arac != null)
                {
                    string gosterimAd = "";
                    if (arac.marka_id != null && arac.model_id != null && markalar != null)
                    {
                        var marka = markalar.FirstOrDefault(m => m.id == arac.marka_id);
                        if (marka != null)
                        {
                            var model = marka.modeller?.FirstOrDefault(m => m.id == arac.model_id);
                            if (model != null) gosterimAd = $"{marka.ad} {model.ad}";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(gosterimAd) && !string.IsNullOrWhiteSpace(arac.ozel_marka))
                    {
                        gosterimAd = $"{arac.ozel_marka} {arac.ozel_model}";
                    }

                    talep.arac_adi_tam = string.IsNullOrWhiteSpace(gosterimAd) ? $"Araç ID: {arac.id}" : gosterimAd;
                }
                else
                {
                    talep.arac_adi_tam = "Sistemden Silinmiş Araç";
                }
            }

            // Veriler işlendikten sonra filtreyi uygula ve listeyi yapılandır
            FiltreleriUygula();
        }
    }

    // =========================================================
    // FİLTRELEME SİSTEMİ
    // =========================================================

    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        FiltreleriUygula();
    }

    private void FiltreleriUygula()
    {
        if (_orijinalTalepler == null) return;

        var filtrelenmisListe = _orijinalTalepler.AsEnumerable();

        // 1. ARAMA FİLTRESİ
        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            var metin = AramaBar.Text;
            filtrelenmisListe = filtrelenmisListe.Where(t =>
                (t.kullanici_ad_soyad != null && t.kullanici_ad_soyad.Contains(metin, StringComparison.OrdinalIgnoreCase)) ||
                (t.arac_adi_tam != null && t.arac_adi_tam.Contains(metin, StringComparison.OrdinalIgnoreCase))
            );
        }

        // 2. DURUM FİLTRESİ
        if (_secilenDurum != "Tümü")
        {
            filtrelenmisListe = filtrelenmisListe.Where(t => t.durum == _secilenDurum);
        }

        // Arama Barı Filtresi
        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            var kelime = AramaBar.Text.ToLower();
            filtrelenmisListe = filtrelenmisListe.Where(t =>
                (t.kullanici_ad_soyad != null && t.kullanici_ad_soyad.ToLower().Contains(kelime)) ||
                (t.arac_adi_tam != null && t.arac_adi_tam.ToLower().Contains(kelime))
            );
        }

        // 3. EFSANE SIRALAMA MANTIĞI (Geri Geldi!)
        // Önce duruma göre aciliyet sırası, sonra eskiden yeniye (ID sırası en güvenli tarih sırasıdır)
        filtrelenmisListe = filtrelenmisListe
            .OrderBy(t => t.durum switch
            {
                "Bekliyor" => 1,
                "Onaylandı" => 2,
                "İşlemde" => 3,
                "Tamamlandı" => 4,
                "İptal Edildi" => 5,
                _ => 6
            })
            .ThenBy(t => t.id); // Aynı durumdaki talepleri en eskiden (ilk eklenen) en yeniye doğru sıralar
        RequestsList.ItemsSource = filtrelenmisListe.ToList();
    }

    // =========================================================
    // ÜST TARAF FİLTRE DROPDOWN KONTROLLERİ
    // =========================================================

    private void OnFiltreDurumKutusuAcKapat(object sender, EventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
    }

    private void OnFiltreDurumSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            _secilenDurum = secilen;
            SecilenDurumButonu.Text = secilen;
            DurumSecimKutusu.IsVisible = false; // Kapat
            DurumListesi.SelectedItem = null;   // Seçimi sıfırla
            FiltreleriUygula();                 // Listeyi tazele
        }
    }

    // =========================================================
    // KART İÇİ DURUM SEÇİM KONTROLLERİ (MADDE 46)
    // =========================================================

    private void OnItemDurumKutusuAc(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var tiklananTalep = btn?.BindingContext as ServisTalebi;

        if (tiklananTalep != null)
        {
            // Aynı anda sadece bir kartın dropdown'ı açık kalsın
            if (_orijinalTalepler != null)
            {
                foreach (var talep in _orijinalTalepler)
                {
                    if (talep != tiklananTalep && talep.DropdownAcikMi)
                    {
                        talep.DropdownAcikMi = false;
                    }
                }
            }
            // Tıklananı tersine çevir
            tiklananTalep.DropdownAcikMi = !tiklananTalep.DropdownAcikMi;
        }
    }

    private void OnItemDurumSecildi(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn != null)
        {
            var yeniDurum = btn.Text;
            var secilenTalep = btn.BindingContext as ServisTalebi;

            if (secilenTalep != null)
            {
                secilenTalep.durum = yeniDurum;
                secilenTalep.DropdownAcikMi = false; // Seçim sonrası kapat
            }
        }
    }

    // =========================================================
    // GÜNCELLEME İŞLEMİ
    // =========================================================

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var talep = button?.CommandParameter as ServisTalebi;

        if (talep != null)
        {
            // API üzerinden güncelleme isteği yapılandırılır
            bool basarili = await _apiService.AdminTalepGuncelleAsync(talep.id, talep.durum, talep.tahmini_tutar);

            if (basarili)
            {
                await DisplayAlert("Başarılı", "Talep başarıyla güncellendi.", "Tamam");
                await VerileriYukle(); // Listeyi son haliyle tazele
            }
            else
            {
                await DisplayAlert("Hata", "Güncellenirken bir sorun oluştu, lütfen tekrar deneyin.", "Tamam");
            }
        }
    }
}