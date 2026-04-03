using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace OtoServisApp.Views;

public partial class AdminUserManagementView : ContentPage
{
    private readonly ApiService _apiService;
    private int _gecerliSayfa = 1;
    private int _toplamSayfa = 1;
    private const int SayfaBoyutu = 10;
    
    private bool _yukleniyor = false;
    private bool _uiGuncelleniyor = false; // Switch krizini önleyecek kalkan

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
        await Task.Delay(100);
        await VerileriGetir();
    }

    private async Task VerileriGetir()
    {
        if (_yukleniyor) return;
        
        _yukleniyor = true;
        _uiGuncelleniyor = true; // Kalkanı kaldır: UI güncellenirken Switch'ler işlem yapmasın

        try
        {
            string arama = UserSearchBar.Text ?? string.Empty;
            string url = $"admin/kullanicilar?sayfa={_gecerliSayfa}&sayfa_boyutu={SayfaBoyutu}&arama={arama}";

            var response = await _apiService.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<KullaniciListeResponse>(content, options);

                // Asenkron olarak UI Thread'i bekliyoruz
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Kullanicilar.Clear();
                    if (result?.kullanicilar != null)
                    {
                        foreach (var k in result.kullanicilar)
                        {
                            Kullanicilar.Add(k);
                        }
                    }

                    _toplamSayfa = (result?.toplam_sayfa > 0) ? result.toplam_sayfa : 1;
                    PageInfoLabel.Text = $"Sayfa {_gecerliSayfa} / {_toplamSayfa}";
                    BtnGeri.IsEnabled = _gecerliSayfa > 1;
                    BtnIleri.IsEnabled = _gecerliSayfa < _toplamSayfa;
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Kullanıcı Listesi Hatası", ex.Message, "Tamam");
        }
        finally 
        { 
            // İşlemler tamamen bittiğinde kalkanları indiriyoruz
            _uiGuncelleniyor = false; 
            _yukleniyor = false; 
        }
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
        // UI listeyi doldururken tetikleniyorsa doğrudan iptal et!
        if (_uiGuncelleniyor || _yukleniyor) return;

        if (sender is Switch sw && sw.BindingContext is KullaniciSadelestirilmis k)
        {
            var data = new { aktif_mi = e.Value };
            await _apiService.PutAsync($"admin/kullanicilar/{k.id}/durum", data);
        }
    }
}

// Backend ile %100 uyuşan sınıflar
public class KullaniciListeResponse
{
    public List<KullaniciSadelestirilmis> kullanicilar { get; set; }
    public int toplam_kayit { get; set; }
    public int gecerli_sayfa { get; set; }
    public int toplam_sayfa { get; set; }
}

public class KullaniciSadelestirilmis
{
    public int id { get; set; }
    public string ad_soyad { get; set; }
    public string eposta { get; set; }
    public string rol { get; set; }
    public bool aktif_mi { get; set; }
}