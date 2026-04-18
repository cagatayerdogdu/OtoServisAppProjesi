using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminPastRequestsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<ServisTalebi> _orijinalTalepler;

    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Tamamlandı", "İptal Edildi" };
    private string _secilenDurum = "Tümü";
    private string _aktifArama = "";

    private int _sayfaBoyutu = 20;
    private int _mevcutSayfa = 1;
    private int _toplamSayfa = 1;
    private int _toplamKayit = 0;
    private bool _yukleniyor = false;
    private bool _ilkYukleme = true;

    private CancellationTokenSource _aramaCts;

    public AdminPastRequestsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
        GuncelleButonDurumlari();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_ilkYukleme)
        {
            LoadingOverlay.IsVisible = true;
            LoadingTitle.Text = "Geçmiş Talepler Yükleniyor...";
            await Task.Delay(5);

            try
            {
                if (DurumListesi != null && DurumListesi.ItemsSource == null)
                    DurumListesi.ItemsSource = _durumFiltreleri;

                await TalepleriYukle(sayfa: 1);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Veriler yüklenirken bir sorun oluştu.", "Tamam");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                _ilkYukleme = false;
            }
        }
    }

    private async Task TalepleriYukle(int sayfa)
    {
        if (_yukleniyor) return;
        _yukleniyor = true;

        int skip = (sayfa - 1) * _sayfaBoyutu;

        try
        {
            var (yeniTalepler, toplamKayit) = await _apiService.AdminGecmisTalepleriSayfaliGetirAsync(
                skip: skip,
                limit: _sayfaBoyutu,
                durum: _secilenDurum,
                arama: _aktifArama
            );

            _toplamKayit = toplamKayit;
            _toplamSayfa = (int)Math.Ceiling((double)toplamKayit / _sayfaBoyutu);
            if (_toplamSayfa == 0) _toplamSayfa = 1;
            _mevcutSayfa = sayfa;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ToplamTalepLabel.Text = $"{toplamKayit} talep";
                SayfaBilgiLabel.Text = $"Sayfa {_mevcutSayfa} / {_toplamSayfa}";
                GuncelleButonDurumlari();
            });

            _orijinalTalepler = yeniTalepler ?? new List<ServisTalebi>();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PastRequestsList.ItemsSource = _orijinalTalepler;
                PastRequestsList.ScrollTo(0);
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Veriler yüklenirken bir sorun oluştu.", "Tamam");
            System.Diagnostics.Debug.WriteLine($"Geçmiş talepler yükleme hatası: {ex.Message}");
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
            await TalepleriYukle(_mevcutSayfa - 1);
    }

    private async void OnSonrakiTapped(object sender, TappedEventArgs e)
    {
        if (_yukleniyor) return;
        if (_mevcutSayfa < _toplamSayfa)
            await TalepleriYukle(_mevcutSayfa + 1);
    }

    private void OnFiltreDurumKutusuAcKapatTapped(object sender, TappedEventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
    }

    private void OnFiltreDurumSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            _secilenDurum = secilen;
            SecilenDurumLabel.Text = secilen;
            DurumSecimKutusu.IsVisible = false;
            DurumListesi.SelectedItem = null;
            _ = TalepleriYukle(sayfa: 1);
        }
    }

    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        _aramaCts?.Cancel();
        _aramaCts = new CancellationTokenSource();

        Task.Delay(300, _aramaCts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                _aktifArama = AramaBar.Text;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await TalepleriYukle(sayfa: 1);
                });
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _aramaCts?.Cancel();
    }
}