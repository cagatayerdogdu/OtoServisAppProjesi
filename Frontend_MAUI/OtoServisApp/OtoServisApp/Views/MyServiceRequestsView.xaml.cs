using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class MyServiceRequestsView : ContentPage
{
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<Marka> _tumMarkalar;
    private List<ServisTalebi> _orijinalTalepler;

    // Filtre Değişkenleri
    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde", "Tamamlandı", "İptal Edildi" };
    private string _secilenDurum = "Tümü";

    public MyServiceRequestsView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. AŞAMA: Kullanıcıya donma hissi vermemek için Loading ekranını anında aç
        LoadingOverlay.IsVisible = true;

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek ve Loading animasyonunu başlatması için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (20ms) bekleyip thread'i rahatlatıyoruz.
        await Task.Delay(20);

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
        _tumMarkalar = await _apiService.MarkalariGetirAsync();

        _orijinalTalepler = await _apiService.ServisTalepleriniGetirAsync(_aktifKullanici.id);

        if (_orijinalTalepler != null)
        {
            foreach (var talep in _orijinalTalepler)
            {
                var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
                if (hizmet != null) talep.hizmet_adi = hizmet.ad;
                // Aracın Aktifler (A) listesinde olup olmadığına bak
                var aracAktif = _aktifKullanici.araclar?.FirstOrDefault(a => a.id == talep.arac_id);

                // Eğer araç listede yoksa (Yani Soft Delete 'X' yapılmışsa) API'den geçmiş kaydını bul!
                if (aracAktif == null)
                {
                    aracAktif = await _apiService.AracGetirAsync(talep.arac_id);
                }

                var arac = await _apiService.AracGetirAsync(talep.arac_id);
                if (arac != null)
                {
                    string gosterimAd = "";
                    if (arac.marka_id != null && arac.model_id != null && _tumMarkalar != null)
                    {
                        var marka = _tumMarkalar.FirstOrDefault(m => m.id == arac.marka_id);
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
                    talep.arac_adi = string.IsNullOrWhiteSpace(gosterimAd) ? $"Araç ID: {arac.id}" : gosterimAd;
                }

                // YENİ REVİZE: Talebe ait fotoğraf var mı kontrolü
                var fotolar = await _apiService.TalepFotograflariniGetirAsync(talep.id);
                talep.foto_var_mi = fotolar != null && fotolar.Count > 0;
            }
            FiltreleriUygula();
        }
    }

    private void OnFiltreAcKapat(object sender, EventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
    }

    // Arama barı tetikleyicisi
    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        FiltreleriUygula();
    }

    private void OnFiltreSecildi(object sender, SelectionChangedEventArgs e)
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

    private void FiltreleriUygula()
    {
        if (_orijinalTalepler == null) return;

        var filtrelenmisListe = _orijinalTalepler.AsEnumerable();

        if (_secilenDurum != "Tümü")
        {
            filtrelenmisListe = filtrelenmisListe.Where(t => t.durum == _secilenDurum);
        }

        // YENİ EKLENEN ARAMA KONTROLÜ
        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            var kelime = AramaBar.Text.ToLower();
            filtrelenmisListe = filtrelenmisListe.Where(t =>
                (t.hizmet_adi != null && t.hizmet_adi.ToLower().Contains(kelime)) ||
                (t.arac_adi != null && t.arac_adi.ToLower().Contains(kelime))
            );
        }

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
            .ThenByDescending(t => t.id);

        // YENİ KURAL (Madde 16): Talepleri ID'ye (veya tarihe) göre en yeniden en eskiye sırala
        RequestsList.ItemsSource = null;
        RequestsList.ItemsSource = filtrelenmisListe.ToList();
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            if (secilenTalep.durum == "Tamamlandı" || secilenTalep.durum == "İptal Edildi")
            {
                await DisplayAlert("İşlem Engellendi", "Bu talep sonlandığı için üzerinde değişiklik yapılamaz.", "Tamam");
                return;
            }
            await Navigation.PushAsync(new EditServiceRequestView(secilenTalep, _aktifKullanici));
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            // YENİ KURAL: Sadece Bekliyor olanlar iptal edilebilir										
            if (secilenTalep.durum != "Bekliyor")
            {
                await DisplayAlert("İşlem Engellendi", "Sadece 'Bekliyor' durumundaki talepler iptal edilebilir.", "Tamam");
                return;
            }

            bool eminMisin = await DisplayAlert("Onay", "Bu servis talebini iptal etmek (silmek) istediğinize emin misiniz?", "Evet, İptal Et", "Vazgeç");
            if (eminMisin)
            {

                // İşlem başlıyor, ekranı kilitle ve Loading'i göster
                LoadingTitle.Text = "Lütfen bekleyiniz...";
                LoadingOverlay.IsVisible = true;
                await Task.Delay(20); // UI çizimi için nefes aldır

                bool basarili = await _apiService.ServisTalebiSilAsync(secilenTalep.id);

                try
                {
                    if (basarili)
                    {
                        await DisplayAlert("Başarılı", "Talebiniz iptal edildi.", "Tamam");

                        // Listeyi yeniden yükle (Artık donmayacak çünkü Loading çalışıyor)
                        await VerileriYukle();
                        // Artık güncelledikten sonra liste asla kaymayacak, sen sayfadan çıkana kadar orada kalacak. Kapamıştım geri açtık.
                    }
                    else
                    {
                        await DisplayAlert("Hata", "Talebiniz iptal edilirken bir sorun oluştu.", "Tamam");
                    }
                }
                finally
                {
                    // İşlem bitti, ekranı serbest bırak
                    LoadingOverlay.IsVisible = false;
                    LoadingTitle.Text = "Veriler Yükleniyor..."; // Sonraki kullanımlar için varsayılana çevir
                }
            }
        }
    }

    // YENİ REVİZE: Fotoğrafları Gör Butonu Tıklanma Olayı
    private async void OnViewPhotosClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            await Navigation.PushAsync(new ViewPhotosView(secilenTalep));
        }
    }
}