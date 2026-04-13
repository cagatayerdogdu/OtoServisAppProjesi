using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminRequestsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<ServisTalebi> _orijinalTalepler;

    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde"}; //, "Tamamlandı", "İptal Edildi" 
    private string _secilenDurum = "Tümü";

    public AdminRequestsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (100ms) bekleyip thread'i rahatlatıyoruz.
        // await Task.Delay(20);

        // Yükleme işlemini bu rahatlamadan sonra tetikliyoruz.
        DurumListesi.ItemsSource = _durumFiltreleri;
        await VerileriYukle();
    }

    private async Task VerileriYukle()
    {
        // Veriler yüklenirken mevcut listeyi temizleyebiliriz				   
        _tumHizmetler = await _apiService.HizmetleriGetirAsync();
        _orijinalTalepler = await _apiService.AdminAktifTalepleriGetirAsync();
        var markalar = await _apiService.MarkalariGetirAsync();

        if (_orijinalTalepler != null)
        {
            foreach (var talep in _orijinalTalepler)
            {
                // 1. Hizmet Adı Eşleştirme
                if (_tumHizmetler != null)
                {
                    var h = _tumHizmetler.FirstOrDefault(x => x.id == talep.hizmet_id);
                    if (h != null) talep.hizmet_adi = h.ad;
                }

                // 2. Araç Bilgilerini Detaylandırma (Kritik Blok)
                var arac = await _apiService.AracGetirAsync(talep.arac_id);
                if (arac != null)
                {
                    string gosterimAd = "";
                    if (arac.marka_id != null && arac.model_id != null && markalar != null)
                    {
                        var marka = markalar.FirstOrDefault(m => m.id == arac.marka_id);
                        if (marka != null)
                        {
                            var model = marka.modeller?.FirstOrDefault(m => m.id == arac.model_id);
                            if (model != null) gosterimAd = $"{marka.ad} {model.ad}";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(gosterimAd) && !string.IsNullOrWhiteSpace(arac.ozel_marka))
                    {
                        gosterimAd = $"{arac.ozel_marka} {arac.ozel_model}";
                    }

                    talep.arac_adi_tam = string.IsNullOrWhiteSpace(gosterimAd) ? $"Araç ID: {arac.id}" : gosterimAd;
                }
                else
                {
                    talep.arac_adi_tam = "Sistemden Silinmiş Araç";
                }

                // YENİ REVİZE: 3. Admin tarafında talebe ait fotoğraf var mı kontrolü
                var fotolar = await _apiService.TalepFotograflariniGetirAsync(talep.id);
                talep.foto_var_mi = fotolar != null && fotolar.Count > 0;
            }

            // Veriler işlendikten sonra filtreyi uygula ve listeyi yapılandır										 
            FiltreleriUygula();
        }
    }

    // =========================================================
    // FİLTRELEME SİSTEMİ
    // =========================================================

    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        FiltreleriUygula();
    }

    private void FiltreleriUygula()
    {
        if (_orijinalTalepler == null) return;

        var filtrelenmisListe = _orijinalTalepler.AsEnumerable();

        // 1. ARAMA FİLTRESİ
        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            var metin = AramaBar.Text;
            filtrelenmisListe = filtrelenmisListe.Where(t =>
                (t.kullanici_ad_soyad != null && t.kullanici_ad_soyad.Contains(metin, StringComparison.OrdinalIgnoreCase)) ||
                (t.arac_adi_tam != null && t.arac_adi_tam.Contains(metin, StringComparison.OrdinalIgnoreCase))
            );
        }

        // 2. DURUM FİLTRESİ
        if (_secilenDurum != "Tümü")
        {
            filtrelenmisListe = filtrelenmisListe.Where(t => t.durum == _secilenDurum);
        }

        // Arama Barı Filtresi			   
        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            var kelime = AramaBar.Text.ToLower();
            filtrelenmisListe = filtrelenmisListe.Where(t =>
                (t.kullanici_ad_soyad != null && t.kullanici_ad_soyad.ToLower().Contains(kelime)) ||
                (t.arac_adi_tam != null && t.arac_adi_tam.ToLower().Contains(kelime))
            );
        }

        // Değişiklikleri ekrana yansıtmak için listeyi tazeliyoruz
        RequestsList.ItemsSource = null;
        // 3. EFSANE SIRALAMA MANTIĞI (Geri Geldi!)
        // Önce duruma göre aciliyet sırası, sonra eskiden yeniye (ID sırası en güvenli tarih sırasıdır)
        filtrelenmisListe = filtrelenmisListe
            .OrderBy(t => t.durum switch
            {
                "Bekliyor" => 1,
                "Onaylandı" => 2,
                "İşlemde" => 3,
                "Tamamlandı" => 4,
                "İptal Edildi" => 5,
                _ => 6
            })
            .ThenBy(t => t.id); // Aynı durumdaki talepleri en eskiden (ilk eklenen) en yeniye doğru sıralar
        RequestsList.ItemsSource = filtrelenmisListe.ToList();
    }

    // =========================================================
    // ÜST TARAF FİLTRE DROPDOWN KONTROLLERİ
    // =========================================================

    private void OnFiltreDurumKutusuAcKapat(object sender, EventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;

        // REVİZE: Üst filtre açıldığında, kartların içindeki tüm açık dropdownları kapat ve ekranı tazele
        if (DurumSecimKutusu.IsVisible && _orijinalTalepler != null)
        {
            foreach (var talep in _orijinalTalepler)
            {
                talep.DropdownAcikMi = false;
            }
            FiltreleriUygula();
        }
    }

    private void OnFiltreDurumSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            _secilenDurum = secilen;
            SecilenDurumButonu.Text = secilen;
            DurumSecimKutusu.IsVisible = false;
            DurumListesi.SelectedItem = null;
            FiltreleriUygula();
        }
    }

    /*
    // =========================================================
    // KART İÇİ DURUM SEÇİM KONTROLLERİ
    // =========================================================

    private void OnItemDurumKutusuAc(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var tiklananTalep = btn?.BindingContext as ServisTalebi;

        if (tiklananTalep != null)
        {
            // REVİZE: Kart içindeki açılırken ÜST FİLTREYİ ZORLA KAPAT
            DurumSecimKutusu.IsVisible = false;

            if (_orijinalTalepler != null)
            {
                foreach (var talep in _orijinalTalepler)
                {
                    if (talep != tiklananTalep)
                    {
                        talep.DropdownAcikMi = false;
                    }
                }
            }

            tiklananTalep.DropdownAcikMi = !tiklananTalep.DropdownAcikMi;

            // REVİZE: Değişikliği ekranda göstermek için listeyi tazele
            FiltreleriUygula();
        }
    }


    private void OnItemDurumSecildi(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn != null)
        {
            var yeniDurum = btn.Text;
            var secilenTalep = btn.BindingContext as ServisTalebi;

            if (secilenTalep != null)
            {
                secilenTalep.durum = yeniDurum;
                secilenTalep.DropdownAcikMi = false;
                FiltreleriUygula(); // UI Tazele
            }
        }
    }
    // =========================================================
    // GÜNCELLEME İŞLEMİ
    // =========================================================
    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var talep = button?.CommandParameter as ServisTalebi;

        if (talep != null)
        {
            // API üzerinden güncelleme isteği yapılandırılır											
            bool basarili = await _apiService.AdminTalepGuncelleAsync(talep.id, talep.durum, talep.tahmini_tutar);

            if (basarili)
            {
                await DisplayAlert("Başarılı", "Talep başarıyla güncellendi.", "Tamam");
                await VerileriYukle(); // Listeyi son haliyle tazele
            }
            else
            {
                await DisplayAlert("Hata", "Güncellenirken bir sorun oluştu, lütfen tekrar deneyin.", "Tamam");
            }
        }
    }
    */



    // =========================================================
    // KART İÇİ DURUM SEÇİM KONTROLLERİ (SCROLL KAYMAMASI İÇİN YENİDEN YAZILDI)
    // =========================================================

    private void OnItemDurumKutusuAc(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var parentStack = btn?.Parent as VerticalStackLayout;

        if (parentStack != null)
        {
            // Tıklanan butonun hemen altındaki dropdown kutusunu (Border) buluyoruz
            var dropdownBorder = parentStack.Children.OfType<Border>().FirstOrDefault();
            if (dropdownBorder != null)
            {
                dropdownBorder.IsVisible = !dropdownBorder.IsVisible;
            }
        }
    }

    private void OnItemDurumSecildi(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var secilenTalep = btn?.BindingContext as ServisTalebi;

        if (secilenTalep != null && btn != null)
        {
            var yeniDurum = btn.Text;
            secilenTalep.durum = yeniDurum;

            // Kapatılacak dropdown menüsünü bul
            var verticalLayout = btn.Parent as VerticalStackLayout;
            var dropdownBorder = verticalLayout?.Parent as Border;

            if (dropdownBorder != null)
            {
                dropdownBorder.IsVisible = false; // Menüyü kapat

                // Ana butonu bul ve ekrandaki metnini (durumunu) değiştir
                var mainStack = dropdownBorder.Parent as VerticalStackLayout;
                var mainButton = mainStack?.Children.OfType<Button>().FirstOrDefault();
                if (mainButton != null)
                {
                    mainButton.Text = yeniDurum;
                }
            }
        }
    }

    // =========================================================
    // GÜNCELLEME İŞLEMİ
    // =========================================================
    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var talep = button?.CommandParameter as ServisTalebi;

        if (talep != null)
        {
            // YENİ EKLENEN KISIM: Uygulamaya giriş yapan kişinin ID'sini alıyoruz.
            // Bulduğun 'kullanici_id_gizli' anahtarını kullanarak ID'yi çekiyoruz
            string idStr = await SecureStorage.Default.GetAsync("kullanici_id_gizli");
            int? aktifAdminId = int.TryParse(idStr, out int id) ? id : (int?)null;

            // API servisine bu ID'yi de parametre olarak geçiyoruz
            bool basarili = await _apiService.AdminTalepGuncelleAsync(talep.id, talep.durum, talep.tahmini_tutar, aktifAdminId);
            
            if (basarili)
            {
                await DisplayAlert("Başarılı", "Talep başarıyla güncellendi.", "Tamam");
                // YENİ REVİZE: VerileriYukle(); metodunu sildik! 
                // Artık güncelledikten sonra liste asla kaymayacak, sen sayfadan çıkana kadar orada kalacak.
            }
            else
            {
                await DisplayAlert("Hata", "Güncellenirken bir sorun oluştu, lütfen tekrar deneyin.", "Tamam");
            }
        }
    }

    // =========================================================
    // MAUI'nin yerleşik panoya kopyalama özelliği
    // =========================================================    
    private async void OnCopyTapped(object sender, EventArgs e)
    {
        var label = sender as Label;
        var gesture = label?.GestureRecognizers.FirstOrDefault() as TapGestureRecognizer;
        var kopyalanacakMetin = gesture?.CommandParameter as string;

        if (!string.IsNullOrWhiteSpace(kopyalanacakMetin))
        {
            await Clipboard.Default.SetTextAsync(kopyalanacakMetin);
            await DisplayAlert("Kopyalandı", "Bilgi panoya kopyalandı.", "Tamam");
        }
    }

    // ===============================================================================
    // YENİ REVİZE: Admin Tarafından Fotoğrafları Gör Butonu Tıklanma Olayı
    // ===============================================================================
    private async void OnViewPhotosClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            await Navigation.PushAsync(new ViewPhotosView(secilenTalep));
        }
    }

    // =========================================================
    // YENİ: ADMİN TARAFINDAN FOTOĞRAF EKLEME İŞLEMİ
    // =========================================================
    private async void OnAddPhotoClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var talep = button?.CommandParameter as ServisTalebi;

        if (talep == null) return;

        try
        {
            // 1. ADIM: Fotoğrafları seçiyoruz
            var sonuclar = await FilePicker.PickMultipleAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Servis Fotoğraflarını Seçin"
            });

            if (sonuclar == null || !sonuclar.Any()) return;

            // 2. ADIM: Yükleme ekranını yapılandır ve aç
            LoadingTitle.Text = "Fotoğraflar Sunucuya Aktarılıyor...";
            LoadingSubText.Text = $"{sonuclar.Count()} adet görsel işleniyor.";
            LoadingOverlay.IsVisible = true;

            int basarili = 0;
            int hatali = 0;

            foreach (var foto in sonuclar)
            {
                using var stream = await foto.OpenReadAsync();

                // Güvenli isimlendirme (Admin olduğu için başına 'Admin' ekliyoruz)
                string zaman = DateTime.Now.ToString("yyyy_MM_dd_HHmm_ssfff");
                string uzanti = Path.GetExtension(foto.FileName);
                if (string.IsNullOrEmpty(uzanti)) uzanti = ".jpg";

                string ozelDosyaAdi = $"Admin-{talep.id}-{zaman}{uzanti}";

                // API'ye gönderim yapıyoruz
                string sonuc = await _apiService.UploadHasarFotografAsync(talep.id, stream, ozelDosyaAdi);

                if (sonuc == "OK") basarili++;
                else hatali++;
            }

            // 3. ADIM: Bilgilendirme ve Liste Yenileme
            if (hatali > 0)
            {
                await DisplayAlert("Kısmi Başarılı", $"{basarili} fotoğraf yüklendi, {hatali} fotoğraf yüklenemedi.", "Tamam");
            }
            else
            {
                await DisplayAlert("Başarılı", "Tüm fotoğraflar talebe başarıyla eklendi.", "Tamam");
            }

            // Listeyi tazeliyoruz ki "Fotoğrafları Gör" butonu (eğer varsa) görünür olsun
            await VerileriYukle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Fotoğraf ekleme işlemi sırasında bir sorun oluştu: " + ex.Message, "Tamam");
        }
        finally
        {
            // 4. ADIM: Yükleme ekranını kapat
            LoadingOverlay.IsVisible = false;
        }
    }
}