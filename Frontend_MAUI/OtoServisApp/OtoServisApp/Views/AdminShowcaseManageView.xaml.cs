using OtoServisApp.Models;
using OtoServisApp.Services;
using System.IO;

namespace OtoServisApp.Views;

public partial class AdminShowcaseManageView : ContentPage
{
    private readonly ApiService _apiService;
    private List<TamamlananIs> _liste;
    private TamamlananIs? _duzenlenenOge;
    private Stream? _seciliFotografStream;
    private string? _seciliFotografDosyaAdi;

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
            VitrinListesi.ItemsSource = _liste;
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
        BaslikEntry.Text = "";
        AciklamaEditor.Text = "";
        EtiketEntry.Text = "";
        TarihEntry.Text = "";
        _seciliFotografStream = null;
        SecilenFotoImage.Source = null;
        DuzenlemeFormu.IsVisible = true;
    }

    private async void OnDuzenleTapped(object sender, TappedEventArgs e)
    {
        var oge = e.Parameter as TamamlananIs;
        if (oge == null) return;

        _duzenlenenOge = oge;
        BaslikEntry.Text = oge.Baslik;
        AciklamaEditor.Text = oge.Aciklama;
        EtiketEntry.Text = oge.Etiket;
        TarihEntry.Text = oge.Tarih;

        _seciliFotografStream = null;
        // Mevcut resmi göster
        SecilenFotoImage.Source = oge.TamResimUrl;

        DuzenlemeFormu.IsVisible = true;
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

    private async void OnKaydetClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BaslikEntry.Text) ||
            string.IsNullOrWhiteSpace(AciklamaEditor.Text) ||
            string.IsNullOrWhiteSpace(EtiketEntry.Text) ||
            string.IsNullOrWhiteSpace(TarihEntry.Text))
        {
            await ModernAlertService.ShowInfoAsync("Tüm alanları doldurun.", "Uyarı");
            return;
        }

        // Yeni kayıt için fotoğraf zorunlu
        if (_duzenlenenOge == null && _seciliFotografStream == null)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen bir fotoğraf seçin veya çekin.", "Uyarı");
            return;
        }

        LoadingOverlay.IsVisible = true;
        try
        {
            if (_duzenlenenOge == null)
            {
                // Ekle
                var yeni = await _apiService.VitrinEkleAsync(
                    BaslikEntry.Text,
                    AciklamaEditor.Text,
                    EtiketEntry.Text,
                    TarihEntry.Text,
                    _seciliFotografStream!,
                    _seciliFotografDosyaAdi ?? "foto.jpg"
                );
                await ModernAlertService.ShowInfoAsync("İş eklendi.", "Başarılı");
            }
            else
            {
                // Güncelle
                var guncel = await _apiService.VitrinGuncelleAsync(
                    _duzenlenenOge.Id,
                    BaslikEntry.Text,
                    AciklamaEditor.Text,
                    EtiketEntry.Text,
                    TarihEntry.Text,
                    _seciliFotografStream,
                    _seciliFotografDosyaAdi
                );
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
        }
    }

    private void OnIptalClicked(object sender, EventArgs e)
    {
        DuzenlemeFormu.IsVisible = false;
        _seciliFotografStream?.Dispose();
        _seciliFotografStream = null;
    }
}