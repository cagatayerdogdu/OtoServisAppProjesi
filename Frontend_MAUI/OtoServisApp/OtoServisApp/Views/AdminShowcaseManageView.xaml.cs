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
    private Stream? _seciliFotografStream;
    private string? _seciliFotografDosyaAdi;
    private List<Hizmet> _hizmetler;

    public AdminShowcaseManageView() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Yukle();
    }

    private async Task Yukle()
    {
        LoadingOverlay.IsVisible = true;
        LoadingMessage.Text = "Veriler Yükleniyor...";
        try
        {
            _liste = await _apiService.VitrinListesiGetirAsync();
            VitrinListesi.ItemsSource = _liste;
            _hizmetler = await _apiService.HizmetleriGetirAsync();
            HizmetPicker.ItemsSource = _hizmetler.Select(h => h.ad).ToList();
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

    private async void OnVitrineGitTapped(object sender, TappedEventArgs e) => await Navigation.PushAsync(new ShowcaseView());

    private void OnYeniEkleTapped(object sender, TappedEventArgs e)
    {
        _duzenlenenOge = null;
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

    private async void OnGaleridenSecClicked(object sender, TappedEventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images, PickerTitle = "Bir fotoğraf seçin" });
            if (result != null)
            {
                _seciliFotografStream = await result.OpenReadAsync();
                _seciliFotografDosyaAdi = result.FileName;
                SecilenFotoImage.Source = ImageSource.FromStream(() => _seciliFotografStream);
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf seçilemedi: " + ex.Message, "Hata");
        }
    }

    private async void OnKameraCekClicked(object sender, TappedEventArgs e)
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
                _seciliFotografStream = await photo.OpenReadAsync();
                _seciliFotografDosyaAdi = photo.FileName;
                SecilenFotoImage.Source = ImageSource.FromStream(() => _seciliFotografStream);
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf çekilemedi: " + ex.Message, "Hata");
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
        string etiket = secilenHizmet.ad;

        LoadingOverlay.IsVisible = true;
        LoadingMessage.Text = _duzenlenenOge == null ? "Ekleniyor..." : "Güncelleniyor...";
        KaydetLabel.Text = LoadingMessage.Text;

        try
        {
            if (_duzenlenenOge == null)
            {
                await _apiService.VitrinEkleAsync(BaslikEntry.Text, AciklamaEditor.Text, etiket, TarihEntry.Text, secilenHizmet.id, _seciliFotografStream!, _seciliFotografDosyaAdi ?? "foto.jpg");
                await ModernAlertService.ShowInfoAsync("İş eklendi.", "Başarılı");
            }
            else
            {
                await _apiService.VitrinGuncelleAsync(_duzenlenenOge.Id, BaslikEntry.Text, AciklamaEditor.Text, etiket, TarihEntry.Text, secilenHizmet.id, _seciliFotografStream, _seciliFotografDosyaAdi);
                await ModernAlertService.ShowInfoAsync("İş güncellendi.", "Başarılı");
            }

            DuzenlemeFormu.IsVisible = false;
            _seciliFotografStream?.Dispose();
            _seciliFotografStream = null;
            await Yukle();
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Kaydedilemedi: " + ex.Message, "Hata");
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
}