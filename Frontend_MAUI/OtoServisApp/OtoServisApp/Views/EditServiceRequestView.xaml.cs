using OtoServisApp.Models;
using OtoServisApp.Services;
using System.Collections.Concurrent;

namespace OtoServisApp.Views;

public partial class EditServiceRequestView : ContentPage
{
    private readonly ApiService _apiService;
    private ServisTalebi _talep;
    private Kullanici _aktifKullanici;
    private List<Hizmet> _orijinalHizmetler;
    private Hizmet _secilenHizmet;

    // YENİ: AracPicker yerine seçilen aracı hafızada tutacağımız değişken
    private dynamic _secilenArac;

    // --- YENİ REVİZE BAŞLANGICI: Fotoğraf Değişkenleri ---
    private int MaksimumFotoSayisi = 4;
    public System.Collections.ObjectModel.ObservableCollection<FileResult> SecilenFotograflar { get; set; } = new();
    // --- YENİ REVİZE BİTİŞİ ---

    public EditServiceRequestView(ServisTalebi talep, Kullanici aktifKullanici)
    {
        InitializeComponent();
        _talep = talep;
        _aktifKullanici = aktifKullanici;
        _apiService = new ApiService();

        // --- Hasar Fotoğrafları Ekleme için ---
        BindingContext = this;
    }
    
    private bool _sayfaIlkYuklendi = false;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_sayfaIlkYuklendi) return;
        _sayfaIlkYuklendi = true;

        // 1. AŞAMA: Kullanıcıya donma hissi vermemek için Loading ekranını anında aç
        LoadingTitle.Text = "Bilgiler Yükleniyor..."; // YENİ REVİZE: Ekran açılış yazısını amaca uygun güncelledik
        LoadingSubText.Text = "Lütfen bekleyiniz.";
        LoadingOverlay.IsVisible = true;

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek ve Loading animasyonunu başlatması için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (20ms) bekleyip thread'i rahatlatıyoruz.
        await Task.Delay(10);

        try
        {
            DurumLabel.Text = _talep.durum;

            if (_talep.durum == "Bekliyor")
            {
                StandartDuzenlemeFormu.IsVisible = true;
                DuzeltmeTalebiFormu.IsVisible = false;
                KaydetLabel.IsVisible = true;
                KaydetLabel.Text = "Değişiklikleri Kaydet";

                // 3. AŞAMA: Asıl veriyi (API İsteklerini) şimdi çekiyoruz
                await VerileriYukle();
            }
            else if (_talep.durum == "Onaylandı" || _talep.durum == "İşlemde")
            {
                StandartDuzenlemeFormu.IsVisible = false;
                DuzeltmeTalebiFormu.IsVisible = true;
                KaydetLabel.IsVisible = true;
                KaydetLabel.Text = "Düzeltme Talebini İlet";

                // Eğer daha önceden bir not yazdıysa onu göster
                if (!string.IsNullOrEmpty(_talep.duzeltme_notu))
                {
                    DuzeltmeNotuEditor.Text = _talep.duzeltme_notu;
                }
            }
            else // Tamamlandı veya İptal Edildi
            {
                StandartDuzenlemeFormu.IsVisible = false;
                DuzeltmeTalebiFormu.IsVisible = false;
                KaydetLabel.IsVisible = false;
                ReadOnlyWarningLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Veriler yüklenirken bir sorun oluştu.", "Hata");
            System.Diagnostics.Debug.WriteLine($"Yükleme Hatası: {ex.Message}");
        }
        finally
        {
            // 4. AŞAMA: Veri gelse de, hata da verse Loading ekranını KESİNLİKLE kapat
            LoadingOverlay.IsVisible = false;
        }
    }

    private async Task VerileriYukle()
    {
        // --- ESKİ YAPI (YORUM SATIRINA ALINDI: Çalışan kod silinmedi, yeni revize eklendi) ---
        // _orijinalHizmetler = await _apiService.HizmetleriGetirAsync();
        // var araclar = await _apiService.KullaniciAraclariGetirAsync(_aktifKullanici.id);
        // var markalar = await _apiService.MarkalariGetirAsync();

        // --- YENİ REVİZE BAŞLANGICI: Paralel Veri Çekimi (Sayfa Açılışını Hızlandırır) ---
        var hizmetTask = _apiService.HizmetleriGetirAsync();
        var aracTask = _apiService.KullaniciAraclariGetirAsync(_aktifKullanici.id);
        var markaTask = _apiService.MarkalariGetirAsync();

        // Üç isteğin de aynı anda tamamlanmasını bekliyoruz (Zaman tasarrufu)
        await Task.WhenAll(hizmetTask, aracTask, markaTask);

        _orijinalHizmetler = await hizmetTask;
        var araclar = await aracTask;
        var markalar = await markaTask;
        // --- YENİ REVİZE BİTİŞİ ---

        if (_orijinalHizmetler != null)
        {
            HizmetListesi.ItemsSource = _orijinalHizmetler;

            var mevcutHizmet = _orijinalHizmetler.FirstOrDefault(h => h.id == _talep.hizmet_id);
            if (mevcutHizmet != null)
            {
                _secilenHizmet = mevcutHizmet;
                SecilenHizmetLabel.Text = mevcutHizmet.ad;
                SecilenHizmetLabel.TextColor = Color.FromArgb("#111111");
            }
        }

        if (araclar != null)
        {
            var pickerAracListesi = araclar.Select(a => {
                string gosterimAd = "";

                if (a.marka_id != null && a.model_id != null && markalar != null)
                {
                    var marka = markalar.FirstOrDefault(m => m.id == a.marka_id);
                    if (marka != null)
                    {
                        var model = marka.modeller?.FirstOrDefault(m => m.id == a.model_id);
                        if (model != null)
                        {
                            gosterimAd = $"{marka.ad} {model.ad}";
                        }
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

            var seciliArac = pickerAracListesi.FirstOrDefault(a => a.Id == _talep.arac_id);
            if (seciliArac != null)
            {
                _secilenArac = seciliArac;
                SecilenAracLabel.Text = seciliArac.marka_model_yazi;
                SecilenAracLabel.TextColor = Color.FromArgb("#111111");
            }
        }

        if (DateTime.TryParse(_talep.talep_tarihi, out DateTime parsedDate))
        {
            TarihPicker.Date = parsedDate;
        }
        AdresEditor.Text = _talep.adres;
        NotlarEditor.Text = _talep.notlar;
    }

    // =====================================================================================
    // 1. ANA TETİKLEYİCİ METOT (Sadece iş akışını yönetir, detaylarla boğuşmaz)
    // =====================================================================================
    private async void OnKaydetTapped(object sender, TappedEventArgs e)
    {
        // 1. Doğrulama (Hatalıysa işlemi anında kes)
        if (!await GirdileriDogrula()) return;

        // 2. Arayüzü Kilitle ve Kullanıcıya Bilgi Ver
        LoadingOverlay.IsVisible = true;
        LoadingTitle.Text = (SecilenFotograflar != null && SecilenFotograflar.Count > 0) ? "Fotoğraflar Yükleniyor..." : "Kaydediliyor...";
        LoadingSubText.Text = "Lütfen işlemin bitmesini bekleyiniz.";

        // Arayüz motoruna "Loading" çizimi için 50ms nefes aldırıyoruz (Ölümcül donmayı engeller)
        await Task.Delay(10);

        try
        {
            // 3. Veritabanı Güncellemesini Yap
            bool guncellemeBasarili = await VeritabaniGuncelleAsync();

            if (!guncellemeBasarili)
            {
                await ModernAlertService.ShowInfoAsync("Güncelleme sırasında bir sorun oluştu, lütfen tekrar deneyin.", "Hata");
                return; // İşlemi kes
            }

            // 4. Fotoğrafları Yükle (Varsa)
            string fotoHataMesajlari = await FotograflariYukleAsync();

            // 5. Sonuç Mesajını Göster ve Çık
            if (!string.IsNullOrEmpty(fotoHataMesajlari))
            {
                await ModernAlertService.ShowInfoAsync($"Talebiniz güncellendi ancak bazı fotoğraflar yüklenemedi:\n{fotoHataMesajlari}", "Kısmi Başarılı");
            }
            else
            {
                await ModernAlertService.ShowInfoAsync("Talep bilgileriniz ve fotoğraflarınız başarıyla güncellendi.", "Başarılı");
            }
            MessagingCenter.Send<object>(this, "TalepGuncellendi");
            await Navigation.PopAsync(); // Önceki ekrana dön
        }
        catch (Exception ex)
        {
            await ModernAlertService.ShowInfoAsync("Beklenmeyen bir hata oluştu: " + ex.Message, "Hata");
        }
        finally
        {
            // İşlem bitince şalteri KESİNLİKLE indir
            LoadingOverlay.IsVisible = false;
        }
    }

    // =====================================================================================
    // 2. YARDIMCI METOT: Sadece form doğrulamasını yapar
    // =====================================================================================
    private async Task<bool> GirdileriDogrula()
    {
        if (_talep.durum == "Bekliyor")
        {
            if (_secilenHizmet == null) { await ModernAlertService.ShowInfoAsync("Lütfen bir hizmet seçin.", "Uyarı"); return false; }
            if (_secilenArac == null) { await ModernAlertService.ShowInfoAsync("Lütfen Araç seçimini yapın.", "Uyarı"); return false; }
        }
        else if (_talep.durum == "Onaylandı" || _talep.durum == "İşlemde")
        {
            if (string.IsNullOrWhiteSpace(DuzeltmeNotuEditor.Text))
            {
                await ModernAlertService.ShowInfoAsync("Lütfen düzeltmek istediğiniz alanları yazın.", "Uyarı"); return false;
            }
        }
        return true;
    }

    // =====================================================================================
    // 3. YARDIMCI METOT: Sadece veritabanı (API Put) işlemini yapar
    // =====================================================================================
    private async Task<bool> VeritabaniGuncelleAsync()
    {
        if (_talep.durum == "Bekliyor")
        {
            return await _apiService.ServisTalebiGuncelleAsync(
                _talep.id, _secilenHizmet.id, _secilenArac.Id, TarihPicker.Date.ToString("yyyy-MM-dd"), AdresEditor.Text, NotlarEditor.Text, false, ""
            );
        }
        else
        {
            return await _apiService.ServisTalebiGuncelleAsync(
                _talep.id, null, null, "", "", "", true, DuzeltmeNotuEditor.Text
            );
        }
    }

    // =====================================================================================
    // 4. YARDIMCI METOT: Sadece Fotoğraf Yükleme ve Stream işlemlerini yapar
    // =====================================================================================
    /*private async Task<string> FotograflariYukleAsync()
    {
        if (SecilenFotograflar == null || SecilenFotograflar.Count == 0) return ""; // Fotoğraf yoksa boş dön, hata yok.

        string hatalar = "";

        // Önce eskileri temizle
        await _apiService.EskiFotograflariTemizleAsync(_talep.id);

        // Kullanıcı adını döngü dışında SADECE BİR KERE temizleyerek işlemciyi yormuyoruz
        string temizAdSoyad = string.IsNullOrWhiteSpace(_aktifKullanici?.ad_soyad) ? "Kullanici" : _aktifKullanici.ad_soyad.Replace(" ", "");

        foreach (var foto in SecilenFotograflar)
        {
            try
            {
                using var stream = await foto.OpenReadAsync();

                string uzanti = string.IsNullOrWhiteSpace(foto.FileName) ? ".jpg" : Path.GetExtension(foto.FileName);
                if (string.IsNullOrEmpty(uzanti)) uzanti = ".jpg";

                string zaman = DateTime.Now.ToString("yyyy_MM_dd_HHmm_ssfff");
                string ozelDosyaAdi = $"{temizAdSoyad}-{_talep.id}-{zaman}{uzanti}";

                string uploadSonuc = await _apiService.UploadHasarFotografAsync(_talep.id, stream, ozelDosyaAdi);

                if (uploadSonuc != "OK")
                {
                    hatalar += $"- {foto.FileName ?? "Bilinmeyen Dosya"}: {uploadSonuc}\n";
                }
            }
            catch (Exception ex)
            {
                hatalar += $"- {foto.FileName}: Dosya okunamadı ({ex.Message})\n";
            }
        }

        return hatalar; // Hata varsa string dolu döner, yoksa boş döner
    }*/

    private async Task<string> FotograflariYukleAsync()
    {
        if (SecilenFotograflar == null || SecilenFotograflar.Count == 0) return "";

        await _apiService.EskiFotograflariTemizleAsync(_talep.id);

        string temizAdSoyad = string.IsNullOrWhiteSpace(_aktifKullanici?.ad_soyad) ? "Kullanici" : _aktifKullanici.ad_soyad.Replace(" ", "");
        var hataListesi = new ConcurrentBag<string>();

        var uploadTasks = SecilenFotograflar.Select(async foto =>
        {
            try
            {
                using var stream = await foto.OpenReadAsync();
                string uzanti = Path.GetExtension(foto.FileName);
                if (string.IsNullOrEmpty(uzanti)) uzanti = ".jpg";
                string zaman = DateTime.Now.ToString("yyyy_MM_dd_HHmm_ssfff");
                string ozelDosyaAdi = $"{temizAdSoyad}-{_talep.id}-{zaman}{uzanti}";
                string sonuc = await _apiService.UploadHasarFotografAsync(_talep.id, stream, ozelDosyaAdi);
                if (sonuc != "OK")
                    hataListesi.Add($"- {foto.FileName}: {sonuc}");
            }
            catch (Exception ex)
            {
                hataListesi.Add($"- {foto.FileName}: {ex.Message}");
            }
        });

        await Task.WhenAll(uploadTasks);
        return string.Join("\n", hataListesi);
    }


    // =====================================================================================
    // 5. YENİ REVİZE: Fotoğraf Seçimi (OnAddPhotoClicked) 
    // Gereksiz if/else karmaşası temizlendi, kod daha akıcı hale getirildi.
    // =====================================================================================
    private async void OnAddPhotoTapped(object sender, TappedEventArgs e)
    {
        int eklenebilirSayi = MaksimumFotoSayisi - SecilenFotograflar.Count;

        if (eklenebilirSayi <= 0)
        {
            await ModernAlertService.ShowInfoAsync($"En fazla {MaksimumFotoSayisi} adet fotoğraf ekleyebilirsiniz.", "Bilgi");
            return;
        }

        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Hasar Fotoğraflarını Seçin",
                FileTypes = FilePickerFileType.Images
            };

            var photos = await FilePicker.Default.PickMultipleAsync(options);

            if (photos != null)
            {
                int eklenen = 0;
                foreach (var photo in photos)
                {
                    if (eklenen < eklenebilirSayi)
                    {
                        SecilenFotograflar.Add(photo);
                        eklenen++;
                    }
                    else
                    {
                        await ModernAlertService.ShowInfoAsync($"Maksimum {MaksimumFotoSayisi} fotoğrafa ulaşıldı, diğerleri göz ardı edildi.", "Sınır Aşıldı");
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            await ModernAlertService.ShowInfoAsync("Fotoğraf seçimi iptal edildi veya bir sorun oluştu.", "İptal");
        }
    }

    // --- YENİ REVİZE BAŞLANGICI: Hasar Fotoğrafı Silme İşlemleri ---
    private void OnRemovePhotoTapped(object sender, TappedEventArgs e)
    {
        var photo = e.Parameter as FileResult;
        if (photo != null)
        {
            SecilenFotograflar.Remove(photo);
        }
    }
    // --- YENİ REVİZE BİTİŞİ ---

    // --- HİZMET SEÇİMİ METOTLARI ---
    private void OnHizmetSecimTapped(object sender, TappedEventArgs e)
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
        {
            HizmetListesi.ItemsSource = _orijinalHizmetler;
        }
        else
        {
            HizmetListesi.ItemsSource = _orijinalHizmetler.Where(h =>
                (h.ad != null && h.ad.ToLower().Contains(aramaMetni)) ||
                (h.aciklama != null && h.aciklama.ToLower().Contains(aramaMetni))
            ).ToList();
        }
    }

    private void OnHizmetSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as Hizmet;
        if (secilen != null)
        {
            _secilenHizmet = secilen;

            SecilenHizmetLabel.Text = secilen.ad;
            SecilenHizmetLabel.TextColor = Color.FromArgb("#111111");

            HizmetAramaKutusu.IsVisible = false; // Seçilince kapat
            HizmetListesi.SelectedItem = null; // Seçimi sıfırla
        }
    }

    // --- ARAÇ SEÇİMİ METOTLARI (YENİ) ---
    private void OnAracSecimTapped(object sender, TappedEventArgs e)
    {
        AracAramaKutusu.IsVisible = !AracAramaKutusu.IsVisible;

        if (AracAramaKutusu.IsVisible)
        {
            HizmetAramaKutusu.IsVisible = false; // DİĞERİNİ ZORLA KAPAT
        }
    }

    private void OnAracSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault();
        if (secilen != null)
        {
            _secilenArac = secilen;
            dynamic ar = secilen; // Anonim obje olduğu için dynamic ile okuyoruz

            SecilenAracLabel.Text = ar.marka_model_yazi;
            SecilenAracLabel.TextColor = Color.FromArgb("#111111");

            AracAramaKutusu.IsVisible = false; // Seçilince kapat
            AracListesi.SelectedItem = null; // Seçimi sıfırla
        }
    }
}