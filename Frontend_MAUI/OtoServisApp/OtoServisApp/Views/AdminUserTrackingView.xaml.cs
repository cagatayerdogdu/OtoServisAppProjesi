using OtoServisApp.Services;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

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
        //await Task.Delay(20);
        await Yukle();
    }

    private async Task Yukle()
    {
        try
        {
            var res = await _api.GetAsync($"admin/kullanici-takip?sayfa={_page}");
            var content = await res.Content.ReadAsStringAsync();

            // DEBUG: JSON'u konsola yazdır
            System.Diagnostics.Debug.WriteLine("Gelen JSON: " + content);

            if (res.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = await Task.Run(() => JsonSerializer.Deserialize<TakipResponse>(content, options));

                // DEBUG: Liste null mı kontrol et
                if (data?.liste == null)
                {
                    await DisplayAlert("Hata", "Deserialize sonucu liste null", "Tamam");
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Musteriler.Clear();
                    foreach (var m in data.liste)
                    {
                        Musteriler.Add(m);
                    }

                    // DEBUG: Kaç eleman eklendiğini göster
                    // DisplayAlert("Bilgi", $"{data.liste.Count} müşteri eklendi", "Tamam");

                    _toplamSayfa = data.toplam_sayfa > 0 ? data.toplam_sayfa : 1;
                    PageInfo.Text = $"{_page} / {_toplamSayfa}";
                    BtnGeri.IsEnabled = _page > 1;
                    BtnIleri.IsEnabled = _page < _toplamSayfa;
                });
            }
            else
            {
                await DisplayAlert("API Hatası", $"Durum: {res.StatusCode}\n{content}", "Tamam");
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
            {
                await DisplayAlert("Başarılı", "Hatırlatma maili gönderildi.", "Tamam");

                // KRİTİK DOKUNUŞ: Listeyi API'den tekrar çek ki yeni tarih ekrana yansısın
                await Yukle();
            }
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

public class TakipMusteriEski
{
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("ad_soyad")]
    public string ad_soyad { get; set; }

    [JsonPropertyName("eposta")]
    public string eposta { get; set; }

    [JsonPropertyName("son_giris_tarihi")]
    public string son_giris_tarihi { get; set; }

    [JsonPropertyName("kac_gun_oldu")]
    public int? kac_gun_oldu { get; set; }

    [JsonPropertyName("mail_istiyor_mu")]
    public bool mail_istiyor_mu { get; set; }

    // Görüntüleme metinleri
    public string KacGunText => kac_gun_oldu.HasValue ? $"{kac_gun_oldu} gündür girmiyor" : "Hiç giriş yapmamış";
    public string SonGirisText => string.IsNullOrEmpty(son_giris_tarihi) ? "Hiç giriş yok" : son_giris_tarihi;
    public string MailIzinDurum => mail_istiyor_mu ? "✅ Mail izni var" : "❌ Mail izni yok";
    public Color MailIzinRengi => mail_istiyor_mu ? Colors.Green : Colors.Red;
}

public class TakipMusteri
{
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("ad_soyad")]
    public string ad_soyad { get; set; }

    [JsonPropertyName("eposta")]
    public string eposta { get; set; }

    [JsonPropertyName("son_giris_tarihi")]
    public string son_giris_tarihi { get; set; }

    [JsonPropertyName("kac_gun_oldu")]
    public int? kac_gun_oldu { get; set; }

    [JsonPropertyName("mail_istiyor_mu")]
    public bool mail_istiyor_mu { get; set; }

    [JsonPropertyName("son_hatirlatma_tarihi")]
    public string son_hatirlatma_tarihi { get; set; }

    // Senin Eklediğin Güzel Görüntüleme Metinleri
    public string KacGunText => kac_gun_oldu.HasValue ? $"{kac_gun_oldu} gündür girmiyor" : "Hiç giriş yapmamış";
    public string SonGirisText => string.IsNullOrEmpty(son_giris_tarihi) ? "Hiç giriş yok" : son_giris_tarihi;
    public string MailIzinDurum => mail_istiyor_mu ? "✅ Mail izni var" : "❌ Mail izni yok";
    public Color MailIzinRengi => mail_istiyor_mu ? Colors.Green : Colors.Red;

    // Yeni Eklenen Hatırlatma Metinleri
    public string SonHatirlatmaText
    {
        get
        {
            if (string.IsNullOrEmpty(son_hatirlatma_tarihi))
                return "📌 Henüz hatırlatma maili atılmadı";

            if (DateTime.TryParse(son_hatirlatma_tarihi, out DateTime tarih))
                return $"⏰ Son Hatırlatma: {tarih.ToString("dd.MM.yyyy HH:mm")}";

            return "📌 Hatırlatma durumu bilinmiyor";
        }
    }

    public Color HatirlatmaRenk => string.IsNullOrEmpty(son_hatirlatma_tarihi) ? Color.FromArgb("#7F8C8D") : Color.FromArgb("#E67E22");

}

public class TakipResponse
{
    public List<TakipMusteri> liste { get; set; }
    public int toplam_sayfa { get; set; }
}