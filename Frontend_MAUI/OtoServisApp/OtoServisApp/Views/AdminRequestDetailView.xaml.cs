using OtoServisApp.Models;
using OtoServisApp.Services;
using System.IO;

#if IOS
using UIKit;
using CoreGraphics;
#elif ANDROID
using Android.Views;
using Android.Graphics;
#endif

namespace OtoServisApp.Views;

public partial class AdminRequestDetailView : ContentPage
{
    private readonly ApiService _apiService;
    private ServisTalebi _talep;

    public AdminRequestDetailView(ServisTalebi talep)
    {
        InitializeComponent();
        _talep = talep;
        _apiService = new ApiService();
        BindingContext = _talep;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Durum listesini hazırla
        FloatingDurumListesi.ItemsSource = new List<string> { "Bekliyor", "Onaylandı", "İşlemde", "Tamamlandı", "İptal Edildi" };
    }

    private void OnDurumSecimTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        if (border == null) return;

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

        AbsoluteLayout.SetLayoutBounds(FloatingItemDurumMenusu, new Microsoft.Maui.Graphics.Rect(buton_X, buton_Y, 130, 200));
    }

    private void OnFloatingMenuClose(object sender, EventArgs e)
    {
        FloatingMenuOverlay.IsVisible = false;
    }

    private void OnFloatingDurumSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (!string.IsNullOrEmpty(secilen))
        {
            _talep.durum = secilen;
            KartDurumLabel.Text = secilen;
        }
        FloatingMenuOverlay.IsVisible = false;
    }

    private async void OnUpdateTapped(object sender, TappedEventArgs e)
    {
        if (_talep == null) return;

        // Tutarı Entry'den güncelle
        if (double.TryParse(TutarEntry.Text, out double yeniTutar))
        {
            _talep.tahmini_tutar = yeniTutar;
        }

        LoadingOverlay.IsVisible = true;
        LoadingTitle.Text = "Güncelleniyor...";

        string idStr = await SecureStorage.Default.GetAsync("kullanici_id_gizli");
        int? aktifAdminId = int.TryParse(idStr, out int id) ? id : (int?)null;

        bool basarili = await _apiService.AdminTalepGuncelleAsync(_talep.id, _talep.durum, _talep.tahmini_tutar, aktifAdminId);

        if (basarili)
        {
            await DisplayAlert("Başarılı", "Talep güncellendi.", "Tamam");
            MessagingCenter.Send<object>(this, "TalepGuncellendi");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Hata", "Güncellenirken bir sorun oluştu.", "Tamam");
        }

        LoadingOverlay.IsVisible = false;
    }

    private async void OnViewPhotosTapped(object sender, TappedEventArgs e)
    {
        if (_talep != null)
            await Navigation.PushAsync(new ViewPhotosView(_talep));
    }

    private async void OnAddPhotoTapped(object sender, TappedEventArgs e)
    {
        if (_talep == null) return;

        try
        {
            var sonuclar = await FilePicker.PickMultipleAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Servis Fotoğraflarını Seçin"
            });

            if (sonuclar == null || !sonuclar.Any()) return;

            LoadingOverlay.IsVisible = true;
            LoadingTitle.Text = "Fotoğraflar Yükleniyor...";

            int basarili = 0;
            foreach (var foto in sonuclar)
            {
                using var stream = await foto.OpenReadAsync();
                string zaman = DateTime.Now.ToString("yyyy_MM_dd_HHmm_ssfff");
                string uzanti = System.IO.Path.GetExtension(foto.FileName);
                if (string.IsNullOrEmpty(uzanti)) uzanti = ".jpg";
                string ozelDosyaAdi = $"Admin-{_talep.id}-{zaman}{uzanti}";
                string sonuc = await _apiService.UploadHasarFotografAsync(_talep.id, stream, ozelDosyaAdi);
                if (sonuc == "OK") basarili++;
            }

            await DisplayAlert("Başarılı", $"{basarili} fotoğraf yüklendi.", "Tamam");
            // Fotoğraf durumu güncellensin diye sayfayı yenileyebiliriz ama detayda çok gerekli değil.
            _talep.foto_var_mi = true; // anlık güncelleme
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Fotoğraf eklenemedi: {ex.Message}", "Tamam");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnCopyTapped(object sender, TappedEventArgs e)
    {
        var adres = e.Parameter as string;
        if (!string.IsNullOrWhiteSpace(adres))
        {
            await Clipboard.Default.SetTextAsync(adres);
            await DisplayAlert("Kopyalandı", "Adres panoya kopyalandı.", "Tamam");
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