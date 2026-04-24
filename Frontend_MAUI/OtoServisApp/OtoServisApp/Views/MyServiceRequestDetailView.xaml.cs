using System.Diagnostics;
using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class MyServiceRequestDetailView : ContentPage
{
    private ServisTalebi _talep;
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;

    private List<Hizmet> _tumHizmetler;
    private List<Marka> _tumMarkalar;

    public MyServiceRequestDetailView(ServisTalebi talep, Kullanici kullanici)
    {
        InitializeComponent();
        _talep = talep;
        _aktifKullanici = kullanici;
        _apiService = new ApiService();

        BindingContext = _talep;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Referans verileri önbelleğe al
            if (_tumHizmetler == null)
                _tumHizmetler = await _apiService.HizmetleriGetirAsync();
            if (_tumMarkalar == null)
                _tumMarkalar = await _apiService.MarkalariGetirAsync();

            // Güncel talebi API'den çek
            var guncelTalep = await _apiService.TalepGetirAsync(_talep.id);
            if (guncelTalep != null)
            {
                _talep = guncelTalep;

                // Eksik alanları zenginleştir (hizmet adı, araç adı)
                await TalebiZenginlestir(_talep);

                // UI'ı tamamen tazele
                BindingContext = null;
                BindingContext = _talep;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Detay güncellenirken hata: {ex.Message}");
        }
    }

    private async Task TalebiZenginlestir(ServisTalebi talep)
    {
        // Hizmet adı
        var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
        if (hizmet != null)
            talep.hizmet_adi = hizmet.ad;

        // Araç adı
        var arac = await _apiService.AracGetirAsync(talep.arac_id);
        if (arac != null)
        {
            string gosterimAd = "";
            if (arac.marka_id != null && arac.model_id != null && _tumMarkalar != null)
            {
                var marka = _tumMarkalar.FirstOrDefault(m => m.id == arac.marka_id);
                var model = marka?.modeller?.FirstOrDefault(m => m.id == arac.model_id);
                if (marka != null && model != null)
                    gosterimAd = $"{marka.ad} {model.ad}";
            }
            if (string.IsNullOrWhiteSpace(gosterimAd) && !string.IsNullOrWhiteSpace(arac.ozel_marka))
                gosterimAd = $"{arac.ozel_marka} {arac.ozel_model}";
            if (string.IsNullOrWhiteSpace(gosterimAd))
                gosterimAd = $"Araç ID: {arac.id}";
            talep.arac_adi_tam = gosterimAd;
        }
        else
        {
            talep.arac_adi_tam = "Sistemden Silinmiş Araç";
        }
    }

    // --- Aşağıdaki metotlar aynen kalacak ---
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
}