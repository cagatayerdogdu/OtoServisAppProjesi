using System.Collections.Concurrent;
using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminRequestsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<ServisTalebi> _orijinalTalepler;

    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde" };
    private string _secilenDurum = "Tümü";

    public AdminRequestsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        LoadingTitle.Text = "Talepler Yükleniyor...";
        LoadingSubText.Text = "Lütfen bekleyiniz.";
        LoadingOverlay.IsVisible = true;

        await Task.Delay(5);

        try
        {
            if (DurumListesi != null && DurumListesi.ItemsSource == null)
                DurumListesi.ItemsSource = _durumFiltreleri;

            await VerileriYukle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Veriler yüklenirken bir sorun oluştu.", "Tamam");
            System.Diagnostics.Debug.WriteLine($"Yükleme Hatası: {ex.Message}");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private async Task VerileriYukle()
    {
        // 1. PARALEL ÇEKİM: Ana verileri aynı anda ateşleyip zaman kazanıyoruz
        var hizmetTask = _apiService.HizmetleriGetirAsync();
        var talepTask = _apiService.AdminAktifTalepleriGetirAsync();
        var markaTask = _apiService.MarkalariGetirAsync();

        await Task.WhenAll(hizmetTask, talepTask, markaTask);

        _tumHizmetler = await hizmetTask;
        _orijinalTalepler = await talepTask;
        var markalar = await markaTask;

        if (_orijinalTalepler != null && _orijinalTalepler.Count > 0)
        {
            // 2. CACHE (ÖNBELLEK) MEKANİZMASI: Aynı aracı tekrar tekrar API'den çekmemek için

            var aracHavuzu = new ConcurrentDictionary<int, Arac>();

            // 3. N+1 PROBLEMİNİN ÇÖZÜMÜ: Tüm taleplerin detaylarını paralel olarak doldur

            var gorevler = _orijinalTalepler.Select(async talep =>
            {
                // Hizmet Eşleştirme
                if (_tumHizmetler != null)
                {
                    var h = _tumHizmetler.FirstOrDefault(x => x.id == talep.hizmet_id);
                    if (h != null) talep.hizmet_adi = h.ad;
                }

                // Araç Bilgilerini Detaylandırma (Hafızada yoksa API'den çekip havuza at)
                if (!aracHavuzu.TryGetValue(talep.arac_id, out var arac))
                {
                    arac = await _apiService.AracGetirAsync(talep.arac_id);
                    if (arac != null)
                    {
                        aracHavuzu.TryAdd(talep.arac_id, arac);
                    }
                }

                if (arac != null)
                {
                    string gosterimAd = "";
                    if (arac.marka_id != null && arac.model_id != null && markalar != null)
                    {
                        var marka = markalar.FirstOrDefault(m => m.id == arac.marka_id);
                        var model = marka?.modeller?.FirstOrDefault(m => m.id == arac.model_id);
                        if (marka != null && model != null) gosterimAd = $"{marka.ad} {model.ad}";
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

                //var fotolar = await _apiService.TalepFotograflariniGetirAsync(talep.id);
                //talep.foto_var_mi = fotolar != null && fotolar.Count > 0;
            });
            // Başlatılan tüm asenkron görevleri bekle
            await Task.WhenAll(gorevler);

            // Fotoğraf durumlarını TEK SEFERDE toplu olarak al
            var talepIdleri = _orijinalTalepler.Select(t => t.id).ToList();
            var fotoDurumlari = await _apiService.TopluFotografDurumuGetirAsync(talepIdleri);

            foreach (var talep in _orijinalTalepler)
            {
                talep.foto_var_mi = fotoDurumlari.TryGetValue(talep.id, out var varMi) && varMi;
            }

            FiltreleriUygula();
        }
        else
        {
            RequestsList.ItemsSource = null;
        }
    }

    // =========================================================
    // FİLTRELEME SİSTEMİ (Arama Barı)
    // =========================================================
    private CancellationTokenSource _aramaCts;
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _aramaCts?.Cancel();
        RequestsList.ItemsSource = null;
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
                (t.arac_adi_tam != null && t.arac_adi_tam.ToLower().Contains(kelime))
            );
        }

        filtrelenmisListe = filtrelenmisListe
            .OrderBy(t => t.durum switch
            {
                "Bekliyor" => 1,
                "Onaylandı" => 2,
                "İşlemde" => 3,
                // "Tamamlandı" => 4,
                // "İptal Edildi" => 5,
                _ => 4
            })
            .ThenBy(t => t.id);

        RequestsList.ItemsSource = filtrelenmisListe.ToList();
    }

    // =========================================================
    // ÜST TARAF FİLTRE DROPDOWN KONTROLLERİ
    // =========================================================
    private void OnFiltreDurumKutusuAcKapat(object sender, EventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;

        if (DurumSecimKutusu.IsVisible && _orijinalTalepler != null)
        {
            foreach (var talep in _orijinalTalepler.Where(t => t.DropdownAcikMi))
            {
                talep.DropdownAcikMi = false;
            }
        }
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

    // =========================================================
    // KART İÇİ DURUM SEÇİMİ
    // =========================================================
    private void OnItemDurumKutusuAc(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var secilenTalep = btn?.BindingContext as ServisTalebi;

        if (secilenTalep != null)
        {
            DurumSecimKutusu.IsVisible = false;

            if (_orijinalTalepler != null)
            {
                foreach (var talep in _orijinalTalepler.Where(t => t.DropdownAcikMi && t != secilenTalep))
                {
                    talep.DropdownAcikMi = false;
                }
            }

            // Model üzerinden görünürlüğü tetikliyoruz (Bu sayede UI otomatik güncelleniyor)
            secilenTalep.DropdownAcikMi = !secilenTalep.DropdownAcikMi;
        }
    }

    private void OnItemDurumSecildi(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var yeniDurum = btn?.Text;
        var secilenTalep = btn?.BindingContext as ServisTalebi;

        if (secilenTalep != null && !string.IsNullOrEmpty(yeniDurum))
        {
            secilenTalep.durum = yeniDurum;
            secilenTalep.DropdownAcikMi = false; // Menüyü kapat

            // Hiyerarşik ağaçtan ana butonu bulup yazısını güncelliyoruz (Anlık yenileme hissi için)
            var verticalLayout = btn.Parent as VerticalStackLayout;
            var dropdownBorder = verticalLayout?.Parent as Border;
            var grid = dropdownBorder?.Parent as Grid;

            if (grid != null)
            {
                var mainButton = grid.Children.OfType<Button>().FirstOrDefault();
                if (mainButton != null)
                {
                    mainButton.Text = yeniDurum;
                }
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
                    await VerileriYukle();
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

    private async void OnViewPhotosClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            await Navigation.PushAsync(new ViewPhotosView(secilenTalep));
        }
    }

    private async void OnAddPhotoClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var talep = button?.CommandParameter as ServisTalebi;

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
                string uzanti = Path.GetExtension(foto.FileName);
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

            await VerileriYukle();
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

    private void OnPhoneTapped(object sender, TappedEventArgs e)
    {
        var phoneNumber = e.Parameter as string;
        if (!string.IsNullOrWhiteSpace(phoneNumber) && PhoneDialer.Default.IsSupported)
        {
            PhoneDialer.Default.Open(phoneNumber);
        }
    }
}