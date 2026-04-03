using OtoServisApp.Services;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace OtoServisApp.Views;

public partial class AdminUserTrackingView : ContentPage
{
    private readonly ApiService _api;
    private int _page = 1;
    private int _toplamSayfa = 1;

    // Ekrana bağlanacak kesin tipli liste
    public ObservableCollection<TakipMusteri> Musteriler { get; set; } = new ObservableCollection<TakipMusteri>();

    public AdminUserTrackingView()
    {
        InitializeComponent();
        _api = new ApiService();
        TrackingList.ItemsSource = Musteriler;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(100);
        await Yukle();
    }

    private async Task Yukle()
    {
        try
        {
            var res = await _api.GetAsync($"admin/kullanici-takip?sayfa={_page}");
            var content = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<TakipResponse>(content, options);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Musteriler.Clear();
                    if (data?.liste != null)
                    {
                        foreach (var m in data.liste) Musteriler.Add(m);
                        _toplamSayfa = data.toplam_sayfa > 0 ? data.toplam_sayfa : 1;
                        PageInfo.Text = $"{_page} / {_toplamSayfa}";
                        BtnGeri.IsEnabled = _page > 1;
                        BtnIleri.IsEnabled = _page < _toplamSayfa;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Dönüştürme Hatası", $"Detay: {ex.Message}", "Tamam");
        }
    }

    private async void OnSendReminderClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is not TakipMusteri m) return;

        if (!m.mail_istiyor_mu)
        {
            await DisplayAlert("Uyarı", "Bu kullanıcı e-posta bildirimlerini kapatmıştır (KVKK). Hatırlatma gönderilemez.", "Tamam");
            return;
        }

        bool onayla = await DisplayAlert("Hatırlatma", $"{m.ad_soyad} kullanıcısına hatırlatma gönderilsin mi?", "Gönder", "İptal");
        if (!onayla) return;

        try
        {
            var body = new { ozel_mesaj = "Sizi uzun zamandır aramızda göremedik. Bir çayımızı içmeye bekliyoruz." };
            var res = await _api.PostAsync($"admin/kullanici-takip/{m.id}/hatirlatma-gonder", body);

            if (res.IsSuccessStatusCode)
                await DisplayAlert("Başarılı", "Hatırlatma başarıyla gönderildi.", "Tamam");
            else
                await DisplayAlert("Uyarı", "Gönderim yapılamadı (KVKK veya sistem hatası).", "Tamam");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "İşlem sırasında hata: " + ex.Message, "Tamam");
        }
    }

    private async void OnPrev(object sender, EventArgs e)
    {
        if (_page > 1)
        {
            _page--;
            await Yukle();
        }
    }

    private async void OnNext(object sender, EventArgs e)
    {
        if (_page < _toplamSayfa)
        {
            _page++;
            await Yukle();
        }
    }
}

public class TakipMusteri
{
    public int id { get; set; }
    public string ad_soyad { get; set; }
    public string eposta { get; set; }
    public string son_giris_tarihi { get; set; }   // formatlı string
    public int? kac_gun_oldu { get; set; }
    public bool mail_istiyor_mu { get; set; }

    // Görüntüleme metinleri
    public string KacGunText => kac_gun_oldu.HasValue ? $"{kac_gun_oldu} gündür girmiyor" : "Hiç giriş yapmamış";
    public string SonGirisText => string.IsNullOrEmpty(son_giris_tarihi) ? "Hiç giriş yok" : son_giris_tarihi;
    public string MailIzinDurum => mail_istiyor_mu ? "✅ Mail izni var" : "❌ Mail izni yok";
    public Color MailIzinRengi => mail_istiyor_mu ? Colors.Green : Colors.Red;
}

public class TakipResponse
{
    public List<TakipMusteri> liste { get; set; }
    public int toplam_sayfa { get; set; }
}