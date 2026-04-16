using System.Collections.Concurrent;
using System.Diagnostics;
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

    // DeepSeek - Lazy Loading + Server‑Side Filtreleme \\
    private const int SayfaBoyutu = 20;
    private int _mevcutSkip = 0;
    private bool _dahaFazlaVar = true;
    private bool _yukleniyor = false;
    private bool _ilkYukleme = true;

    // Filtreler
    private string _aktifDurum = "Tümü";
    private string _aktifArama = "";

    // Yeni revize bitişi \\

    // Filtre Değişkenleri
    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde", "Tamamlandı", "İptal Edildi" };
    private string _secilenDurum = "Tümü";

    public MyServiceRequestsView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();

        MessagingCenter.Subscribe<object>(this, "TalepGuncellendi", async (sender) =>
        {
            await TalepleriYukle(reset: true);
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_ilkYukleme)
        {
            LoadingOverlay.IsVisible = true;
            LoadingTitle.Text = "Talepler Yükleniyor...";
            await Task.Delay(5);

            try
            {
                if (DurumListesi != null && DurumListesi.ItemsSource == null)
                    DurumListesi.ItemsSource = _durumFiltreleri;

                await TalepleriYukle(reset: true);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Talep verileri yüklenirken bir sorun oluştu.", "Tamam");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }
        else
        {
            // Sayfa zaten yüklenmiş, sadece mevcut listeyi yeniden bağla (edge swipe dönüşleri için)
            if (_orijinalTalepler != null && _orijinalTalepler.Any())
            {
                RequestsList.ItemsSource = _orijinalTalepler.ToList();
            }
        }
    }

    private async Task TalepleriYukle(bool reset = true)
    {
        if (_yukleniyor) return;
        if (!reset && !_dahaFazlaVar) return;

        _yukleniyor = true;

        if (reset)
        {
            _mevcutSkip = 0;
            _dahaFazlaVar = true;
            _orijinalTalepler?.Clear();
            // UI'da hemen boşaltmak isterseniz:
            // RequestsList.ItemsSource = null;
        }

        // İlk yüklemede loading overlay göster
        if (_ilkYukleme)
        {
            LoadingOverlay.IsVisible = true;
            LoadingTitle.Text = "Talepler Yükleniyor...";
        }

        try
        {
            // İlk yüklemede referans verileri de çek
            if (_ilkYukleme)
            {
                _tumHizmetler = await _apiService.HizmetleriGetirAsync();
                _tumMarkalar = await _apiService.MarkalariGetirAsync();
                _ilkYukleme = false;
            }

            var (yeniTalepler, toplamKayit) = await _apiService.KullaniciTalepleriniSayfaliGetirAsync(
                _aktifKullanici.id,
                skip: _mevcutSkip,
                limit: SayfaBoyutu,
                durum: _aktifDurum,
                arama: _aktifArama
            );

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ToplamTalepLabel.Text = $"{toplamKayit} talep";
            });

            if (yeniTalepler != null && yeniTalepler.Any())
            {
                if (_orijinalTalepler == null)
                    _orijinalTalepler = new List<ServisTalebi>();

                // Gelen talepleri zenginleştir (araç adı, hizmet adı, foto durumu)
                await TalepleriZenginlestir(yeniTalepler);

                _orijinalTalepler.AddRange(yeniTalepler);
                _mevcutSkip += yeniTalepler.Count;

                // Toplam kayıt sayısına göre daha fazla var mı kontrol et
                _dahaFazlaVar = _orijinalTalepler.Count < toplamKayit;
            }
            else
            {
                _dahaFazlaVar = false;
            }

            // UI'ı güncelle (filtreleme yok, direkt listeyi bağla)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RequestsList.ItemsSource = _orijinalTalepler?.ToList();
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Veriler yüklenirken bir sorun oluştu.", "Tamam");
            Debug.WriteLine($"Yükleme hatası: {ex.Message}");
        }
        finally
        {
            _yukleniyor = false;
            LoadingOverlay.IsVisible = false;
        }
    }

    private async Task TalepleriZenginlestir(List<ServisTalebi> talepler)
    {
        if (talepler == null || !talepler.Any()) return;

        // Araç havuzu (kullanıcının araçları + yeni çekilenler)
        var aracHavuzu = new ConcurrentDictionary<int, Arac>(
            _aktifKullanici.araclar?.ToDictionary(a => a.id) ?? new Dictionary<int, Arac>()
        );

        var gorevler = talepler.Select(async talep =>
        {
            // Hizmet adı
            var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
            if (hizmet != null) talep.hizmet_adi = hizmet.ad;

            // Araç bilgisi
            if (!aracHavuzu.TryGetValue(talep.arac_id, out var arac))
            {
                arac = await _apiService.AracGetirAsync(talep.arac_id);
                if (arac != null) aracHavuzu.TryAdd(talep.arac_id, arac);
            }

            if (arac != null)
            {
                string gosterimAd = "";
                if (arac.marka_id != null && arac.model_id != null && _tumMarkalar != null)
                {
                    var marka = _tumMarkalar.FirstOrDefault(m => m.id == arac.marka_id);
                    var model = marka?.modeller?.FirstOrDefault(m => m.id == arac.model_id);
                    if (marka != null && model != null) gosterimAd = $"{marka.ad} {model.ad}";
                }
                if (string.IsNullOrWhiteSpace(gosterimAd) && !string.IsNullOrWhiteSpace(arac.ozel_marka))
                    gosterimAd = $"{arac.ozel_marka} {arac.ozel_model}";
                talep.arac_adi = string.IsNullOrWhiteSpace(gosterimAd) ? $"Araç ID: {arac.id}" : gosterimAd;
            }
        });

        await Task.WhenAll(gorevler);

        // Toplu fotoğraf durumu
        var talepIdleri = talepler.Select(t => t.id).ToList();
        var fotoDurumlari = await _apiService.TopluFotografDurumuGetirAsync(talepIdleri);
        foreach (var talep in talepler)
        {
            talep.foto_var_mi = fotoDurumlari.TryGetValue(talep.id, out var varMi) && varMi;
        }
    }

    /*private async Task VerileriYukle()
    {
        // 1. Temel verileri çek
        _tumHizmetler = await _apiService.HizmetleriGetirAsync();
        _tumMarkalar = await _apiService.MarkalariGetirAsync();
        _orijinalTalepler = await _apiService.ServisTalepleriniGetirAsync(_aktifKullanici.id);

        if (_orijinalTalepler != null && _orijinalTalepler.Count > 0)
        {
            // 2. PARALEL İŞLEM İÇİN HAZIRLIK: Araçları hafızaya (RAM) alıyoruz.
            // ConcurrentDictionary kullanıyoruz çünkü birden fazla işlem aynı anda buraya yazmaya çalışacak.
            // Araç havuzu ve paralel işlemler (fotoğraf kontrolü hariç)														 
            var aracHavuzu = new ConcurrentDictionary<int, Arac>(
                _aktifKullanici.araclar?.ToDictionary(a => a.id) ?? new Dictionary<int, Arac>()
            );

            // 3. İŞLEMLERİ AYNI ANDA BAŞLAT (N+1 Probleminin Çözümü)											 
            var gorevler = _orijinalTalepler.Select(async talep =>
            {
                // Hizmet adı
                var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
                if (hizmet != null) talep.hizmet_adi = hizmet.ad;

                // Araç Bilgisini Getir (Sadece hafızada yoksa API'ye git)
                if (!aracHavuzu.TryGetValue(talep.arac_id, out var arac))
                {
                    arac = await _apiService.AracGetirAsync(talep.arac_id);
                    if (arac != null)
                        aracHavuzu.TryAdd(talep.arac_id, arac); // Bulduğumuz aracı havuza ekle ki bir daha çekmeyelim
                }

                // Aracın gösterim adını ayarla				   
                if (arac != null)
                {
                    string gosterimAd = "";
                    if (arac.marka_id != null && arac.model_id != null && _tumMarkalar != null)
                    {
                        var marka = _tumMarkalar.FirstOrDefault(m => m.id == arac.marka_id);
                        var model = marka?.modeller?.FirstOrDefault(m => m.id == arac.model_id);
                        if (marka != null && model != null) gosterimAd = $"{marka.ad} {model.ad}";
                    }

                    if (string.IsNullOrWhiteSpace(gosterimAd) && !string.IsNullOrWhiteSpace(arac.ozel_marka))
                    {
                        gosterimAd = $"{arac.ozel_marka} {arac.ozel_model}";
                    }
                    talep.arac_adi = string.IsNullOrWhiteSpace(gosterimAd) ? $"Araç ID: {arac.id}" : gosterimAd;
                }

                // Talebe ait fotoğraf var mı kontrolü (Aynı anda çalışır, sistemi bekletmez)
                //var fotolar = await _apiService.TalepFotograflariniGetirAsync(talep.id);
                //talep.foto_var_mi = fotolar != null && fotolar.Count > 0;
            });

            // Başlatılan tüm paralel görevlerin bitmesini bekle (Saniyeler süren işlemi milisaniyelere indirir)
            await Task.WhenAll(gorevler);

            // 4. Fotoğraf durumlarını TEK SEFERDE toplu olarak al
            var talepIdleri = _orijinalTalepler.Select(t => t.id).ToList();
            var fotoDurumlari = await _apiService.TopluFotografDurumuGetirAsync(talepIdleri);

            foreach (var talep in _orijinalTalepler)
            {
                talep.foto_var_mi = fotoDurumlari.TryGetValue(talep.id, out var varMi) && varMi;
            }

            // 4. Filtreleri uygula
            FiltreleriUygula();
        }
        else
        {
            RequestsList.ItemsSource = null;
        }
    }*/

    /*private void OnFiltreAcKapat(object sender, EventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
    }*/

    private void OnFiltreAcKapatTapped(object sender, TappedEventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
    }


    // Arama barı tetikleyicisi
    /*private CancellationTokenSource _aramaCts;
    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        _aramaCts?.Cancel();
        _aramaCts = new CancellationTokenSource();

        Task.Delay(100, _aramaCts.Token)
            .ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    MainThread.BeginInvokeOnMainThread(FiltreleriUygula);
            });
    }*/

    private CancellationTokenSource _aramaCts;
    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        _aramaCts?.Cancel();
        _aramaCts = new CancellationTokenSource();

        Task.Delay(100, _aramaCts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                _aktifArama = AramaBar.Text;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await TalepleriYukle(reset: true);
                });
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _aramaCts?.Cancel();
        //RequestsList.ItemsSource = null;
        //edge swipe ile geri dönüldüğünde listenin boş kalmasına neden olabileceği için kaldırıldı.
    }

    /*private void OnFiltreSecildi(object sender, SelectionChangedEventArgs e)
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
    }*/

    private void OnFiltreSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            _aktifDurum = secilen;
            //SecilenDurumButonu.Text = secilen;
            SecilenDurumLabel.Text = secilen;
            DurumSecimKutusu.IsVisible = false;
            DurumListesi.SelectedItem = null;

            // Filtre değişti, sıfırdan yükle
            _ = TalepleriYukle(reset: true);
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

        //RequestsList.ItemsSource = null;
        RequestsList.ItemsSource = filtrelenmisListe.ToList();
    }

    /*private async void OnEditClicked(object sender, EventArgs e)
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
    }*/

    /*private async void OnCancelClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            if (secilenTalep.durum != "Bekliyor")
            {
                await DisplayAlert("İşlem Engellendi", "Sadece 'Bekliyor' durumundaki talepler iptal edilebilir.", "Tamam");
                return;
            }

            bool eminMisin = await DisplayAlert("Onay", "Bu servis talebini iptal etmek (silmek) istediğinize emin misiniz?", "Evet, İptal Et", "Vazgeç");
            if (eminMisin)
            {
                LoadingTitle.Text = "Lütfen bekleyiniz...";
                LoadingOverlay.IsVisible = true;
                await Task.Delay(10);

                try
                {
                    bool basarili = await _apiService.ServisTalebiSilAsync(secilenTalep.id);
                    if (basarili)
                    {
                        await DisplayAlert("Başarılı", "Talebiniz iptal edildi.", "Tamam");
                        //await VerileriYukle();
                        await TalepleriYukle(reset: true);
                    }
                    else
                    {
                        await DisplayAlert("Hata", "Talebiniz iptal edilirken bir sorun oluştu.", "Tamam");
                    }
                }
                finally
                {
                    LoadingOverlay.IsVisible = false;
                    LoadingTitle.Text = "Talepler Yükleniyor..."; // Yazıyı geri eski haline alıyoruz
                }
            }
        }
    }*/

    /*private async void OnViewPhotosClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            await Navigation.PushAsync(new ViewPhotosView(secilenTalep));
        }
    }*/

    private async void OnViewPhotosTapped(object sender, TappedEventArgs e)
    {
        var label = sender as Label;
        var secilenTalep = label?.BindingContext as ServisTalebi;
        if (secilenTalep != null)
            await Navigation.PushAsync(new ViewPhotosView(secilenTalep));
    }

    private async void OnEditTapped(object sender, TappedEventArgs e)
    {
        var label = sender as Label;
        var secilenTalep = label?.BindingContext as ServisTalebi;

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

    private async void OnCancelTapped(object sender, TappedEventArgs e)
    {
        var label = sender as Label;
        var secilenTalep = label?.BindingContext as ServisTalebi;

        if (secilenTalep != null)
        {
            if (secilenTalep.durum != "Bekliyor")
            {
                await DisplayAlert("İşlem Engellendi", "Sadece 'Bekliyor' durumundaki talepler iptal edilebilir.", "Tamam");
                return;
            }

            bool eminMisin = await DisplayAlert("Onay", "Bu servis talebini iptal etmek (silmek) istediğinize emin misiniz?", "Evet, İptal Et", "Vazgeç");
            if (eminMisin)
            {
                LoadingTitle.Text = "Lütfen bekleyiniz...";
                LoadingOverlay.IsVisible = true;
                await Task.Delay(10);

                try
                {
                    bool basarili = await _apiService.ServisTalebiSilAsync(secilenTalep.id);
                    if (basarili)
                    {
                        await DisplayAlert("Başarılı", "Talebiniz iptal edildi.", "Tamam");
                        //await VerileriYukle();
                        await TalepleriYukle(reset: true);
                    }
                    else
                    {
                        await DisplayAlert("Hata", "Talebiniz iptal edilirken bir sorun oluştu.", "Tamam");
                    }
                }
                finally
                {
                    LoadingOverlay.IsVisible = false;
                    LoadingTitle.Text = "Talepler Yükleniyor..."; // Yazıyı geri eski haline alıyoruz
                }
            }
        }
    }

    private async void OnThresholdReached(object sender, EventArgs e)
    {
        if (!_yukleniyor && _dahaFazlaVar)
        {
            await TalepleriYukle(reset: false);
        }
    }
}