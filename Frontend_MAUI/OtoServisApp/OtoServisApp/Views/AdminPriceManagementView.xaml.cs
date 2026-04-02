using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Text.Json;
using System.Text;

namespace OtoServisApp.Views;

public partial class AdminPriceManagementView : ContentPage
{
    private readonly ApiService _apiService;

    // Arama yaparken orijinal listeyi kaybetmemek için burada yedekte tutuyoruz
    private List<Hizmet> _tumHizmetler = new List<Hizmet>();

    public AdminPriceManagementView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // İlk çalışan koddaki hayat kurtaran gecikme (UI'ın kendine gelmesi için)
        await Task.Delay(20);

        // Sayfa açıldığında arama kutusunu temizle
        if (HizmetSearchEntry != null)
        {
            HizmetSearchEntry.Text = string.Empty;
        }

        await HizmetleriYukle();
    }

    private async Task HizmetleriYukle()
    {
        try
        {
            // ESKİ KOD (Hata veren): var response = await _apiService.GetAsync("hizmetler");
            // YENİ REVİZE: Sistemde zaten tanımlı olan doğru route'u çağırıyoruz.
            var response = await _apiService.GetAsync("referanslar/hizmetler/");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _tumHizmetler = JsonSerializer.Deserialize<List<Hizmet>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // İlk açılışta listeyi direkt ekrana basıyoruz
                HizmetlerListesi.ItemsSource = _tumHizmetler;
            }
            else
            {
                // Eğer bir hata alırsak sessizce durmasın, ekranda görelim
                await DisplayAlert("API Hatası", $"Veriler alınamadı. Durum: {response.StatusCode}", "Tamam");
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Hata", "Hizmetler yüklenirken bir hata oluştu.", "Tamam");
        }
    }

    // --- ARAMA KUTUSUNA YAZDIKÇA ÇALIŞACAK METOT ---
    private void OnHizmetAraTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_tumHizmetler == null || !_tumHizmetler.Any()) return;

        var aramaKelimesi = e.NewTextValue?.ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(aramaKelimesi))
        {
            // Kutucuk boşaldığında tam listeyi geri ver
            HizmetlerListesi.ItemsSource = _tumHizmetler;
        }
        else
        {
            // İçinde aranan harfler geçen hizmetleri süz ve ekrana at
            var filtrelenmisListe = _tumHizmetler
                .Where(h => !string.IsNullOrEmpty(h.ad) && h.ad.ToLowerInvariant().Contains(aramaKelimesi))
                .ToList();

            HizmetlerListesi.ItemsSource = filtrelenmisListe;
        }
    }

    private async void OnFiyatGuncelleClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var secilenHizmet = button?.CommandParameter as Hizmet;

        if (secilenHizmet == null) return;

        decimal yeniFiyat = secilenHizmet.varsayilan_fiyat;

        bool onay = await DisplayAlert("Onay", $"'{secilenHizmet.ad}' hizmetinin fiyatını {yeniFiyat} ₺ olarak güncellemek istediğinize emin misiniz?", "Evet", "Hayır");
        if (!onay) return;

        try
        {
            var data = new { yeni_fiyat = yeniFiyat };
            // Fiyat güncelleme adresi admin/hizmetler/... olarak kalmaya devam ediyor (FastAPI tarafında burası doğru yapılandırılmıştı)
            var response = await _apiService.PutAsync($"admin/hizmetler/{secilenHizmet.id}/fiyat", data);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Başarılı", "Fiyat başarıyla güncellendi.", "Tamam");

                // Güncellemeden sonra arama kutusunu temizleyip listeyi tazele
                if (HizmetSearchEntry != null)
                {
                    HizmetSearchEntry.Text = string.Empty;
                }
                await HizmetleriYukle();
            }
            else
            {
                await DisplayAlert("Hata", "Fiyat güncellenemedi.", "Tamam");
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Hata", "Bağlantı hatası oluştu.", "Tamam");
        }
    }
}