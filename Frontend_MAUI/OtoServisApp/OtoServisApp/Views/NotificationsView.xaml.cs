using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Collections.ObjectModel;

namespace OtoServisApp.Views;

public partial class NotificationsView : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isSelectAllUpdating = false;

    private int _sayfaBoyutu = 20;
    private int _mevcutSayfa = 1;
    private int _toplamSayfa = 1;
    private int _toplamKayit = 0;
    private bool _yukleniyor = false;
    private bool _ilkYukleme = true;

    public ObservableCollection<BildirimResponse> Bildirimler { get; set; } = new();

    public NotificationsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
        NotificationList.ItemsSource = Bildirimler;
        BtnDeleteSelectedLabel.IsVisible = false;
        GuncelleButonDurumlari();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_ilkYukleme)
        {
            LoadingOverlay.IsVisible = true;
            LoadingTitle.Text = "Bildirimler Yükleniyor...";
            await Task.Delay(5);

            try
            {
                await BildirimleriYukle(sayfa: 1);
                _ilkYukleme = false;
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
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        string idStr = await SecureStorage.Default.GetAsync("kullanici_id_gizli");
        if (int.TryParse(idStr, out int id))
            return id;
        return null;
    }

    private async Task BildirimleriYukle(int sayfa)
    {
        if (_yukleniyor) return;
        _yukleniyor = true;

        int? aktifKullaniciId = await GetCurrentUserIdAsync();
        if (aktifKullaniciId == null)
        {
            Bildirimler.Clear();
            _yukleniyor = false;
            return;
        }

        int skip = (sayfa - 1) * _sayfaBoyutu;

        try
        {
            var (yeniBildirimler, toplamKayit) = await _apiService.KullaniciBildirimleriniSayfaliGetirAsync(
                aktifKullaniciId.Value,
                skip: skip,
                limit: _sayfaBoyutu
            );

            _toplamKayit = toplamKayit;
            _toplamSayfa = (int)Math.Ceiling((double)toplamKayit / _sayfaBoyutu);
            if (_toplamSayfa == 0) _toplamSayfa = 1;
            _mevcutSayfa = sayfa;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Bildirimler.Clear();
                foreach (var item in yeniBildirimler)
                {
                    item.IsSelected = false;
                    Bildirimler.Add(item);
                }

                ToplamBildirimLabel.Text = $"{toplamKayit} bildirim";
                SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
                GuncelleButonDurumlari();

                // Hepsini seç checkbox'ını sıfırla
                _isSelectAllUpdating = true;
                ChkSelectAll.IsChecked = false;
                _isSelectAllUpdating = false;
                BtnDeleteSelectedLabel.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Veriler yüklenirken bir sorun oluştu.", "Tamam");
            System.Diagnostics.Debug.WriteLine($"Bildirim yükleme hatası: {ex.Message}");
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
        {
            await BildirimleriYukle(_mevcutSayfa - 1);
        }
    }

    private async void OnSonrakiTapped(object sender, TappedEventArgs e)
    {
        if (_yukleniyor) return;
        if (_mevcutSayfa < _toplamSayfa)
        {
            await BildirimleriYukle(_mevcutSayfa + 1);
        }
    }

    private async void OnThresholdReached(object sender, EventArgs e)
    {
        if (!_yukleniyor && _mevcutSayfa < _toplamSayfa)
        {
            await BildirimleriYukle(_mevcutSayfa + 1);
        }
    }

    private async void OnNotificationTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var bildirim = border?.BindingContext as BildirimResponse;

        if (bildirim != null && !bildirim.okundu_mu)
        {
            bool basarili = await _apiService.BildirimOkunduIsaretleAsync(bildirim.id);
            if (basarili)
            {
                // UI'ı anında güncelle, tüm listeyi yeniden yükleme!
                bildirim.okundu_mu = true;
            }
        }
    }

    private async void OnSingleDeleteInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItemView;
        var bildirim = swipeItem?.BindingContext as BildirimResponse;

        if (bildirim != null)
        {
            bool onay = await DisplayAlert("Onay", "Bu bildirimi silmek istiyor musunuz?", "Evet", "Vazgeç");
            if (onay)
            {
                bool silindi = await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
                if (silindi)
                {
                    Bildirimler.Remove(bildirim);
                    _toplamKayit--;
                    ToplamBildirimLabel.Text = $"{_toplamKayit} bildirim";

                    // Toplam sayfa değişebilir, güncelle
                    _toplamSayfa = (int)Math.Ceiling((double)_toplamKayit / _sayfaBoyutu);
                    if (_toplamSayfa == 0) _toplamSayfa = 1;
                    if (_mevcutSayfa > _toplamSayfa)
                    {
                        _mevcutSayfa = _toplamSayfa;
                        await BildirimleriYukle(_mevcutSayfa);
                    }
                    else
                    {
                        SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
                        GuncelleButonDurumlari();
                    }

                    GuncelleSeciliDurum();
                }
            }
        }
    }

    private void OnItemCheckChanged(object sender, CheckedChangedEventArgs e)
    {
        GuncelleSeciliDurum();
    }

    private void OnSelectAllCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isSelectAllUpdating) return;

        bool yeniDeger = e.Value;
        foreach (var item in Bildirimler)
        {
            item.IsSelected = yeniDeger;
        }
        BtnDeleteSelectedLabel.IsVisible = yeniDeger && Bildirimler.Any();
    }

    private void GuncelleSeciliDurum()
    {
        bool enAzBirSecili = Bildirimler.Any(b => b.IsSelected);
        BtnDeleteSelectedLabel.IsVisible = enAzBirSecili;

        bool hepsiSecili = Bildirimler.Any() && Bildirimler.All(b => b.IsSelected);
        if (ChkSelectAll.IsChecked != hepsiSecili)
        {
            _isSelectAllUpdating = true;
            ChkSelectAll.IsChecked = hepsiSecili;
            _isSelectAllUpdating = false;
        }
    }

    private async void OnDeleteSelectedTapped(object sender, TappedEventArgs e)
    {
        var secilenler = Bildirimler.Where(b => b.IsSelected).ToList();
        if (!secilenler.Any()) return;

        bool onay = await DisplayAlert("Onay", $"{secilenler.Count} adet bildirimi silmek istiyor musunuz?", "Evet", "İptal");
        if (!onay) return;

        LoadingOverlay.IsVisible = true;
        LoadingTitle.Text = "Siliniyor...";

        try
        {
            foreach (var bildirim in secilenler)
            {
                await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
            }

            // UI'dan kaldır
            foreach (var bildirim in secilenler)
            {
                Bildirimler.Remove(bildirim);
            }

            _toplamKayit -= secilenler.Count;
            ToplamBildirimLabel.Text = $"{_toplamKayit} bildirim";
            _toplamSayfa = (int)Math.Ceiling((double)_toplamKayit / _sayfaBoyutu);
            if (_toplamSayfa == 0) _toplamSayfa = 1;

            // Eğer mevcut sayfada hiç kayıt kalmadıysa bir önceki sayfaya geç
            if (Bildirimler.Count == 0 && _mevcutSayfa > 1)
            {
                await BildirimleriYukle(_mevcutSayfa - 1);
            }
            else
            {
                SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
                GuncelleButonDurumlari();
            }

            BtnDeleteSelectedLabel.IsVisible = false;
            _isSelectAllUpdating = true;
            ChkSelectAll.IsChecked = false;
            _isSelectAllUpdating = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Silme işlemi sırasında bir sorun oluştu.", "Tamam");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }
}