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
    private bool _uiGuncelleniyor = false;

    public ObservableCollection<KullaniciSadelestirilmis> Kullanicilar { get; set; } = new ObservableCollection<KullaniciSadelestirilmis>();

    public AdminUserManagementView()
    {
        InitializeComponent();
        _apiService = new ApiService();
        // Ekrana listeyi bağladık
        UsersList.ItemsSource = Kullanicilar;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(10); // Çizim için kısa bir bekleme
        await VerileriGetir();
    }

    private async Task VerileriGetir()
    {
        if (_yukleniyor) return;
        _yukleniyor = true;
        _uiGuncelleniyor = true;

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

                // Ana Thread üzerinde güvenli atama işlemi
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Kullanicilar.Clear();

                    /*if (result?.kullanicilar != null)
                    {
                        foreach (var k in result.kullanicilar)
                        {
                            Kullanicilar.Add(k);
                        }
                    }*/
                    // en üstte ObservableCollection ı çağırdığımız için bunu da yaz demişti kullanmadım. şundan istemiş; foreach ile ekleme sorun değil, 5 eleman için fark etmez. Ama ileride çok sayıda kullanıcı olursa performans için AddRange kullanmak daha iyidir. Üsttekini kapatıp alttaki eklemeyi yaptım.
                    if (result?.kullanicilar != null)
                        Kullanicilar.AddRange(result.kullanicilar);

                    _toplamSayfa = (result?.toplam_sayfa > 0) ? result.toplam_sayfa : 1;
                    PageInfoLabel.Text = $"Sayfa {_gecerliSayfa} / {_toplamSayfa}";

                    BtnGeri.IsEnabled = _gecerliSayfa > 1;
                    BtnIleri.IsEnabled = _gecerliSayfa < _toplamSayfa;

                    _uiGuncelleniyor = false; // Switch kalkanı indirilir
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                DisplayAlert("Kullanıcı Listesi Hatası", ex.Message, "Tamam");
            });
            _uiGuncelleniyor = false;
        }
        finally
        {
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

    // YENİ EKLENEN GÜNCELLEME METODU
    private async void OnUpdateUserClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is not KullaniciSadelestirilmis k) return;

        bool onayla = await DisplayAlert("Güncelleme Onayı", $"{k.ad_soyad} kullanıcısının bilgilerini kaydetmek istiyor musunuz?", "Evet", "Hayır");
        if (!onayla) return;

        try
        {
            // İsim ve durumu paketle
            var payload = new
            {
                ad_soyad = k.ad_soyad,
                aktif_mi = k.aktif_mi
            };

            // Yeni oluşturduğumuz Python endpoint'ine istek at
            var res = await _apiService.PutAsync($"admin/kullanicilar/{k.id}/guncelle", payload);

            if (res.IsSuccessStatusCode)
            {
                await DisplayAlert("Başarılı", "Kullanıcı başarıyla güncellendi.", "Tamam");
                await VerileriGetir(); // Listeyi yenile ki silinme_tarihi UI'a yansısın
            }
            else
            {
                await DisplayAlert("Hata", "Güncelleme başarısız oldu.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }
} // Sınıf Kapanışı

// JSON verisiyle Birebir Eşleşen YENİ Modeller
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
    public string telefon { get; set; }
    public string rol { get; set; }
    public bool aktif_mi { get; set; }

    // YENİ EKLENEN TARİH ALANLARI
    public string kayit_tarihi { get; set; }
    public string silinme_tarihi { get; set; }

    // Silinmişse (Pasifse) UI'da silinme tarihini göstermek için pratik bir tetikleyici
    public bool IsSilinmis => !aktif_mi && silinme_tarihi != "-";
}