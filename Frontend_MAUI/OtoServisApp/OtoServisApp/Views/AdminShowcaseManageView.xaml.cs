using OtoServisApp.Models;
using OtoServisApp.Services;
using Microsoft.Maui.Storage;
using System.IO;

namespace OtoServisApp.Views;

public partial class AdminShowcaseManageView : ContentPage
{
    private readonly ApiService _apiService;
    private List<TamamlananIs> _liste;
    private List<Hizmet> _hizmetler;
    private TamamlananIs? _duzenlenenOge;
    private FileResult? _seciliFoto;

    public AdminShowcaseManageView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Yukle();
    }

    private async Task Yukle()
    {
        LoadingOverlay.IsVisible = true;
        try
        {
            _liste = await _apiService.VitrinListesiGetirAsync();
            _hizmetler = await _apiService.HizmetleriGetirAsync();

            VitrinListesi.ItemsSource = _liste;

            EtiketPicker.ItemsSource = _hizmetler.Select(h => h.ad).ToList();
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Veriler yüklenemedi: " + ex.Message, "Hata");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnYeniEkleTapped(object sender, TappedEventArgs e)
    {
        _duzenlenenOge = null;
        FormBaslik.Text = "Yeni İş Ekle";
        BaslikEntry.Text = "";
        AciklamaEditor.Text = "";
        EtiketPicker.SelectedIndex = -1;
        TarihEntry.Text = "";
        _seciliFoto = null;
        SecilenFotoImage.Source = null;
        FormOverlay.IsVisible = true;
    }

    private async void OnDuzenleTapped(object sender, TappedEventArgs e)
    {
        var oge = e.Parameter as TamamlananIs;
        if (oge == null) return;

        _duzenlenenOge = oge;
        FormBaslik.Text = "İş Düzenle";
        BaslikEntry.Text = oge.Baslik;
        AciklamaEditor.Text = oge.Aciklama;

        var hizmetAd = oge.Etiket?.Replace("✨ ", "").Replace("🔧 ", "").Replace("🧼 ", "").Trim(); // İkonları temizle
        EtiketPicker.SelectedIndex = _hizmetler.FindIndex(h => h.ad == hizmetAd);

        TarihEntry.Text = oge.Tarih;
        _seciliFoto = null;
        SecilenFotoImage.Source = oge.TamResimUrl;
        FormOverlay.IsVisible = true;
    }

    private async void OnSilTapped(object sender, TappedEventArgs e)
    {
        var oge = e.Parameter as TamamlananIs;
        if (oge == null) return;

        bool onay = await ModernAlertService.ShowConfirmationAsync($"'{oge.Baslik}' silinecek. Emin misiniz?", "Silme Onayı");
        if (!onay) return;

        LoadingOverlay.IsVisible = true;
        try
        {
            bool sonuc = await _apiService.VitrinSilAsync(oge.Id);
            if (sonuc)
            {
                await ModernAlertService.ShowInfoAsync("İş silindi.", "Başarılı");
                await Yukle();
            }
            else
            {
                await ModernAlertService.ShowInfoAsync("Silinirken hata oluştu.", "Hata");
            }
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

    private async void OnGaleridenSecClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Bir fotoğraf seçin"
            });

            if (result != null)
            {
                _seciliFoto = result;
                SecilenFotoImage.Source = ImageSource.FromStream(() => result.OpenReadAsync().Result);
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf seçilemedi: " + ex.Message, "Hata");
        }
    }

    private async void OnKameraCekClicked(object sender, EventArgs e)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await ModernAlertService.ShowInfoAsync("Kamera desteklenmiyor.", "Hata");
            return;
        }

        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                _seciliFoto = photo;
                SecilenFotoImage.Source = ImageSource.FromStream(() => photo.OpenReadAsync().Result);
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf çekilemedi: " + ex.Message, "Hata");
        }
    }

    private async void OnKaydetClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BaslikEntry.Text) ||
            string.IsNullOrWhiteSpace(AciklamaEditor.Text) ||
            EtiketPicker.SelectedIndex == -1 ||
            string.IsNullOrWhiteSpace(TarihEntry.Text))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen tüm alanları doldurun.", "Uyarı");
            return;
        }

        if (_duzenlenenOge == null && _seciliFoto == null)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen bir fotoğraf seçin veya çekin.", "Uyarı");
            return;
        }

        LoadingOverlay.IsVisible = true;
        try
        {
            var secilenHizmetAd = EtiketPicker.SelectedItem as string;
            var ikon = await GetIconForHizmet(secilenHizmetAd);
            string etiket = $"{ikon} {secilenHizmetAd}";

            if (_duzenlenenOge == null)
            {
                // Yeni ekle
                using var stream = await _seciliFoto.OpenReadAsync();
                var dosyaAdi = $"AdminShowcase_{DateTime.Now:yyyy_MM_dd_HHmm_ssfff}.jpg";
                var yeni = await _apiService.VitrinEkleAsync(
                    BaslikEntry.Text,
                    AciklamaEditor.Text,
                    etiket,
                    TarihEntry.Text,
                    stream,
                    dosyaAdi
                );
                await ModernAlertService.ShowInfoAsync("İş eklendi.", "Başarılı");
            }
            else
            {
                // Güncelle
                Stream? fotoStream = null;
                string? dosyaAdi = null;
                if (_seciliFoto != null)
                {
                    fotoStream = await _seciliFoto.OpenReadAsync();
                    dosyaAdi = $"AdminShowcase_{DateTime.Now:yyyy_MM_dd_HHmm_ssfff}.jpg";
                }
                var guncel = await _apiService.VitrinGuncelleAsync(
                    _duzenlenenOge.Id,
                    BaslikEntry.Text,
                    AciklamaEditor.Text,
                    etiket,
                    TarihEntry.Text,
                    fotoStream,
                    dosyaAdi
                );
                await ModernAlertService.ShowInfoAsync("İş güncellendi.", "Başarılı");
            }

            FormOverlay.IsVisible = false;
            await Yukle();
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Kaydedilemedi: " + ex.Message, "Hata");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private void OnIptalClicked(object sender, EventArgs e)
    {
        FormOverlay.IsVisible = false;
        _seciliFoto = null;
    }

    private async void OnVitrineGitTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new ShowcaseView());
    }

    private Task<string> GetIconForHizmet(string hizmetAd)
    {
        // Hizmet adına göre ikon eşleştirmesi (istersen genişletebilirsin)
        if (hizmetAd.Contains("Periyodik") || hizmetAd.Contains("Bakım"))
            return Task.FromResult("🔧");
        if (hizmetAd.Contains("Fren"))
            return Task.FromResult("🛑");
        if (hizmetAd.Contains("Motor"))
            return Task.FromResult("⚙️");
        if (hizmetAd.Contains("Seramik") || hizmetAd.Contains("Kaplama"))
            return Task.FromResult("✨");
        if (hizmetAd.Contains("Temiz"))
            return Task.FromResult("🧼");
        return Task.FromResult("🛠️");
    }
}