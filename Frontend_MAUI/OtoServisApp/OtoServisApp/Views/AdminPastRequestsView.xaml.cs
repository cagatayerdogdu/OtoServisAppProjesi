using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminPastRequestsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<ServisTalebi> _orijinalTalepler;

    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Tamamlandı", "İptal Edildi" };
    private string _secilenDurum = "Tümü";

    public AdminPastRequestsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. AŞAMA: Kullanıcıya donma hissi vermemek için Loading ekranını anında aç
        LoadingOverlay.IsVisible = true;

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek ve Loading animasyonunu başlatması için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (20ms) bekleyip thread'i rahatlatıyoruz.
        await Task.Delay(1);

        try
        {
            // Filtre dropdown listelerini vs. burada doldurabilirsin
            if (DurumListesi != null && DurumListesi.ItemsSource == null)
                DurumListesi.ItemsSource = _durumFiltreleri;

            // 3. AŞAMA: Asıl veriyi (API İsteklerini) şimdi çekiyoruz
            await VerileriYukle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Veriler yüklenirken bir sorun oluştu.", "Tamam");
            System.Diagnostics.Debug.WriteLine($"Yükleme Hatası: {ex.Message}");
        }
        finally
        {
            // 4. AŞAMA: Veri gelse de, hata da verse Loading ekranını KESİNLİKLE kapat
            LoadingOverlay.IsVisible = false;
        }
    }

    private async Task VerileriYukle()
    {
        _tumHizmetler = await _apiService.HizmetleriGetirAsync();
        _orijinalTalepler = await _apiService.AdminGecmisTalepleriGetirAsync();

        if (_orijinalTalepler != null)
        {
            foreach (var talep in _orijinalTalepler)
            {
                var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
                if (hizmet != null) talep.hizmet_adi = hizmet.ad;
            }
            FiltreleriUygula();
        }
    }

    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        FiltreleriUygula();
    }

    private void FiltreleriUygula()
    {
        if (_orijinalTalepler == null) return;

        var filtrelenmisListe = _orijinalTalepler.AsEnumerable();

        if (_secilenDurum != "Tümü")
        {
            filtrelenmisListe = filtrelenmisListe.Where(t => t.durum == _secilenDurum);
        }

        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            var kelime = AramaBar.Text.ToLower();
            filtrelenmisListe = filtrelenmisListe.Where(t =>
                (t.kullanici_ad_soyad != null && t.kullanici_ad_soyad.ToLower().Contains(kelime)) ||
                (t.arac_adi_tam != null && t.arac_adi_tam.ToLower().Contains(kelime)) ||
                (t.hizmet_adi != null && t.hizmet_adi.ToLower().Contains(kelime))
            );
        }

        PastRequestsList.ItemsSource = null;
        PastRequestsList.ItemsSource = filtrelenmisListe.ToList();
    }

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
            DurumSecimKutusu.IsVisible = false;
            DurumListesi.SelectedItem = null;
            FiltreleriUygula();
        }
    }
}