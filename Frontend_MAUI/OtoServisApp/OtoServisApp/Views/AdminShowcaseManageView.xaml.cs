using OtoServisApp.Models;
using OtoServisApp.Services;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace OtoServisApp.Views;

public partial class AdminShowcaseManageView : ContentPage
{
    private readonly ApiService _apiService = new();
    private List<TamamlananIs> _liste;
    private TamamlananIs? _duzenlenenOge;
    //private Stream? _seciliFotografStream;
    private string? _seciliFotografDosyaAdi;
    private List<Hizmet> _hizmetler;

    public AdminShowcaseManageView() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Yukle();
    }

    private int _sayfaBoyutu = 15;
    private int _mevcutSayfa = 1;
    private int _toplamSayfa = 1;

    private List<Hizmet> _tumHizmetler;
    private Hizmet _secilenHizmet;

    private async Task Yukle()
    {
        LoadingOverlay.IsVisible = true;
        LoadingMessage.Text = "Veriler Yükleniyor...";
        try
        {
            _liste = await _apiService.VitrinListesiGetirAsync();
            _toplamSayfa = (int)Math.Ceiling((double)_liste.Count / _sayfaBoyutu);
            if (_toplamSayfa == 0) _toplamSayfa = 1;
            SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
            GuncelleButonDurumlari();
            SayfayiGoster();
            _hizmetler = await _apiService.HizmetleriGetirAsync();
            HizmetPicker.ItemsSource = _hizmetler.Select(h => h.ad).ToList();

            // Hizmetleri çek
            _tumHizmetler = await _apiService.HizmetleriGetirAsync();
            HizmetListesi.ItemsSource = _tumHizmetler;
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Veriler yüklenemedi. Lütfen tekrar deneyin.", "Hata");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private void OnHizmetSecimTapped(object sender, TappedEventArgs e)
    {
        HizmetAramaKutusu.IsVisible = !HizmetAramaKutusu.IsVisible;
        if (HizmetAramaKutusu.IsVisible)
        {
            HizmetAramaBar.Text = string.Empty;
            HizmetListesi.ItemsSource = _tumHizmetler;
            HizmetAramaBar.Focus();
        }
    }

    private void OnHizmetAramaDegisti(object sender, TextChangedEventArgs e)
    {
        if (_tumHizmetler == null) return;
        var arama = e.NewTextValue?.ToLower() ?? "";
        if (string.IsNullOrWhiteSpace(arama))
            HizmetListesi.ItemsSource = _tumHizmetler;
        else
            HizmetListesi.ItemsSource = _tumHizmetler.Where(h =>
                h.ad.ToLower().Contains(arama) ||
                (h.aciklama != null && h.aciklama.ToLower().Contains(arama))
            ).ToList();
    }

    private void OnHizmetSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as Hizmet;
        if (secilen != null)
        {
            _secilenHizmet = secilen;
            SecilenHizmetLabel.Text = secilen.ad;
            HizmetAramaKutusu.IsVisible = false;
            HizmetListesi.SelectedItem = null;
        }
    }




    private void SayfayiGoster()
    {
        var sayfaListesi = _liste.Skip((_mevcutSayfa - 1) * _sayfaBoyutu).Take(_sayfaBoyutu).ToList();
        VitrinListesi.ItemsSource = sayfaListesi;
    }

    private void GuncelleButonDurumlari()
    {
        BtnOncekiLabel.Opacity = _mevcutSayfa > 1 ? 1.0 : 0.5;
        BtnSonrakiLabel.Opacity = _mevcutSayfa < _toplamSayfa ? 1.0 : 0.5;
    }

    private void OnOncekiTapped(object sender, TappedEventArgs e)
    {
        if (_mevcutSayfa > 1)
        {
            _mevcutSayfa--;
            SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
            GuncelleButonDurumlari();
            SayfayiGoster();
        }
    }

    private void OnSonrakiTapped(object sender, TappedEventArgs e)
    {
        if (_mevcutSayfa < _toplamSayfa)
        {
            _mevcutSayfa++;
            SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
            GuncelleButonDurumlari();
            SayfayiGoster();
        }
    }

    private async void OnVitrineGitTapped(object sender, TappedEventArgs e) => await Navigation.PushAsync(new ShowcaseView());

    private void OnYeniEkleTapped(object sender, TappedEventArgs e)
    {
        _duzenlenenOge = null;
        BaslikEntry.Text = AciklamaEditor.Text = "";
        TarihEntry.Text = DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        HizmetPicker.SelectedIndex = -1;
        _seciliFotografStream?.Dispose();
        _seciliFotografStream = null;
        SecilenFotoImage.Source = null;
        DuzenlemeFormu.IsVisible = true;
    }

    private async void OnDuzenleTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not TamamlananIs oge) return;
        _duzenlenenOge = oge;
        BaslikEntry.Text = oge.Baslik;
        AciklamaEditor.Text = oge.Aciklama;
        TarihEntry.Text = oge.Tarih;
        HizmetPicker.SelectedIndex = oge.HizmetId.HasValue ? _hizmetler.FindIndex(h => h.id == oge.HizmetId) : -1;
        _seciliFotografStream?.Dispose();
        _seciliFotografStream = null;
        SecilenFotoImage.Source = oge.TamResimUrl;
        DuzenlemeFormu.IsVisible = true;
    }

    private async void OnSilTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not TamamlananIs oge) return;
        if (!await ModernAlertService.ShowConfirmationAsync($"'{oge.Baslik}' silinecek. Emin misiniz?", "Silme Onayı")) return;

        LoadingOverlay.IsVisible = true;
        LoadingMessage.Text = "Siliniyor...";
        try
        {
            if (await _apiService.VitrinSilAsync(oge.Id))
            {
                await ModernAlertService.ShowInfoAsync("İş silindi.", "Başarılı");
                await Yukle();
            }
            else await ModernAlertService.ShowInfoAsync("Silinirken hata oluştu.", "Hata");
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Hata: " + ex.Message, "Hata");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private MemoryStream? _seciliFotografStream;

    private async void OnGaleridenSecClicked(object sender, TappedEventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images, PickerTitle = "Bir fotoğraf seçin" });
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                _seciliFotografStream = new MemoryStream();
                await stream.CopyToAsync(_seciliFotografStream);
                _seciliFotografStream.Position = 0;
                _seciliFotografDosyaAdi = result.FileName;
                SecilenFotoImage.Source = ImageSource.FromStream(() => new MemoryStream(_seciliFotografStream.ToArray()));
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf seçilirken bir sorun oluştu. Lütfen tekrar deneyin.", "Hata");
        }
    }

    private async void OnKameraCekClicked(object sender, TappedEventArgs e)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await ModernAlertService.ShowInfoAsync("Cihazınız kamera ile fotoğraf çekmeyi desteklemiyor.", "Hata");
            return;
        }
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                using var stream = await photo.OpenReadAsync();
                _seciliFotografStream = new MemoryStream();
                await stream.CopyToAsync(_seciliFotografStream);
                _seciliFotografStream.Position = 0;
                _seciliFotografDosyaAdi = photo.FileName;
                SecilenFotoImage.Source = ImageSource.FromStream(() => new MemoryStream(_seciliFotografStream.ToArray()));
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf çekilirken bir sorun oluştu. Lütfen tekrar deneyin.", "Hata");
        }
    }

    private async void OnKaydetTapped(object sender, TappedEventArgs e)
    {   
        // Türkçe karakter desteği ile büyük harf yap
        BaslikEntry.Text = BaslikEntry.Text?.ToUpper(new CultureInfo("tr-TR"));
        AciklamaEditor.Text = AciklamaEditor.Text?.ToUpper(new CultureInfo("tr-TR"));

        // Başlık ve açıklama zorunlu
        if (string.IsNullOrWhiteSpace(BaslikEntry.Text) || string.IsNullOrWhiteSpace(AciklamaEditor.Text))
        {
            await ModernAlertService.ShowInfoAsync("Başlık ve açıklama zorunludur.", "Uyarı");
            return;
        }

        // Tarih boş ise otomatik ata
        string tarih = TarihEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(tarih))
        {
            tarih = DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
            TarihEntry.Text = tarih;
        }

        if (_duzenlenenOge == null && _seciliFotografStream == null)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen bir fotoğraf seçin veya çekin.", "Uyarı");
            return;
        }
        /*if (HizmetPicker.SelectedIndex == -1)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen bir hizmet seçin (etiket için).", "Uyarı");
            return;
        }*/
        if (_secilenHizmet == null)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen bir hizmet seçin (etiket için).", "Uyarı");
            return;
        }

        //var secilenHizmet = _hizmetler[HizmetPicker.SelectedIndex];
        //string etiket = $"{GetIconForHizmet(secilenHizmet.ad)} {secilenHizmet.ad}";
        string etiket = $"{GetIconForHizmet(_secilenHizmet.ad)} {_secilenHizmet.ad}";
        int? hizmetId = _secilenHizmet.id;

        LoadingOverlay.IsVisible = true;
        LoadingMessage.Text = _duzenlenenOge == null ? "Ekleniyor..." : "Güncelleniyor...";
        KaydetLabel.Text = LoadingMessage.Text;

        try
        {
            if (_seciliFotografStream != null) _seciliFotografStream.Position = 0;

            if (_duzenlenenOge == null)
            {
                await _apiService.VitrinEkleAsync(BaslikEntry.Text, AciklamaEditor.Text, etiket, tarih, secilenHizmet.id, _seciliFotografStream!, _seciliFotografDosyaAdi ?? "foto.jpg");
                await ModernAlertService.ShowInfoAsync("İş başarıyla eklendi.", "Başarılı");
            }
            else
            {
                await _apiService.VitrinGuncelleAsync(_duzenlenenOge.Id, BaslikEntry.Text, AciklamaEditor.Text, etiket, tarih, secilenHizmet.id, _seciliFotografStream, _seciliFotografDosyaAdi);
                await ModernAlertService.ShowInfoAsync("İş başarıyla güncellendi.", "Başarılı");
            }

            DuzenlemeFormu.IsVisible = false;
            _seciliFotografStream?.Dispose();
            _seciliFotografStream = null;
            await Yukle();
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("İşlem sırasında bir hata oluştu. Lütfen internet bağlantınızı kontrol edip tekrar deneyin.", "Hata");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
            KaydetLabel.Text = "Kaydet";
        }
    }

    private void OnIptalTapped(object sender, TappedEventArgs e)
    {
        DuzenlemeFormu.IsVisible = false;
        _seciliFotografStream?.Dispose();
        _seciliFotografStream = null;
    }
    
    private string GetIconForHizmet(string hizmetAd)
    {
        // PERİYODİK BAKIM VE SIVILAR
        //if (hizmetAd.Contains("Periyodik") || hizmetAd.Contains("Bakım")) return "🔧";
        if (hizmetAd.Contains("Periyodik Bakım")) return "🔧";
        if (hizmetAd.Contains("Kışlık")) return "❄️";
        if (hizmetAd.Contains("Yazlık")) return "☀️";
        if (hizmetAd.Contains("Ağır Bakım") || hizmetAd.Contains("Triger")) return "⚙️";
        if (hizmetAd.Contains("Motor Yağı")) return "🛢️";
        if (hizmetAd.Contains("Antifriz")) return "🧊";
        if (hizmetAd.Contains("Fren Hidroliği")) return "🛑";
        if (hizmetAd.Contains("Direksiyon Hidroliği")) return "🚗";
        if (hizmetAd.Contains("Cam Suyu") || hizmetAd.Contains("Silecek")) return "💧";
        if (hizmetAd.Contains("Ekspertiz") || hizmetAd.Contains("Check-Up")) return "🔍";

        // FREN SİSTEMİ
        if (hizmetAd.Contains("Fren Balata")) return "🛑";
        if (hizmetAd.Contains("Fren Disk")) return "🛞";
        if (hizmetAd.Contains("Fren Kaliper")) return "🔧";
        if (hizmetAd.Contains("Fren Merkezi")) return "🧰";
        if (hizmetAd.Contains("Fren Hortumu")) return "🛠️";
        if (hizmetAd.Contains("ABS Sensörü")) return "📡";
        if (hizmetAd.Contains("ABS Beyni")) return "🧠";
        if (hizmetAd.Contains("El Freni Teli")) return "🅿️";

        // MOTOR VE MEKANİK
        if (hizmetAd.Contains("Buji")) return "🔥";
        if (hizmetAd.Contains("Kızdırma Bujisi")) return "🌡️";
        if (hizmetAd.Contains("Ateşleme Bobini")) return "⚡";
        if (hizmetAd.Contains("DPF") || hizmetAd.Contains("Partikül")) return "💨";
        if (hizmetAd.Contains("EGR")) return "🌫️";
        if (hizmetAd.Contains("Enjektör")) return "💉";
        if (hizmetAd.Contains("Motor Takozu")) return "🪨";
        if (hizmetAd.Contains("Turbo")) return "🌀";
        if (hizmetAd.Contains("Silindir Kapak Contası")) return "🧱";
        if (hizmetAd.Contains("Termostat")) return "🌡️";
        if (hizmetAd.Contains("Radyatör")) return "🧊";
        if (hizmetAd.Contains("Su Pompası") || hizmetAd.Contains("Devirdaim")) return "💦";
        if (hizmetAd.Contains("Yağ Kaçağı")) return "🛢️";
        if (hizmetAd.Contains("Katalitik")) return "♻️";
        if (hizmetAd.Contains("Boğaz Kelebeği")) return "🦋";

        // ŞANZIMAN VE DEBRİYAJ
        if (hizmetAd.Contains("Baskı Balata") || hizmetAd.Contains("Debriyaj")) return "⚙️";
        if (hizmetAd.Contains("Otomatik Şanzıman")) return "🔄";
        if (hizmetAd.Contains("DSG") || hizmetAd.Contains("EDC")) return "⚙️";
        if (hizmetAd.Contains("Mekatronik")) return "🧠";
        if (hizmetAd.Contains("Aks Lalesi") || hizmetAd.Contains("Körük")) return "🛞";

        // ALT TAKIM VE SÜSPANSİYON
        if (hizmetAd.Contains("Amortisör")) return "🚗";
        if (hizmetAd.Contains("Z Rot")) return "🔩";
        if (hizmetAd.Contains("Rotil") || hizmetAd.Contains("Rot Başı")) return "🔧";
        if (hizmetAd.Contains("Salıncak") || hizmetAd.Contains("Tabla")) return "🛠️";
        if (hizmetAd.Contains("Tekerlek Rulmanı") || hizmetAd.Contains("Porya")) return "🛞";
        if (hizmetAd.Contains("Helezon Yayı")) return "〰️";
        if (hizmetAd.Contains("Direksiyon Kutusu")) return "🚗";
        if (hizmetAd.Contains("Rot Balans")) return "🎯";
        if (hizmetAd.Contains("Amortisör Takozu")) return "🔩";

        // ELEKTRİK, ELEKTRONİK VE İKLİMLENDİRME
        if (hizmetAd.Contains("Akü")) return "🔋";
        if (hizmetAd.Contains("Şarj Dinamosu") || hizmetAd.Contains("Alternatör")) return "⚡";
        if (hizmetAd.Contains("Marş Dinamosu")) return "🔑";
        if (hizmetAd.Contains("Klima Gazı")) return "❄️";
        if (hizmetAd.Contains("Klima Kompresör")) return "🌀";
        if (hizmetAd.Contains("Kalorifer Peteği")) return "🔥";
        if (hizmetAd.Contains("Far Ampulü") || hizmetAd.Contains("Xenon")) return "💡";
        if (hizmetAd.Contains("Far Temizliği")) return "✨";
        if (hizmetAd.Contains("Sigorta") || hizmetAd.Contains("Tesisat")) return "🔌";
        if (hizmetAd.Contains("ECU") || hizmetAd.Contains("Yazılım")) return "💻";

        // Genel
        return "🛠️";
    }
}