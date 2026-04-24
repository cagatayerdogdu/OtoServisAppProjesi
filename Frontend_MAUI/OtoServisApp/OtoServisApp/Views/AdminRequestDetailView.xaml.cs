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

        MessagingCenter.Subscribe<object, ServisTalebi>(this, "TalepDetayGuncellendi", (sender, guncelTalep) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _talep = guncelTalep;
                KartDurumLabel.Text = guncelTalep.durum; // Durum label'ını manuel güncelle
                BindingContext = _talep;
            });
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BottomSheetDurumListesi.ItemsSource = new List<string> { "Bekliyor", "Onaylandı", "İşlemde", "Tamamlandı", "İptal Edildi" };
    }

    /// <summary>
    /// Mevcut Durum butonuna tıklanınca yüzen durum menüsünü açar.
    /// Menü varsayılan olarak butonun ALTINDA açılır.
    /// Eğer altta yeterli boşluk yoksa butonun ÜSTÜNDE açılır.
    /// Menü yüksekliği içeriğe göre dinamik olarak hesaplanır.
    /// </summary>
    private void OnDurumSecimTapped(object sender, TappedEventArgs e)
    {
        BottomSheetMenu.IsVisible = true;
    }

    private void OnCloseBottomSheet(object sender, TappedEventArgs e)
    {
        BottomSheetMenu.IsVisible = false;
    }

    private void OnBottomSheetDurumSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (!string.IsNullOrEmpty(secilen))
        {
            _talep.durum = secilen;
            KartDurumLabel.Text = secilen;
        }
        BottomSheetMenu.IsVisible = false;
        BottomSheetDurumListesi.SelectedItem = null;
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

        //string idStr = await SecureStorage.Default.GetAsync("kullanici_id_gizli");
        //int? aktifAdminId = int.TryParse(idStr, out int id) ? id : (int?)null;

        string idStr = await SecureStorageHelper.GetUserIdAsync();
        int? aktifAdminId = int.TryParse(idStr, out int id) ? id : (int?)null;

        bool basarili = await _apiService.AdminTalepGuncelleAsync(_talep.id, _talep.durum, _talep.tahmini_tutar, aktifAdminId);

        if (basarili)
        {
            await ModernAlertService.ShowInfoAsync("Talep güncellendi.", "Başarılı");
            // Mevcut:
            // MessagingCenter.Send<object>(this, "TalepGuncellendi");

            // Yeni:
            MessagingCenter.Send<object>(this, "TalepGuncellendi");
            MessagingCenter.Send<object, ServisTalebi>(this, "TalepDetayGuncellendi", _talep);
            await Navigation.PopAsync();
        }
        else
        {
            await ModernAlertService.ShowInfoAsync("Güncellenirken bir sorun oluştu.", "Hata");
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

            if (basarili > 0)
            {
                await ModernAlertService.ShowInfoAsync($"{basarili} fotoğraf yüklendi.", "Başarılı");
                // Fotoğraf durumu güncellensin diye sayfayı yenileyebiliriz ama detayda çok gerekli değil.
            }
            else
            {
                // Hiç yüklenemediyse kullanıcıya bilgi vermeyebiliriz veya hata mesajı gösterilebilir.
                // Şimdilik sessiz kalalım.
            }

            _talep.foto_var_mi = true; // anlık güncelleme
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync($"Fotoğraf eklenemedi: {ex.Message}", "Hata");
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
            await ModernAlertService.ShowInfoAsync("Adres panoya kopyalandı.", "Kopyalandı");
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


    // Sayfadan ayrılırken abonelikten çıkmak için
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MessagingCenter.Unsubscribe<object, ServisTalebi>(this, "TalepDetayGuncellendi");
    }
}