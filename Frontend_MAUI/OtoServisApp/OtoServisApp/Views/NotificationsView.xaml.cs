using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Collections.ObjectModel;

namespace OtoServisApp.Views;

public partial class NotificationsView : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isUpdatingSelection = false;

    public ObservableCollection<BildirimResponse> Bildirimler { get; set; } = new();

    private int _sayfaBoyutu = 20;
    private int _mevcutSayfa = 1;
    private int _toplamSayfa = 1;
    private int _toplamKayit = 0;
    private bool _yukleniyor = false;

    private int? _aktifKullaniciId;

    public NotificationsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
        NotificationList.ItemsSource = Bildirimler;
        GuncelleButonDurumlari();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _aktifKullaniciId = await GetCurrentUserIdAsync();
        if (_aktifKullaniciId == null) return;

        LoadingOverlay.IsVisible = true;
        LoadingTitle.Text = "Bildirimler Yükleniyor...";
        await Task.Delay(5);

        try
        {
            await BildirimleriYukle(sayfa: 1);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Bildirimler yüklenirken bir sorun oluştu.", "Tamam");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        string idStr = await SecureStorage.Default.GetAsync("kullanici_id_gizli");
        return int.TryParse(idStr, out int id) ? id : null;
    }

    private async Task BildirimleriYukle(int sayfa)
    {
        if (_yukleniyor) return;
        _yukleniyor = true;

        int skip = (sayfa - 1) * _sayfaBoyutu;

        try
        {
            var (yeniBildirimler, toplamKayit) = await _apiService.BildirimleriSayfaliGetirAsync(
                _aktifKullaniciId.Value,
                skip: skip,
                limit: _sayfaBoyutu
            );

            _toplamKayit = toplamKayit;
            _toplamSayfa = (int)Math.Ceiling((double)toplamKayit / _sayfaBoyutu);
            if (_toplamSayfa == 0) _toplamSayfa = 1;
            _mevcutSayfa = sayfa;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ToplamBildirimLabel.Text = $"{toplamKayit} bildirim";
                SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
                GuncelleButonDurumlari();
            });

            Bildirimler.Clear();
            if (yeniBildirimler != null)
            {
                foreach (var b in yeniBildirimler)
                {
                    b.IsSelected = false;
                    Bildirimler.Add(b);
                }
            }

            // Seçili öğe yoksa sil butonunu gizle ve hepsini seç checkbox'ını kaldır
            _isUpdatingSelection = true;
            ChkSelectAll.IsChecked = false;
            _isUpdatingSelection = false;
            DeleteSelectedBorder.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Bildirimler yüklenirken bir sorun oluştu.", "Tamam");
        }
        finally
        {
            _yukleniyor = false;
        }
    }

    private void GuncelleButonDurumlari()
    {
        BtnOncekiLabel.Opacity = _mevcutSayfa > 1 ? 1.0 : 0.5;
        BtnSonrakiLabel.Opacity = _mevcutSayfa < _toplamSayfa ? 1.0 : 0.5;
    }

    private async void OnOncekiTapped(object sender, TappedEventArgs e)
    {
        if (_yukleniyor) return;
        if (_mevcutSayfa > 1)
            await BildirimleriYukle(_mevcutSayfa - 1);
    }

    private async void OnSonrakiTapped(object sender, TappedEventArgs e)
    {
        if (_yukleniyor) return;
        if (_mevcutSayfa < _toplamSayfa)
            await BildirimleriYukle(_mevcutSayfa + 1);
    }

    private async void OnNotificationTapped(object sender, TappedEventArgs e)
    {
        var bildirim = e.Parameter as BildirimResponse;
        if (bildirim == null || bildirim.okundu_mu) return;

        // UI'ı hemen güncelle (okundu olarak işaretle)
        bildirim.okundu_mu = true;

        // Arka planda API'ye bildir
        _ = _apiService.BildirimOkunduIsaretleAsync(bildirim.id);
    }

    private async void OnSingleDeleteInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItemView;
        var bildirim = swipeItem?.BindingContext as BildirimResponse;
        if (bildirim == null) return;

        bool onay = await DisplayAlert("Onay", "Bu bildirimi silmek istiyor musunuz?", "Evet", "Vazgeç");
        if (!onay) return;

        LoadingOverlay.IsVisible = true;
        LoadingTitle.Text = "Siliniyor...";

        bool basarili = await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
        if (basarili)
        {
            Bildirimler.Remove(bildirim);
            _toplamKayit--;
            ToplamBildirimLabel.Text = $"{_toplamKayit} bildirim";

            // Seçim durumlarını sıfırla
            _isUpdatingSelection = true;
            ChkSelectAll.IsChecked = false;
            _isUpdatingSelection = false;
            DeleteSelectedBorder.IsVisible = false;

            if (Bildirimler.Count == 0 && _mevcutSayfa > 1)
            {
                await BildirimleriYukle(_mevcutSayfa - 1);
            }
        }

        LoadingOverlay.IsVisible = false;
    }

    private void OnItemCheckChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;

        // Sil butonu görünürlüğünü güncelle
        DeleteSelectedBorder.IsVisible = Bildirimler.Any(b => b.IsSelected);

        // Hepsini seç checkbox'ını güncelle
        bool hepsiSecili = Bildirimler.Any() && Bildirimler.All(b => b.IsSelected);
        if (ChkSelectAll.IsChecked != hepsiSecili)
        {
            _isUpdatingSelection = true;
            ChkSelectAll.IsChecked = hepsiSecili;
            _isUpdatingSelection = false;
        }
    }

    private void OnSelectAllCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;

        _isUpdatingSelection = true;
        bool yeniDeger = e.Value;
        foreach (var item in Bildirimler)
            item.IsSelected = yeniDeger;

        DeleteSelectedBorder.IsVisible = yeniDeger && Bildirimler.Any();
        _isUpdatingSelection = false;
    }

    private async void OnDeleteSelectedTapped(object sender, TappedEventArgs e)
    {
        var secilenler = Bildirimler.Where(b => b.IsSelected).ToList();
        if (!secilenler.Any()) return;

        bool onay = await DisplayAlert("Onay", $"{secilenler.Count} adet bildirimi silmek istiyor musunuz?", "Evet", "İptal");
        if (!onay) return;

        LoadingOverlay.IsVisible = true;
        LoadingTitle.Text = "Seçilenler Siliniyor...";

        foreach (var bildirim in secilenler)
        {
            bool basarili = await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
            if (basarili)
                Bildirimler.Remove(bildirim);
        }

        _toplamKayit = Math.Max(0, _toplamKayit - secilenler.Count);
        ToplamBildirimLabel.Text = $"{_toplamKayit} bildirim";

        LoadingOverlay.IsVisible = false;

        _isUpdatingSelection = true;
        ChkSelectAll.IsChecked = false;
        DeleteSelectedBorder.IsVisible = false;
        _isUpdatingSelection = false;

        if (Bildirimler.Count == 0 && _mevcutSayfa > 1)
        {
            await BildirimleriYukle(_mevcutSayfa - 1);
        }
    }
}