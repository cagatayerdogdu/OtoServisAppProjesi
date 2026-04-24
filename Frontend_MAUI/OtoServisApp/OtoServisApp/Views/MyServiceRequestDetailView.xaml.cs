using System.Diagnostics;
using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class MyServiceRequestDetailView : ContentPage
{
    private ServisTalebi _talep;
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;

    public MyServiceRequestDetailView(ServisTalebi talep, Kullanici kullanici)
    {
        InitializeComponent();
        _talep = talep;
        _aktifKullanici = kullanici;
        _apiService = new ApiService();

        BindingContext = _talep;

        // Detay güncelleme mesajını dinle
        MessagingCenter.Subscribe<object, ServisTalebi>(this, "TalepDetayGuncellendi", (sender, guncelTalep) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _talep = guncelTalep;
                BindingContext = _talep;
            });
        });
    }

    private async void OnViewPhotosTapped(object sender, TappedEventArgs e)
    {
        if (_talep != null)
            await Navigation.PushAsync(new ViewPhotosView(_talep));
    }

    private async void OnEditTapped(object sender, TappedEventArgs e)
    {
        if (_talep != null)
        {
            if (_talep.durum == "Tamamlandı" || _talep.durum == "İptal Edildi")
            {
                await ModernAlertService.ShowInfoAsync("Bu talep sonlandığı için üzerinde değişiklik yapılamaz.", "İşlem Engellendi");
                return;
            }
            await Navigation.PushAsync(new EditServiceRequestView(_talep, _aktifKullanici));
        }
    }

    private async void OnCancelTapped(object sender, TappedEventArgs e)
    {
        if (_talep != null)
        {
            if (_talep.durum != "Bekliyor")
            {
                await ModernAlertService.ShowInfoAsync("Sadece 'Bekliyor' durumundaki talepler iptal edilebilir.", "İşlem Engellendi");
                return;
            }

            bool? eminMisinSonuc = await ModernAlertService.ShowDeleteConfirmationAsync("Bu servis talebini iptal etmek (silmek) istediğinize emin misiniz?", "Onay");
            bool eminMisin = eminMisinSonuc == true;
            if (eminMisin)
            {
                LoadingOverlay.IsVisible = true;
                LoadingTitle.Text = "İptal Ediliyor...";
                await Task.Delay(10);

                try
                {
                    bool basarili = await _apiService.ServisTalebiSilAsync(_talep.id);
                    if (basarili)
                    {
                        await ModernAlertService.ShowInfoAsync("Talebiniz iptal edildi.", "Başarılı");
                        MessagingCenter.Send<object>(this, "TalepGuncellendi");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await ModernAlertService.ShowInfoAsync("Talebiniz iptal edilirken bir sorun oluştu.", "Hata");
                    }
                }
                finally
                {
                    LoadingOverlay.IsVisible = false;
                }
            }
        }
    }

    // Sayfadan ayrılırken abonelikten çıkmak için
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MessagingCenter.Unsubscribe<object, ServisTalebi>(this, "TalepDetayGuncellendi");
    }
}