using OtoServisApp.Models;
using OtoServisApp.Services;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using System.IO;

namespace OtoServisApp.Views;

public partial class AdminShowcaseManageView : ContentPage
{
    private readonly ApiService _apiService;
    private List<TamamlananIs> _liste;
    private TamamlananIs? _duzenlenenOge;
    private Stream? _seciliFotografStream;
    private string? _seciliFotografDosyaAdi;

    public string FormBaslik => _duzenlenenOge == null ? "Yeni İş Ekle" : "İşi Düzenle";

    public AdminShowcaseManageView()
    {
        InitializeComponent();
        _apiService = new ApiService();
        BindingContext = this;
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
            await ModernAlertService.ShowInfoAsync("Veriler yüklenirken bir sorun oluştu. Lütfen internet bağlantınızı kontrol edin.", "Yükleme Hatası");
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
        _seciliFotografStream?.Dispose();
        _seciliFotografStream = null;
        SecilenFotoImage.Source = null;
        OnPropertyChanged(nameof(FormBaslik));
        DuzenlemeOverlay.IsVisible = true;
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

        _seciliFotografStream?.Dispose();
        _seciliFotografStream = null;
        SecilenFotoImage.Source = oge.TamResimUrl;
        OnPropertyChanged(nameof(FormBaslik));
        DuzenlemeOverlay.IsVisible = true;
    }

    private async void OnSilTapped(object sender, TappedEventArgs e)
    {
        var oge = e.Parameter as TamamlananIs;
        if (oge == null) return;

        bool onay = await ModernAlertService.ShowConfirmationAsync($"'{oge.Baslik}' isimli vitrin öğesini silmek istediğinize emin misiniz?", "Silme Onayı");
        if (!onay) return;

        LoadingOverlay.IsVisible = true;
        try
        {
            bool sonuc = await _apiService.VitrinSilAsync(oge.Id);
            if (sonuc)
            {
                await ModernAlertService.ShowInfoAsync("Vitrin öğesi başarıyla silindi.", "Başarılı");
                await Yukle();
            }
            else
            {
                await ModernAlertService.ShowInfoAsync("Silme işlemi sırasında bir sorun oluştu.", "Hata");
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Silme işlemi başarısız. Lütfen tekrar deneyin.", "Hata");
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
                _seciliFotografStream?.Dispose();
                _seciliFotografStream = await result.OpenReadAsync();
                _seciliFotografDosyaAdi = result.FileName;
                SecilenFotoImage.Source = ImageSource.FromStream(() => _seciliFotografStream);
            }
        }
        catch (PermissionException)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf seçmek için depolama izni gerekiyor. Lütfen uygulama ayarlarından izin verin.", "İzin Gerekli");
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf seçilirken bir sorun oluştu. Lütfen tekrar deneyin.", "Hata");
        }
    }

    private async void OnKameraCekClicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                await ModernAlertService.ShowInfoAsync("Fotoğraf çekmek için kamera izni gerekiyor. Lütfen uygulama ayarlarından izin verin.", "İzin Gerekli");
                return;
            }

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await ModernAlertService.ShowInfoAsync("Cihazınız fotoğraf çekmeyi desteklemiyor.", "Desteklenmiyor");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                _seciliFotografStream?.Dispose();
                _seciliFotografStream = await photo.OpenReadAsync();
                _seciliFotografDosyaAdi = photo.FileName;
                SecilenFotoImage.Source = ImageSource.FromStream(() => _seciliFotografStream);
            }
        }
        catch (PermissionException)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf çekmek için kamera izni gerekiyor. Lütfen uygulama ayarlarından izin verin.", "İzin Gerekli");
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf çekilirken bir sorun oluştu. Lütfen tekrar deneyin.", "Hata");
        }
    }

    private async void OnKaydetClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BaslikEntry.Text) ||
            string.IsNullOrWhiteSpace(AciklamaEditor.Text) ||
            string.IsNullOrWhiteSpace(EtiketEntry.Text) ||
            string.IsNullOrWhiteSpace(TarihEntry.Text))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen tüm alanları doldurun.", "Eksik Bilgi");
            return;
        }

        if (_duzenlenenOge == null && _seciliFotografStream == null)
        {
            await ModernAlertService.ShowInfoAsync("Lütfen bir fotoğraf seçin veya çekin.", "Fotoğraf Gerekli");
            return;
        }

        LoadingOverlay.IsVisible = true;
        try
        {
            if (_duzenlenenOge == null)
            {
                // Yeni ekleme
                var yeni = await _apiService.VitrinEkleAsync(
                    BaslikEntry.Text,
                    AciklamaEditor.Text,
                    EtiketEntry.Text,
                    TarihEntry.Text,
                    _seciliFotografStream!,
                    _seciliFotografDosyaAdi ?? "foto.jpg"
                );
                await ModernAlertService.ShowInfoAsync("Yeni vitrin öğesi başarıyla eklendi.", "Başarılı");
            }
            else
            {
                // Güncelleme
                var guncel = await _apiService.VitrinGuncelleAsync(
                    _duzenlenenOge.Id,
                    BaslikEntry.Text,
                    AciklamaEditor.Text,
                    EtiketEntry.Text,
                    TarihEntry.Text,
                    _seciliFotografStream,
                    _seciliFotografDosyaAdi
                );
                await ModernAlertService.ShowInfoAsync("Vitrin öğesi başarıyla güncellendi.", "Başarılı");
            }

            DuzenlemeOverlay.IsVisible = false;
            _seciliFotografStream?.Dispose();
            _seciliFotografStream = null;
            await Yukle();
        }
        catch (HttpRequestException ex)
        {
            await ModernAlertService.ShowInfoAsync("Sunucuya bağlanırken bir sorun oluştu. Lütfen internet bağlantınızı kontrol edin.", "Bağlantı Hatası");
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Kaydetme işlemi başarısız. Lütfen tekrar deneyin.", "Hata");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private void OnIptalClicked(object sender, EventArgs e)
    {
        DuzenlemeOverlay.IsVisible = false;
        _seciliFotografStream?.Dispose();
        _seciliFotografStream = null;
    }

    private async void OnVitrinGoruntuleTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new ShowcaseView());
    }
}