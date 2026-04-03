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

        try
        {
            string arama = UserSearchBar.Text ?? string.Empty;
            string url = $"admin/kullanicilar?sayfa={_gecerliSayfa}&sayfa_boyutu={SayfaBoyutu}&arama={arama}";

            var response = await _apiService.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Kullanicilar.Clear();

                    if (root.TryGetProperty("kullanicilar", out var liste))
                    {
                        foreach (var k in liste.EnumerateArray())
                        {
                            try
                            {
                                // Tek tek alanları kontrol ederek ekleyelim
                                var yeniKullanici = new KullaniciSadelestirilmis
                                {
                                    id = k.GetProperty("id").GetInt32(),
                                    ad_soyad = k.TryGetProperty("ad_soyad", out var ad) ? ad.GetString() : "İsimsiz",
                                    eposta = k.TryGetProperty("eposta", out var ep) ? ep.GetString() : "-",
                                    rol = k.TryGetProperty("rol", out var rl) ? rl.GetString() : "Müşteri",
                                    aktif_mi = k.TryGetProperty("aktif_mi", out var ak) && ak.GetBoolean()
                                };
                                Kullanicilar.Add(yeniKullanici);
                            }
                            catch (Exception itemEx)
                            {
                                // Eğer bir satırda hata varsa sessizce logla veya göster
                                Console.WriteLine("Satır Okuma Hatası: " + itemEx.Message);
                            }
                        }
                    }

                    if (root.TryGetProperty("toplam_sayfa", out var ts))
                        _toplamSayfa = ts.GetInt32();

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