using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Net.Http;
using Microsoft.Maui.Controls;

namespace OtoServisApp.Views;

public partial class AdminUserManagementView : ContentPage
{
    private readonly ApiService _apiService;
    private int _gecerliSayfa = 1;
    private int _toplamSayfa = 1;
    private const int SayfaBoyutu = 10;
    private bool _yukleniyor = false;

    public ObservableCollection<KullaniciSadelestirilmis> Kullanicilar { get; set; } = new ObservableCollection<KullaniciSadelestirilmis>();

    public AdminUserManagementView()
    {
        InitializeComponent();
        _apiService = new ApiService();
        UsersList.ItemsSource = Kullanicilar;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // VerileriGetir();
        await Task.Delay(50);
        await VerileriGetir(); // UI thread dışında çalıştır
    }

    private async Task VerileriGetir()
    {
        try
        {
            // Adım 1
            await MainThread.InvokeOnMainThreadAsync(() => DebugLabel1.Text = "1. Metot başladı");

            if (_yukleniyor) return;
            _yukleniyor = true;

            await MainThread.InvokeOnMainThreadAsync(() => DebugLabel2.Text = "2. _yukleniyor true");

            string arama = UserSearchBar.Text ?? string.Empty;
            string url = $"admin/kullanicilar?sayfa={_gecerliSayfa}&sayfa_boyutu={SayfaBoyutu}&arama={arama}";

            await MainThread.InvokeOnMainThreadAsync(() => DebugLabel3.Text = "3. URL hazır: " + url);

            var response = await _apiService.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            await MainThread.InvokeOnMainThreadAsync(() => DebugLabel1.Text = "4. JSON alındı, uzunluk: " + content.Length);

            if (response.IsSuccessStatusCode)
            {
                // Geçici test verileri
                var fakeList = new List<KullaniciSadelestirilmis>
{
    new KullaniciSadelestirilmis { ad_soyad = "Test 1", eposta = "test1@test.com", rol = "Musteri", aktif_mi = true },
    new KullaniciSadelestirilmis { ad_soyad = "Test 2", eposta = "test2@test.com", rol = "Musteri", aktif_mi = true }
};
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Kullanicilar.Clear();
                    foreach (var k in fakeList) Kullanicilar.Add(k);
                    UsersList.ItemsSource = null;
                    UsersList.ItemsSource = Kullanicilar;
                });

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = await Task.Run(() => JsonSerializer.Deserialize<KullaniciListeResponse>(content, options));

                await MainThread.InvokeOnMainThreadAsync(() => DebugLabel2.Text = $"5. Deserialize tamam, eleman sayısı: {data?.kullanicilar?.Count}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Kullanicilar.Clear();
                    if (data?.kullanicilar != null)
                    {
                        foreach (var k in data.kullanicilar)
                            Kullanicilar.Add(k);
                    }
                    DebugLabel3.Text = $"6. Koleksiyona eklendi, Count: {Kullanicilar.Count}";

                    _toplamSayfa = data?.toplam_sayfa ?? 1;
                    PageInfoLabel.Text = $"Sayfa {_gecerliSayfa} / {_toplamSayfa}";
                    BtnGeri.IsEnabled = _gecerliSayfa > 1;
                    BtnIleri.IsEnabled = _gecerliSayfa < _toplamSayfa;

                    // Zorla yenileme
                    UsersList.ItemsSource = null;
                    UsersList.ItemsSource = Kullanicilar;

                    DebugLabel1.Text = "7. ItemsSource atandı";
                    DebugLabel2.Text = $"8. ItemsSource tipi: {UsersList.ItemsSource?.GetType()}";
                    DebugLabel3.Text = $"9. CollectionView Height: {UsersList.Height}, Width: {UsersList.Width}";
                });
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() => DebugLabel1.Text = "Hata: " + response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() => DebugLabel1.Text = $"HATA: {ex.Message}");
        }
        finally { _yukleniyor = false; }
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _gecerliSayfa = 1;
        await VerileriGetir();
    }

    private async void OnPrevPageClicked(object sender, EventArgs e)
    {
        if (_gecerliSayfa > 1)
        {
            _gecerliSayfa--;
            await VerileriGetir();
        }
    }

    private async void OnNextPageClicked(object sender, EventArgs e)
    {
        if (_gecerliSayfa < _toplamSayfa)
        {
            _gecerliSayfa++;
            await VerileriGetir();
        }
    }

    private async void OnUserStatusToggled(object sender, ToggledEventArgs e)
    {
        if (_yukleniyor) return;
        if (sender is Switch sw && sw.BindingContext is KullaniciSadelestirilmis k)
        {
            var data = new { aktif_mi = e.Value };
            await _apiService.PutAsync($"admin/kullanicilar/{k.id}/durum", data);
        }
    }

    public class KullaniciListeResponse
    {
        public List<KullaniciSadelestirilmis> kullanicilar { get; set; }
        public int toplam_kayit { get; set; }
        public int gecerli_sayfa { get; set; }
        public int toplam_sayfa { get; set; }
    }
}

// MODEL YAPILARI
public class KullaniciSadelestirilmis
{
    public int id { get; set; }
    public string ad_soyad { get; set; }
    public string eposta { get; set; }
    public string rol { get; set; }
    public bool aktif_mi { get; set; }
}