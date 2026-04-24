using System.Collections.Concurrent;
using System.Diagnostics;
using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class MyServiceView : ContentPage
{
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<Marka> _tumMarkalar;
    private List<ServisTalebi> _orijinalTalepler;

    // Sayfalama için parametrik değer
    private int _sayfaBoyutu = 15;
    private int _mevcutSayfa = 1;
    private int _toplamSayfa = 1;
    private int _toplamKayit = 0;
    private bool _yukleniyor = false;
    private bool _ilkYukleme = true;

    private string _aktifDurum = "Tümü";
    private string _aktifArama = "";

    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde", "Tamamlandı", "İptal Edildi" };

    public MyServiceView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();

        GuncelleButonDurumlari();

        /* Çalışmadı.
        // Talep güncelleme/işlem mesajını dinle
        MessagingCenter.Subscribe<object>(this, "TalepGuncellendi", async (sender) =>
        {
            // Mevcut sayfayı koruyarak listeyi yenile (loading göstermeden)
            await TalepleriYukle(_mevcutSayfa);
        });
        */
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_ilkYukleme)
        {
            // İlk yükleme: loading overlay ile çek
            //_ilkYukleme = false;
            LoadingOverlay.IsVisible = true;
            LoadingTitle.Text = "Talepler Yükleniyor...";
            await Task.Delay(5);

            try
            {
                if (DurumListesi != null && DurumListesi.ItemsSource == null)
                    DurumListesi.ItemsSource = _durumFiltreleri;

                await TalepleriYukle(sayfa: 1);
            }
            catch (Exception ex)
            {
                await ModernAlertService.ShowInfoAsync("Talep verileri yüklenirken bir sorun oluştu.", "Hata");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }
        /*else
        {
            // Geri dönüldüğünde SESSİZCE yenile (loading gösterme)
            await TalepleriYukle(_mevcutSayfa, silent: true);
        }*/
    }

    private async Task TalepleriYukle(int sayfa/*, bool silent = false*/)
    {
        if (_yukleniyor) return;
        _yukleniyor = true;

        int skip = (sayfa - 1) * _sayfaBoyutu;

        try
        {
            if (_ilkYukleme)
            {
                _tumHizmetler = await _apiService.HizmetleriGetirAsync();
                _tumMarkalar = await _apiService.MarkalariGetirAsync();
                _ilkYukleme = false;
            }

            var (yeniTalepler, toplamKayit) = await _apiService.KullaniciTalepleriniSayfaliGetirAsync(
                _aktifKullanici.id,
                skip: skip,
                limit: _sayfaBoyutu,
                durum: _aktifDurum,
                arama: _aktifArama
            );

            _toplamKayit = toplamKayit;
            _toplamSayfa = (int)Math.Ceiling((double)toplamKayit / _sayfaBoyutu);
            if (_toplamSayfa == 0) _toplamSayfa = 1;
            _mevcutSayfa = sayfa;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ToplamTalepLabel.Text = $"{toplamKayit} talep";
                SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
                GuncelleButonDurumlari();
            });

            if (yeniTalepler != null && yeniTalepler.Any())
            {
                _orijinalTalepler = new List<ServisTalebi>(yeniTalepler);
                await TalepleriZenginlestir(_orijinalTalepler);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RequestsList.ItemsSource = _orijinalTalepler.ToList();
                });
            }
            else
            {
                _orijinalTalepler?.Clear();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RequestsList.ItemsSource = null;
                });
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Veriler yüklenirken bir sorun oluştu.", "Hata");
            Debug.WriteLine($"Yükleme hatası: {ex.Message}");
        }
        finally
        {
            _yukleniyor = false;
        }
    }

    private async Task TalepleriZenginlestir(List<ServisTalebi> talepler)
    {
        if (talepler == null || !talepler.Any()) return;

        var aracHavuzu = new ConcurrentDictionary<int, Arac>(
            _aktifKullanici.araclar?.ToDictionary(a => a.id) ?? new Dictionary<int, Arac>()
        );

        var gorevler = talepler.Select(async talep =>
        {
            var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
            if (hizmet != null) talep.hizmet_adi = hizmet.ad;

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

        // 📸 TOPLU FOTOĞRAF DURUMU SORGUSU (EKLENDİ)
        var talepIdleri = talepler.Select(t => t.id).ToList();
        var fotoDurumlari = await _apiService.TopluFotografDurumuGetirAsync(talepIdleri);
        foreach (var talep in talepler)
        {
            talep.foto_var_mi = fotoDurumlari.TryGetValue(talep.id, out var varMi) && varMi;
        }
    }

    private void GuncelleButonDurumlari()
    {
        BtnOncekiLabel.Opacity = _mevcutSayfa > 1 ? 1.0 : 0.5;
        BtnSonrakiLabel.Opacity = _mevcutSayfa < _toplamSayfa ? 1.0 : 0.5;
    }

    private async void OnOncekiTapped(object sender, TappedEventArgs e)
    {
        if (_yukleniyor) return;
        if (_mevcutSayfa > 1)
        {
            await TalepleriYukle(_mevcutSayfa - 1);
        }
    }

    private async void OnSonrakiTapped(object sender, TappedEventArgs e)
    {
        if (_yukleniyor) return;
        if (_mevcutSayfa < _toplamSayfa)
        {
            await TalepleriYukle(_mevcutSayfa + 1);
        }
    }

    private void OnFiltreAcKapatTapped(object sender, TappedEventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
    }

    private CancellationTokenSource _aramaCts;
    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        _aramaCts?.Cancel();
        _aramaCts = new CancellationTokenSource();

        Task.Delay(300, _aramaCts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                _aktifArama = AramaBar.Text;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await TalepleriYukle(sayfa: 1);
                });
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MessagingCenter.Unsubscribe<object>(this, "TalepGuncellendi");
        _aramaCts?.Cancel();
    }

    private void OnFiltreSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            _aktifDurum = secilen;
            SecilenDurumLabel.Text = secilen;
            DurumSecimKutusu.IsVisible = false;
            DurumListesi.SelectedItem = null;

            _ = TalepleriYukle(sayfa: 1);
        }
    }

    private async void OnDetayTapped(object sender, TappedEventArgs e)
    {
        var secilenTalep = e.Parameter as ServisTalebi;
        if (secilenTalep != null)
        {
            await Navigation.PushAsync(new MyServiceRequestDetailView(secilenTalep, _aktifKullanici));
        }
    }
}