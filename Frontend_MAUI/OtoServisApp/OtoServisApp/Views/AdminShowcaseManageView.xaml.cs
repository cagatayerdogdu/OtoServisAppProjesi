using OtoServisApp.Models;
using OtoServisApp.Services;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

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
        TarihEntry.Text = DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        BaslikEntry.Text = AciklamaEditor.Text = TarihEntry.Text = "";
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
        if (string.IsNullOrWhiteSpace(BaslikEntry.Text) || string.IsNullOrWhiteSpace(AciklamaEditor.Text) || string.IsNullOrWhiteSpace(TarihEntry.Text))
        {
            await ModernAlertService.ShowInfoAsync("Başlık, açıklama ve tarih zorunludur.", "Uyarı");
            return;
        }
        if (_duzenlenenOge == null && _seciliFotografStream == null)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen bir fotoğraf seçin veya çekin.", "Uyarı");
            return;
        }
        if (HizmetPicker.SelectedIndex == -1)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen bir hizmet seçin (etiket için).", "Uyarı");
            return;
        }

        var secilenHizmet = _hizmetler[HizmetPicker.SelectedIndex];
        string etiket = $"{GetIconForHizmet(secilenHizmet.ad)} {secilenHizmet.ad}";

        LoadingOverlay.IsVisible = true;
        LoadingMessage.Text = _duzenlenenOge == null ? "Ekleniyor..." : "Güncelleniyor...";
        KaydetLabel.Text = LoadingMessage.Text;

        try
        {
            if (_seciliFotografStream != null)
                _seciliFotografStream.Position = 0;

            if (_duzenlenenOge == null)
            {
                await _apiService.VitrinEkleAsync(BaslikEntry.Text, AciklamaEditor.Text, etiket, TarihEntry.Text, secilenHizmet.id, _seciliFotografStream!, _seciliFotografDosyaAdi ?? "foto.jpg");
                await ModernAlertService.ShowInfoAsync("İş başarıyla eklendi.", "Başarılı");
            }
            else
            {
                await _apiService.VitrinGuncelleAsync(_duzenlenenOge.Id, BaslikEntry.Text, AciklamaEditor.Text, etiket, TarihEntry.Text, secilenHizmet.id, _seciliFotografStream, _seciliFotografDosyaAdi);
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
        if (hizmetAd.Contains("Periyodik") || hizmetAd.Contains("Bakım")) return "🔧";
        if (hizmetAd.Contains("Fren")) return "🛑";
        if (hizmetAd.Contains("Motor")) return "⚙️";
        if (hizmetAd.Contains("Seramik") || hizmetAd.Contains("Kaplama")) return "✨";
        if (hizmetAd.Contains("Temiz")) return "🧼";
        if (hizmetAd.Contains("Klima")) return "❄️";
        if (hizmetAd.Contains("Amortisör") || hizmetAd.Contains("Süspansiyon")) return "🚗";
        if (hizmetAd.Contains("Akü") || hizmetAd.Contains("Elektrik")) return "🔋";
        return "🛠️";
    }
}