using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class ShowcaseView : ContentPage
{
    private bool _isTimerRunning;
    private DateTime _sonEtkilesimZamani = DateTime.Now; // Kullanıcının mouse/parmak hareketini takip ederiz

    public ShowcaseView()
    {
        InitializeComponent();
    }

    private async void VerileriYukle()
    {
        try
        {
            var apiService = new ApiService();
            var liste = await apiService.VitrinListesiGetirAsync();

            // Görselleri indirip ImageSource oluştur
            foreach (var item in liste)
            {
                if (!string.IsNullOrEmpty(item.TamResimUrl))
                {
                    try
                    {
                        using var client = new HttpClient();
                        var bytes = await client.GetByteArrayAsync(item.TamResimUrl);
                        item.ResimSource = ImageSource.FromStream(() => new MemoryStream(bytes));
                    }
                    catch
                    {
                        // Hata durumunda boş bırak
                    }
                }
            }

            ShowcaseCarousel.ItemsSource = liste;
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Vitrin yüklenemedi: " + ex.Message, "Hata");
        }

        /* // Manuel gösterim
        var vitrinListesi = new List<TamamlananIs>
        {
            new TamamlananIs {
                Baslik = "BMW 5 Serisi - Seramik Kaplama",
                Aciklama = "Aracın boya yüzeyi tamamen temizlenerek 3 katmanlı premium seramik kaplama uygulandı.",
                ResimUrl = "https://images.unsplash.com/photo-1619682817481-e994891cb1b4?w=800&q=80",
                Etiket = "✨ Seramik Kaplama",
                Tarih = "Ekim 2025"
            },
            new TamamlananIs {
                Baslik = "Audi A6 - Ağır Bakım",
                Aciklama = "Müşterimizin kapısında triger seti ve periyodik filtre değişimleri kusursuzca tamamlandı.",
                ResimUrl = "https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?w=800&q=80",
                Etiket = "🔧 Ağır Bakım",
                Tarih = "Kasım 2025"
            },
            new TamamlananIs {
                Baslik = "Mercedes C200 - Detaylı Temizlik",
                Aciklama = "Deri koltuklar özel solüsyonlarla temizlendi, ozonla dezenfeksiyon yapıldı.",
                ResimUrl = "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?w=800&q=80",
                Etiket = "🧼 Detaylı İç Bakım",
                Tarih = "Aralık 2025"
            }
        };
        ShowcaseCarousel.ItemsSource = vitrinListesi;
        */
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (100ms) bekleyip thread'i rahatlatıyoruz.
        // await Task.Delay(20);

        // Yükleme işlemini bu rahatlamadan sonra tetikliyoruz.

        VerileriYukle();
        // Üst başlığın premium şekilde belirmesi
        await HeaderAnim.FadeTo(1, 1200, Easing.CubicOut);

        _isTimerRunning = true;
        _sonEtkilesimZamani = DateTime.Now;

        // AKILLI MOTOR: 2.5 saniyede bir tetiklenir
        Dispatcher.StartTimer(TimeSpan.FromSeconds(2.5), () =>
        {
            if (!_isTimerRunning) return false;

            if ((DateTime.Now - _sonEtkilesimZamani).TotalSeconds >= 2.0)
            {
                var items = ShowcaseCarousel.ItemsSource as List<TamamlananIs>;
                if (items != null && items.Count > 0)
                {
                    int currentIndex = ShowcaseCarousel.Position;
                    int nextIndex = currentIndex + 1;
                    bool animasyonOlsunMu = true;

                    // Sona geldiysek başa dön, ama MAUI sapıtmasın diye animasyonu kapat!
                    if (nextIndex >= items.Count)
                    {
                        nextIndex = 0;
                        animasyonOlsunMu = false;
                    }

                    // İŞLETİM SİSTEMİ ANA İŞ PARÇACIĞI (MAIN THREAD) ZORLAMASI
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Position yerine MAUI'nin en stabil komutu olan ScrollTo'yu kullanıyoruz
                        ShowcaseCarousel.ScrollTo(nextIndex, position: ScrollToPosition.Center, animate: animasyonOlsunMu);
                        _sonEtkilesimZamani = DateTime.Now;
                    });
                }
            }
            return true;
        });
    }

    // YENİ EKLENEN KISIM: Kullanıcı mouse ile kaydırdığında motorun onunla inatlaşmasını engeller
    private void OnPositionChanged(object sender, PositionChangedEventArgs e)
    {
        // Kullanıcı kendi kaydırdığı an sayacı sıfırlıyoruz. 
        // Böylece okurken ekran altından kayıp gitmeyecek.
        // Kullanıcı kendi kaydırdığında sayacı sıfırlıyoruz ki motor onunla inatlaşmasın
        _sonEtkilesimZamani = DateTime.Now;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isTimerRunning = false; // Sayfadan çıkınca motoru durdur
    }
}

