using OtoServisApp.Models;
using OtoServisApp.Services;

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

    public EditServiceRequestView(ServisTalebi talep, Kullanici aktifKullanici)
    {
        InitializeComponent();
        _talep = talep;
        _aktifKullanici = aktifKullanici;
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (100ms) bekleyip thread'i rahatlatıyoruz.
        await Task.Delay(20);

        // Yükleme işlemini bu rahatlamadan sonra tetikliyoruz.

        DurumLabel.Text = _talep.durum;

        if (_talep.durum == "Bekliyor")
        {
            StandartDuzenlemeFormu.IsVisible = true;
            DuzeltmeTalebiFormu.IsVisible = false;
            KaydetButton.IsVisible = true;
            KaydetButton.Text = "Değişiklikleri Kaydet";
            await VerileriYukle();
        }
        else if (_talep.durum == "Onaylandı" || _talep.durum == "İşlemde")
        {
            StandartDuzenlemeFormu.IsVisible = false;
            DuzeltmeTalebiFormu.IsVisible = true;
            KaydetButton.IsVisible = true;
            KaydetButton.Text = "Düzeltme Talebini İlet";
            KaydetButton.BackgroundColor = Color.FromArgb("#F57C00");

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
            KaydetButton.IsVisible = false;
            ReadOnlyWarningLabel.IsVisible = true;
        }
    }

    private async Task VerileriYukle()
    {
        //var hizmetler = await _apiService.HizmetleriGetirAsync();

        // Hizmetleri çek ve orijinal listeye kaydet (A-Z sıralı gelecek zaten)
        _orijinalHizmetler = await _apiService.HizmetleriGetirAsync();
        var araclar = await _apiService.KullaniciAraclariGetirAsync(_aktifKullanici.id);

        // Sadece markaları çekmemiz yetiyor, modeller zaten markaların içinde gizli!
        var markalar = await _apiService.MarkalariGetirAsync();


        if (_orijinalHizmetler != null)
        {
            HizmetListesi.ItemsSource = _orijinalHizmetler;

            // YENİ: Sayfa açıldığında mevcut hizmeti butona yazdır
            var mevcutHizmet = _orijinalHizmetler.FirstOrDefault(h => h.id == _talep.hizmet_id);
            if (mevcutHizmet != null)
            {
                _secilenHizmet = mevcutHizmet;
                SecilenHizmetButonu.Text = mevcutHizmet.ad;
                SecilenHizmetButonu.TextColor = Color.FromArgb("#111111");
            }
        }
        /*
        if (hizmetler != null)
        {
            HizmetPicker.ItemsSource = hizmetler;
            HizmetPicker.SelectedItem = hizmetler.FirstOrDefault(h => h.id == _talep.hizmet_id);
        }
        */

        if (araclar != null)
        {
            var pickerAracListesi = araclar.Select(a => {
                string gosterimAd = "";

                // Senaryo A: Standart veritabanı kayıtları
                if (a.marka_id != null && a.model_id != null && markalar != null)
                {
                    var marka = markalar.FirstOrDefault(m => m.id == a.marka_id);
                    if (marka != null)
                    {
                        // Modeli API'den değil, direkt bulduğumuz markanın kendi listesinden çekiyoruz!
                        var model = marka.modeller?.FirstOrDefault(m => m.id == a.model_id);

                        if (model != null)
                        {
                            gosterimAd = $"{marka.ad} {model.ad}";
                        }
                    }
                }

                // Senaryo B: Müşteri standart dışı (özel) araç girmişse
                if (string.IsNullOrWhiteSpace(gosterimAd) && !string.IsNullOrWhiteSpace(a.ozel_marka))
                {
                    gosterimAd = $"{a.ozel_marka} {a.ozel_model}";
                }

                // Senaryo C: Son Çare (Sadece marka/model sistemden fiziksel olarak silindiyse çalışır)
                if (string.IsNullOrWhiteSpace(gosterimAd))
                {
                    gosterimAd = "Araç ID: " + a.id;
                }

                // YENİ: XAML'deki Binding isimlerine (marka_model_yazi ve yil) uygun isimlendirildi
                return new { Id = a.id, marka_model_yazi = gosterimAd, yil = a.yil };
            }).ToList();

            // YENİ: Picker yerine CollectionView'a bağlıyoruz
            AracListesi.ItemsSource = pickerAracListesi;

            // YENİ: Sayfa açıldığında mevcut aracı butona yazdır
            var seciliArac = pickerAracListesi.FirstOrDefault(a => a.Id == _talep.arac_id);
            if (seciliArac != null)
            {
                _secilenArac = seciliArac;
                SecilenAracButonu.Text = seciliArac.marka_model_yazi;
                SecilenAracButonu.TextColor = Color.FromArgb("#111111");
            }
        }

        if (DateTime.TryParse(_talep.talep_tarihi, out DateTime parsedDate))
        {
            TarihPicker.Date = parsedDate;
        }
        AdresEditor.Text = _talep.adres;
        NotlarEditor.Text = _talep.notlar;
    }

    private async void OnKaydetClicked(object sender, EventArgs e)
    {
        bool basarili = false;

        if (_talep.durum == "Bekliyor")
        {
            // Eski Kod: var secilenHizmet = HizmetPicker.SelectedItem as Hizmet;
            // YENİ KOD:
            if (_secilenHizmet == null)
            {
                await DisplayAlert("Hata", "Lütfen bir hizmet seçin.", "Tamam");
                return;
            }

            // YENİ: Picker yerine yeni değişkenden kontrol
            if (_secilenArac == null)
            {
                await DisplayAlert("Hata", "Lütfen Araç seçimini yapın.", "Tamam");
                return;
            }

            basarili = await _apiService.ServisTalebiGuncelleAsync(
                _talep.id,
                _secilenHizmet.id,
                _secilenArac.Id, // Dynamic objeden Id'yi alıyoruz
                TarihPicker.Date.ToString("yyyy-MM-dd"),
                AdresEditor.Text,
                NotlarEditor.Text,
                false,
                ""
            );
        }
        else if (_talep.durum == "Onaylandı" || _talep.durum == "İşlemde")
        {
            if (string.IsNullOrWhiteSpace(DuzeltmeNotuEditor.Text))
            {
                await DisplayAlert("Hata", "Lütfen düzeltmek istediğiniz alanları yazın.", "Tamam");
                return;
            }

            basarili = await _apiService.ServisTalebiGuncelleAsync(
                _talep.id, null, null, "", "", "",
                true,
                DuzeltmeNotuEditor.Text
            );
        }

        if (basarili)
        {
            await DisplayAlert("Başarılı", "İşleminiz başarıyla kaydedildi.", "Tamam");
            await Navigation.PopAsync(); // Bir önceki sayfaya dön
        }
        else
        {
            await DisplayAlert("Hata", "Bir sorun oluştu, lütfen tekrar deneyin.", "Tamam");
        }
    }

    // --- HİZMET SEÇİMİ METOTLARI ---
    private void OnHizmetSecimButonuClicked(object sender, EventArgs e)
    {
        HizmetAramaKutusu.IsVisible = !HizmetAramaKutusu.IsVisible;

        if (HizmetAramaKutusu.IsVisible)
        {
            AracAramaKutusu.IsVisible = false; // DİĞERİNİ ZORLA KAPAT
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

            SecilenHizmetButonu.Text = secilen.ad;
            SecilenHizmetButonu.TextColor = Color.FromArgb("#111111");

            HizmetAramaKutusu.IsVisible = false; // Seçilince kapat
            HizmetListesi.SelectedItem = null; // Seçimi sıfırla
        }
    }

    // --- ARAÇ SEÇİMİ METOTLARI (YENİ) ---
    private void OnAracSecimButonuClicked(object sender, EventArgs e)
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

            SecilenAracButonu.Text = ar.marka_model_yazi;
            SecilenAracButonu.TextColor = Color.FromArgb("#111111");

            AracAramaKutusu.IsVisible = false; // Seçilince kapat
            AracListesi.SelectedItem = null; // Seçimi sıfırla
        }
    }
}