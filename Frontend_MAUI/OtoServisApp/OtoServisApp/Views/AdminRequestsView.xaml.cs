using System.Collections.Concurrent;
using OtoServisApp.Models;
using OtoServisApp.Services;

#if IOS
using UIKit;
using CoreGraphics;
#elif ANDROID
using Android.Views;
using Android.Graphics;
#endif  

namespace OtoServisApp.Views;

public partial class AdminRequestsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<Marka> _tumMarkalar;
    private List<ServisTalebi> _orijinalTalepler;
    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde" };
    private string _secilenDurum = "Tümü";

    // Açık olan kartın referansını tutar (yüzen menü için)
    private ServisTalebi _secilenTalep;

    // Lazy loading değişkenleri
    private const int SayfaBoyutu = 20;
    private int _mevcutSkip = 0;
    private bool _dahaFazlaVar = true;
    private bool _yukleniyor = false;
    private bool _ilkYukleme = true;
    private string _aktifArama = "";

    // Arama debounce için
    private CancellationTokenSource _aramaCts;

    public AdminRequestsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_ilkYukleme)
        {
            LoadingOverlay.IsVisible = true;
            LoadingTitle.Text = "Talepler Yükleniyor...";
            LoadingSubText.Text = "Lütfen bekleyiniz.";
            await Task.Delay(5);
        }

        try
        {
            if (DurumListesi != null && DurumListesi.ItemsSource == null)
                DurumListesi.ItemsSource = _durumFiltreleri;

            if (_ilkYukleme)
                await TalepleriYukle(reset: true);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Veriler yüklenirken bir sorun oluştu.", "Tamam");
            System.Diagnostics.Debug.WriteLine($"Yükleme Hatası: {ex.Message}");
        }
        finally
        {
            if (_ilkYukleme)
                LoadingOverlay.IsVisible = false;
        }
    }

    /// <summary>
    /// Talepleri sayfalı olarak yükler. reset=true ise listeyi sıfırlar.
    /// </summary>
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
        }

        if (_ilkYukleme)
        {
            LoadingOverlay.IsVisible = true;
            LoadingTitle.Text = "Talepler Yükleniyor...";
        }

        try
        {
            // İlk yüklemede referans verileri bir kez al
            if (_ilkYukleme)
            {
                _tumHizmetler = await _apiService.HizmetleriGetirAsync();
                _tumMarkalar = await _apiService.MarkalariGetirAsync();
                _ilkYukleme = false;
            }

            // Sayfalı admin talepleri endpoint'ini kullan
            var (yeniTalepler, toplamKayit) = await _apiService.AdminTalepleriniSayfaliGetirAsync(
                skip: _mevcutSkip,
                limit: SayfaBoyutu,
                durum: _secilenDurum,
                arama: _aktifArama
            );

            if (yeniTalepler != null && yeniTalepler.Any())
            {
                if (_orijinalTalepler == null)
                    _orijinalTalepler = new List<ServisTalebi>();

                // Gelen talepleri zenginleştir (araç adı, hizmet adı, foto durumu)
                await TalepleriZenginlestir(yeniTalepler);

                _orijinalTalepler.AddRange(yeniTalepler);
                _mevcutSkip += yeniTalepler.Count;
                _dahaFazlaVar = _orijinalTalepler.Count < toplamKayit;
            }
            else
            {
                _dahaFazlaVar = false;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                RequestsList.ItemsSource = _orijinalTalepler?.ToList();
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Veriler yüklenirken bir sorun oluştu.", "Tamam");
            System.Diagnostics.Debug.WriteLine($"Admin talepler yükleme hatası: {ex.Message}");
        }
        finally
        {
            _yukleniyor = false;
            LoadingOverlay.IsVisible = false;
        }
    }

    /// <summary>
    /// Yeni yüklenen talepleri zenginleştirir: hizmet adı, araç adı ve fotoğraf var mı bilgisi ekler.
    /// </summary>
    private async Task TalepleriZenginlestir(List<ServisTalebi> talepler)
    {
        if (talepler == null || talepler.Count == 0) return;

        // Araç havuzu (cache) – aynı aracı tekrar API'den çekmemek için
        var aracHavuzu = new ConcurrentDictionary<int, Arac>();

        // Her talep için paralel olarak hizmet adı ve araç adı doldur
        var detayGorevleri = talepler.Select(async talep =>
        {
            // Hizmet adı (önceden alınan _tumHizmetler listesinden)
            var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
            if (hizmet != null)
                talep.hizmet_adi = hizmet.ad;

            // Araç bilgisi (cache'de yoksa API'den al)
            if (!aracHavuzu.TryGetValue(talep.arac_id, out var arac))
            {
                arac = await _apiService.AracGetirAsync(talep.arac_id);
                if (arac != null)
                    aracHavuzu.TryAdd(talep.arac_id, arac);
            }

            if (arac != null)
            {
                string gosterimAd = "";
                if (arac.marka_id != null && arac.model_id != null && _tumMarkalar != null)
                {
                    var marka = _tumMarkalar.FirstOrDefault(m => m.id == arac.marka_id);
                    var model = marka?.modeller?.FirstOrDefault(m => m.id == arac.model_id);
                    if (marka != null && model != null)
                        gosterimAd = $"{marka.ad} {model.ad}";
                }

                if (string.IsNullOrWhiteSpace(gosterimAd) && !string.IsNullOrWhiteSpace(arac.ozel_marka))
                    gosterimAd = $"{arac.ozel_marka} {arac.ozel_model}";

                talep.arac_adi_tam = string.IsNullOrWhiteSpace(gosterimAd) ? $"Araç ID: {arac.id}" : gosterimAd;
            }
            else
            {
                talep.arac_adi_tam = "Sistemden Silinmiş Araç";
            }
        });

        await Task.WhenAll(detayGorevleri);

        // Toplu fotoğraf durumu sorgusu (sadece bu sayfadaki talepler için)
        var talepIdleri = talepler.Select(t => t.id).ToList();
        var fotoDurumlari = await _apiService.TopluFotografDurumuGetirAsync(talepIdleri);

        foreach (var talep in talepler)
        {
            talep.foto_var_mi = fotoDurumlari.TryGetValue(talep.id, out var varMi) && varMi;
        }
    }

    /// <summary>
    /// Koleksiyon görünümü sona yaklaştığında tetiklenir, yeni sayfa yükler.
    /// </summary>
    private async void OnThresholdReached(object sender, EventArgs e)
    {
        if (!_yukleniyor && _dahaFazlaVar)
            await TalepleriYukle(reset: false);
    }

    /// <summary>
    /// Arama çubuğu değiştiğinde debounce ile filtreleme yapar.
    /// </summary>
    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        _aramaCts?.Cancel();
        _aramaCts = new CancellationTokenSource();

        Task.Delay(300, _aramaCts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                _aktifArama = AramaBar.Text;
                MainThread.BeginInvokeOnMainThread(async () => await TalepleriYukle(reset: true));
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _aramaCts?.Cancel();
        // ItemsSource = null YAPMIYORUZ, sayfa geri gelince liste boş kalmasın.
    }

    // =========================================================
    // ÜST FİLTRE DROPDOWN KONTROLLERİ
    // =========================================================

    private void OnFiltreDurumKutusuAcKapatTapped(object sender, TappedEventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
        FloatingMenuOverlay.IsVisible = false; // Yüzen menüyü kapat
    }

    private void OnFiltreDurumSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            _secilenDurum = secilen;
            SecilenDurumLabel.Text = secilen;
            DurumSecimKutusu.IsVisible = false;
            DurumListesi.SelectedItem = null;
            _ = TalepleriYukle(reset: true);
        }
    }

    // =========================================================
    // YÜZEN DURUM MENÜSÜ KONTROLLERİ
    // =========================================================

    private void OnFloatingMenuClose(object sender, EventArgs e)
    {
        FloatingMenuOverlay.IsVisible = false;
        _secilenTalep = null;
    }

    /// <summary>
    /// Kart içindeki "Mevcut Durum" alanına tıklandığında yüzen durum menüsünü açar.
    /// </summary>
    private void OnItemDurumKutusuAcTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var talep = e.Parameter as ServisTalebi;

        if (talep == null || border == null) return;

        DurumSecimKutusu.IsVisible = false;
        _secilenTalep = talep;
        FloatingMenuOverlay.IsVisible = true;

        double buton_X = 0;
        double buton_Y = 0;

#if IOS
        var iosBorder = border.Handler?.PlatformView as UIKit.UIView;
        var iosOverlay = FloatingMenuOverlay.Handler?.PlatformView as UIKit.UIView;
        if (iosBorder != null && iosOverlay != null)
        {
            var rect = iosBorder.ConvertRectToView(iosBorder.Bounds, iosOverlay);
            buton_X = rect.X;
            buton_Y = rect.Y + border.Height;
        }
#elif ANDROID
        var androidBorder = border.Handler?.PlatformView as Android.Views.View;
        var androidOverlay = FloatingMenuOverlay.Handler?.PlatformView as Android.Views.View;
        if (androidBorder != null && androidOverlay != null)
        {
            int[] locBorder = new int[2];
            androidBorder.GetLocationOnScreen(locBorder);

            int[] locOverlay = new int[2];
            androidOverlay.GetLocationOnScreen(locOverlay);

            double density = DeviceDisplay.MainDisplayInfo.Density;

            buton_X = (locBorder[0] - locOverlay[0]) / density;
            buton_Y = ((locBorder[1] - locOverlay[1]) / density) + border.Height;
        }
#endif

        AbsoluteLayout.SetLayoutBounds(FloatingItemDurumMenusu, new Microsoft.Maui.Graphics.Rect(buton_X, buton_Y, 130, 160));
    }

    /// <summary>
    /// Yüzen menüden bir durum seçildiğinde talebin durumunu günceller.
    /// </summary>
    private void OnFloatingItemDurumSecildi(object sender, TappedEventArgs e)
    {
        var yeniDurum = e.Parameter as string;
        if (_secilenTalep != null && !string.IsNullOrEmpty(yeniDurum))
        {
            _secilenTalep.durum = yeniDurum;
        }

        FloatingMenuOverlay.IsVisible = false;
        _secilenTalep = null;
    }

    // =========================================================
    // GÜNCELLEME İŞLEMİ
    // =========================================================

    private async void OnUpdateTapped(object sender, TappedEventArgs e)
    {
        var talep = e.Parameter as ServisTalebi;

        if (talep != null)
        {
            LoadingTitle.Text = "Güncelleniyor...";
            LoadingOverlay.IsVisible = true;
            await Task.Delay(5);

            string idStr = await SecureStorage.Default.GetAsync("kullanici_id_gizli");
            int? aktifAdminId = int.TryParse(idStr, out int id) ? id : (int?)null;

            bool basarili = await _apiService.AdminTalepGuncelleAsync(talep.id, talep.durum, talep.tahmini_tutar, aktifAdminId);

            try
            {
                if (basarili)
                {
                    await DisplayAlert("Başarılı", "Talep güncellendi.", "Tamam");
                    await TalepleriYukle(reset: true);
                }
                else
                {
                    await DisplayAlert("Hata", "Güncellenirken bir sorun oluştu.", "Tamam");
                }
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                LoadingTitle.Text = "Talepler Yükleniyor...";
            }
        }
    }

    // =========================================================
    // ADRES KOPYALAMA
    // =========================================================

    private async void OnCopyTapped(object sender, EventArgs e)
    {
        var label = sender as Label;
        var gesture = label?.GestureRecognizers.FirstOrDefault() as TapGestureRecognizer;
        var kopyalanacakMetin = gesture?.CommandParameter as string;

        if (!string.IsNullOrWhiteSpace(kopyalanacakMetin))
        {
            await Clipboard.Default.SetTextAsync(kopyalanacakMetin);
            await DisplayAlert("Kopyalandı", "Bilgi panoya kopyalandı.", "Tamam");
        }
    }

    // =========================================================
    // FOTOĞRAF İŞLEMLERİ
    // =========================================================

    private async void OnViewPhotosTapped(object sender, TappedEventArgs e)
    {
        var secilenTalep = e.Parameter as ServisTalebi;
        if (secilenTalep != null)
        {
            await Navigation.PushAsync(new ViewPhotosView(secilenTalep));
        }
    }

    private async void OnAddPhotoTapped(object sender, TappedEventArgs e)
    {
        var talep = e.Parameter as ServisTalebi;
        if (talep == null) return;

        try
        {
            var sonuclar = await FilePicker.PickMultipleAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Servis Fotoğraflarını Seçin"
            });

            if (sonuclar == null || !sonuclar.Any()) return;

            LoadingTitle.Text = "Fotoğraflar Sunucuya Aktarılıyor...";
            LoadingSubText.Text = $"{sonuclar.Count()} adet görsel işleniyor.";
            LoadingOverlay.IsVisible = true;
            await Task.Delay(5);

            int basarili = 0;
            int hatali = 0;

            foreach (var foto in sonuclar)
            {
                using var stream = await foto.OpenReadAsync();

                string zaman = DateTime.Now.ToString("yyyy_MM_dd_HHmm_ssfff");
                string uzanti = System.IO.Path.GetExtension(foto.FileName);
                if (string.IsNullOrEmpty(uzanti)) uzanti = ".jpg";

                string ozelDosyaAdi = $"Admin-{talep.id}-{zaman}{uzanti}";

                string sonuc = await _apiService.UploadHasarFotografAsync(talep.id, stream, ozelDosyaAdi);

                if (sonuc == "OK") basarili++;
                else hatali++;
            }

            if (hatali > 0)
            {
                await DisplayAlert("Kısmi Başarılı", $"{basarili} fotoğraf yüklendi, {hatali} fotoğraf yüklenemedi.", "Tamam");
            }
            else
            {
                await DisplayAlert("Başarılı", "Tüm fotoğraflar talebe başarıyla eklendi.", "Tamam");
            }

            await TalepleriYukle(reset: true);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Fotoğraf ekleme işlemi sırasında bir sorun oluştu: " + ex.Message, "Tamam");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    // =========================================================
    // TELEFON ARAMA
    // =========================================================

    private void OnPhoneTapped(object sender, TappedEventArgs e)
    {
        var phoneNumber = e.Parameter as string;
        if (!string.IsNullOrWhiteSpace(phoneNumber) && PhoneDialer.Default.IsSupported)
        {
            PhoneDialer.Default.Open(phoneNumber);
        }
    }
}