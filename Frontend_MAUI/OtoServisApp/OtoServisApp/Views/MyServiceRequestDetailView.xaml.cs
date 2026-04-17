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
                await DisplayAlert("İşlem Engellendi", "Bu talep sonlandığı için üzerinde değişiklik yapılamaz.", "Tamam");
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
                await DisplayAlert("İşlem Engellendi", "Sadece 'Bekliyor' durumundaki talepler iptal edilebilir.", "Tamam");
                return;
            }

            bool eminMisin = await DisplayAlert("Onay", "Bu servis talebini iptal etmek (silmek) istediğinize emin misiniz?", "Evet, İptal Et", "Vazgeç");
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
                        await DisplayAlert("Başarılı", "Talebiniz iptal edildi.", "Tamam");
                        MessagingCenter.Send<object>(this, "TalepGuncellendi");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Hata", "Talebiniz iptal edilirken bir sorun oluştu.", "Tamam");
                    }
                }
                finally
                {
                    LoadingOverlay.IsVisible = false;
                }
            }
        }
    }
}