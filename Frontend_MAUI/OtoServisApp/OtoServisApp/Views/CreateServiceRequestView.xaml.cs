using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class CreateServiceRequestView : ContentPage
{
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;

    private List<Hizmet> _orijinalHizmetler;
    private Hizmet _secilenHizmet;
    private dynamic _secilenArac;

    // Hasar Resimleri için Parametrik değişkenimiz
    private int MaksimumFotoSayisi = 4;
    public System.Collections.ObjectModel.ObservableCollection<FileResult> SecilenFotograflar { get; set; } = new();

    public CreateServiceRequestView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();

        if (!string.IsNullOrEmpty(_aktifKullanici.adres))
        {
            AddressEditor.Text = _aktifKullanici.adres;
        }
        // Hasar Resimleri için
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // MADDE 80 (YENİ REVİZE): Sadece bilgi ver, sayfadan atma!
        if (_aktifKullanici.araclar == null || !_aktifKullanici.araclar.Any())
        {
            await ModernAlertService.ShowInfoAsync("Servis talebi oluşturabilmek için sisteme kayıtlı bir aracınız olması gerekir. Şu an ekranı inceleyebilirsiniz ancak talep oluşturmadan önce lütfen bir araç ekleyin.", "Bilgilendirme");
        }

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (100ms) bekleyip thread'i rahatlatıyoruz.
        // await Task.Delay(20);

        // Yükleme işlemini bu rahatlamadan sonra tetikliyoruz.

        // 1. Hizmetleri Çek
        _orijinalHizmetler = await _apiService.HizmetleriGetirAsync();
        if (_orijinalHizmetler != null)
        {
            HizmetListesi.ItemsSource = _orijinalHizmetler;
        }

        // 2. Kullanıcının Araçlarını Hazırla
        var markalar = await _apiService.MarkalariGetirAsync();
        if (markalar != null && _aktifKullanici.araclar != null)
        {
            var pickerAracListesi = _aktifKullanici.araclar.Select(a => {
                string gosterimAd = "";

                if (a.marka_id != null && a.model_id != null)
                {
                    var marka = markalar.FirstOrDefault(m => m.id == a.marka_id);
                    if (marka != null)
                    {
                        var model = marka.modeller?.FirstOrDefault(m => m.id == a.model_id);
                        if (model != null) gosterimAd = $"{marka.ad} {model.ad}";
                    }
                }

                if (string.IsNullOrWhiteSpace(gosterimAd) && !string.IsNullOrWhiteSpace(a.ozel_marka))
                {
                    gosterimAd = $"{a.ozel_marka} {a.ozel_model}";
                }

                if (string.IsNullOrWhiteSpace(gosterimAd))
                {
                    gosterimAd = "Araç ID: " + a.id;
                }

                return new { Id = a.id, marka_model_yazi = gosterimAd, yil = a.yil };
            }).ToList();

            AracListesi.ItemsSource = pickerAracListesi;
        }
    }

    // --- ARAÇ SEÇİMİ ---
    private void OnAracSecimButonuClicked(object sender, EventArgs e)
    {
        AracAramaKutusu.IsVisible = !AracAramaKutusu.IsVisible;
        if (AracAramaKutusu.IsVisible) HizmetAramaKutusu.IsVisible = false;
    }

    private void OnAracSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault();
        if (secilen != null)
        {
            _secilenArac = secilen;
            dynamic ar = secilen;

            SecilenAracButonu.Text = ar.marka_model_yazi;
            SecilenAracButonu.TextColor = Color.FromArgb("#111111");

            AracAramaKutusu.IsVisible = false;
            AracListesi.SelectedItem = null;
        }
    }

    // --- HİZMET SEÇİMİ ---
    private void OnHizmetSecimButonuClicked(object sender, EventArgs e)
    {
        HizmetAramaKutusu.IsVisible = !HizmetAramaKutusu.IsVisible;
        if (HizmetAramaKutusu.IsVisible)
        {
            AracAramaKutusu.IsVisible = false;
            HizmetAramaBar.Focus();
        }
    }

    private void OnHizmetAramaDegisti(object sender, TextChangedEventArgs e)
    {
        if (_orijinalHizmetler == null) return;
        var aramaMetni = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(aramaMetni))
            HizmetListesi.ItemsSource = _orijinalHizmetler;
        else
            HizmetListesi.ItemsSource = _orijinalHizmetler.Where(h =>
                (h.ad != null && h.ad.ToLower().Contains(aramaMetni)) ||
                (h.aciklama != null && h.aciklama.ToLower().Contains(aramaMetni))
            ).ToList();
    }

    private void OnHizmetSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as Hizmet;
        if (secilen != null)
        {
            _secilenHizmet = secilen;

            SecilenHizmetButonu.Text = secilen.ad;
            SecilenHizmetButonu.TextColor = Color.FromArgb("#111111");

            HizmetAramaKutusu.IsVisible = false;
            HizmetListesi.SelectedItem = null;
        }
    }

    // --- KAYDET BUTONU ---
    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        // --- 1. KONTROLLER ---

        // Önce aracın sistemde var olup olmadığına bakalım (Hiç aracı yoksa)
        if (_aktifKullanici.araclar == null || !_aktifKullanici.araclar.Any())
        {
            await ModernAlertService.ShowInfoAsync("Sisteme kayıtlı aracınız bulunmadığı için servis talebi oluşturamazsınız. Lütfen 'Araçlarım' sekmesinden bir araç ekleyip tekrar deneyin.", "İşlem Başarısız");
            return; // İşlemi burada kes
        }

        // Aracı var ama listeden seçmeyi unuttuysa veya diğer alanlar boşsa
        if (_secilenArac == null || _secilenHizmet == null || string.IsNullOrEmpty(AddressEditor.Text))
        {
            await ModernAlertService.ShowInfoAsync("Lütfen araç, hizmet ve adres alanlarını eksiksiz doldurun.", "Uyarı");
            return;
        }

        // --- 2. İŞLEM BAŞLADI: BUTONU VE EKRANI KİLİTLE ---
        SubmitButton.IsEnabled = false;
        SubmitButton.Text = "GÖNDERİLİYOR...";
        LoadingOverlay.IsVisible = true;

        // --- YENİ DİNAMİK METİN AYARI ---
        if (SecilenFotograflar != null && SecilenFotograflar.Count > 0)
        {
            LoadingTitle.Text = "Fotoğraflarınız Yükleniyor...";
            LoadingSubText.Text = "Dosya boyutlarına göre bu işlem biraz zaman alabilir.\nLütfen bekleyiniz.";
        }
        else
        {
            LoadingTitle.Text = "İşleminiz Yapılıyor...";
            LoadingSubText.Text = "Lütfen bekleyiniz.";
        }

        LoadingOverlay.IsVisible = true; // Ekranı şimdi açıyoruz

        try
        {
            string formatliTarih = RequestDatePicker.Date.ToString("yyyy-MM-dd");

            var yeniTalep = new ServisTalebiRequest
            {
                kullanici_id = _aktifKullanici.id,
                arac_id = _secilenArac.Id,
                hizmet_id = _secilenHizmet.id,
                talep_tarihi = formatliTarih,
                adres = AddressEditor.Text,
                notlar = NotesEditor.Text
            };

            // 3. VERİTABANINA KAYIT (Overlay devredeyken yapılıyor)
            string sonuc = await _apiService.ServisTalebiOlusturAsync(yeniTalep);

            if (int.TryParse(sonuc, out int olusturulanTalepId))
            {
                // 4. TALEBİMİZ OLUŞTU, ŞİMDİ FOTOĞRAFLARI YÜKLÜYORUZ
                int yuklenemeyen = 0;
                string hataMesajlari = "";

                foreach (var foto in SecilenFotograflar)
                {
                    using var stream = await foto.OpenReadAsync();

                    string temizAdSoyad = string.IsNullOrWhiteSpace(_aktifKullanici?.ad_soyad)
                                          ? "Kullanici"
                                          : _aktifKullanici.ad_soyad.Replace(" ", "");

                    string uzanti = Path.GetExtension(foto.FileName);
                    if (string.IsNullOrEmpty(uzanti)) uzanti = ".jpg";

                    string ozelDosyaAdi = $"{temizAdSoyad}-{olusturulanTalepId}-{DateTime.Now.ToString("yyyy_MM_dd_HHmm_ssfff")}{uzanti}";

                    string uploadSonuc = await _apiService.UploadHasarFotografAsync(olusturulanTalepId, stream, ozelDosyaAdi);

                    if (uploadSonuc != "OK")
                    {
                        yuklenemeyen++;
                        hataMesajlari += $"- {foto.FileName}: {uploadSonuc}\n";
                    }
                }

                // Başarılı mesajı
                bool fotografEklendi = SecilenFotograflar != null && SecilenFotograflar.Count > 0;

                if (yuklenemeyen > 0)
                {
                    await ModernAlertService.ShowInfoAsync(
                        $"Servis talebiniz oluşturuldu ancak bazı fotoğraflar yüklenemedi:\n{hataMesajlari}\nDaha sonra talebi düzenle (Taleplerim/Durum Takibi) ekranından tekrar yüklemeyi deneyebilirsiniz.",
                        "Kısmi Başarılı");
                }
                else if (fotografEklendi)
                {
                    await ModernAlertService.ShowInfoAsync("Servis talebiniz ve fotoğraflarınız başarıyla alınmıştır. En kısa sürede sizinle iletişime geçeceğiz.", "Başarılı");
                }
                else
                {
                    await ModernAlertService.ShowInfoAsync("Servis talebiniz başarıyla alınmıştır. En kısa sürede sizinle iletişime geçeceğiz.", "Başarılı");
                }

                await Navigation.PopAsync();
            }
            else
            {
                // Backend'den hata veya soru geldi
                SubmitButton.IsEnabled = true;
                SubmitButton.Text = "TALEBİ OLUŞTUR";

                if (sonuc.Contains("yeni bir talep açmak ister misiniz?"))
                {
                    // Özel soru ekranını göstermeden önce Overlay'i kapatıyoruz ki soru gözüksün
                    LoadingOverlay.IsVisible = false;

                    bool? cevapSonuc = await ModernAlertService.ShowAsync("Mevcut Talep Uyarısı", sonuc, "EvetIptal");
                    bool cevap = cevapSonuc == true;
                    if (cevap)
                    {
                        var silinecekHizmet = _orijinalHizmetler.FirstOrDefault(h => h.id == _secilenHizmet.id);
                        if (silinecekHizmet != null)
                        {
                            _orijinalHizmetler.Remove(silinecekHizmet);
                        }

                        HizmetListesi.ItemsSource = null;
                        HizmetListesi.ItemsSource = _orijinalHizmetler;

                        _secilenHizmet = null;
                        SecilenHizmetButonu.Text = "Hizmet Seçiniz";
                        SecilenHizmetButonu.TextColor = Color.FromArgb("#888888");

                        HizmetAramaKutusu.IsVisible = true;
                        HizmetAramaBar.Focus();
                    }
                }
                else
                {
                    await ModernAlertService.ShowInfoAsync(sonuc, "Hata Oluştu");
                }
            }
        }
        catch (Exception ex)
        {
            SubmitButton.IsEnabled = true;
            SubmitButton.Text = "TALEBİ OLUŞTUR";
            await ModernAlertService.ShowInfoAsync("İşlem sırasında hata: " + ex.Message, "Hata");
        }
        finally
        {
            // 5. İŞLEM BİTTİ: EKRAN KİLİDİNİ AÇ
            LoadingOverlay.IsVisible = false;
        }
    }

    // Hasarlı Araç Resimleri Ekleme Fonksiyonu
    // YENİ REVİZE: Toplu Seçim İşlemi (Madde 1)
    private async void OnAddPhotoClicked(object sender, EventArgs e)
    {
        if (SecilenFotograflar.Count >= MaksimumFotoSayisi)
        {
            await ModernAlertService.ShowInfoAsync($"En fazla {MaksimumFotoSayisi} adet fotoğraf ekleyebilirsiniz.", "Bilgi");
            return;
        }

        try
        {
            // MAUI'de toplu fotoğraf seçimi için FilePicker sınıfını sadece resimlere filtreleyerek yapılandırıyoruz
            var options = new PickOptions
            {
                PickerTitle = "Hasar Fotoğraflarını Seçin",
                FileTypes = FilePickerFileType.Images
            };

            // MediaPicker yerine FilePicker'ın çoklu seçim metodunu kullanıyoruz
            var photos = await FilePicker.Default.PickMultipleAsync(options);

            if (photos != null)
            {
                foreach (var photo in photos)
                {
                    if (SecilenFotograflar.Count < MaksimumFotoSayisi)
                    {
                        SecilenFotograflar.Add(photo);
                    }
                    else
                    {
                        await ModernAlertService.ShowInfoAsync($"Maksimum {MaksimumFotoSayisi} fotoğraf sınırına ulaşıldı. Diğerleri eklenemedi.", "Bilgi");
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraflar seçilirken bir hata oluştu: " + ex.Message, "Hata");
        }
    }

    private void OnRemovePhotoClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var photo = button?.CommandParameter as FileResult;
        if (photo != null)
        {
            SecilenFotograflar.Remove(photo);
        }
    }
}