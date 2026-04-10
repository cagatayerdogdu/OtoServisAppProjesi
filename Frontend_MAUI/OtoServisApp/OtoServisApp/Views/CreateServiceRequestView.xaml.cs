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
    private int MaksimumFotoSayisi = 3;
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
        if (_secilenArac == null || _secilenHizmet == null || string.IsNullOrEmpty(AddressEditor.Text))
        {
            await DisplayAlert("Uyarı", "Lütfen araç, hizmet ve adres alanlarını eksiksiz doldurun.", "Tamam");
            return;
        }

        SubmitButton.IsEnabled = false;
        SubmitButton.Text = "GÖNDERİLİYOR...";

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

        string sonuc = await _apiService.ServisTalebiOlusturAsync(yeniTalep);

        //if (sonuc == "OK")
        //{
        //    await DisplayAlert("Başarılı", "Servis talebiniz alınmıştır. En kısa sürede sizinle iletişime geçeceğiz.", "Tamam");
        //    await Navigation.PopAsync();
        //}
        // Eski: if (sonuc == "OK")
        // Yeni: if hasarlı resim eklemeye uygun.
        if (int.TryParse(sonuc, out int olusturulanTalepId))
        {
            // Talebimiz başarılı oluştu, şimdi fotoğrafları yüklüyoruz
            int yuklenemeyen = 0;
            string hataMesajlari = "";

            foreach (var foto in SecilenFotograflar)
            {
                string uploadSonuc = await _apiService.UploadHasarFotografAsync(olusturulanTalepId, foto.FullPath);
                if (uploadSonuc != "OK")
                {
                    yuklenemeyen++;
                    hataMesajlari += $"- {foto.FileName}: {uploadSonuc}\n";
                }
            }

            if (yuklenemeyen > 0)
            {
                await DisplayAlert("Kısmi Başarılı", $"Servis talebiniz oluşturuldu ancak bazı fotoğraflar yüklenemedi:\n{hataMesajlari}\nDaha sonra talebi düzenle (Taleplerim/Durum Takibi) ekranından tekrar yüklemeyi deneyebilirsiniz.", "Anladım");
            }
            else
            {
                await DisplayAlert("Başarılı", "Servis talebiniz ve fotoğraflarınız başarıyla alınmıştır. En kısa sürede sizinle iletişime geçeceğiz.", "Tamam");
            }

            await Navigation.PopAsync();
        }
        else
        {
            SubmitButton.IsEnabled = true;
            SubmitButton.Text = "TALEBİ OLUŞTUR";

            // YENİ REVİZE: Backend'den bizim özel sorumuz gelirse devreye girer
            if (sonuc.Contains("yeni bir talep açmak ister misiniz?"))
            {
                bool cevap = await DisplayAlert("Mevcut Talep Uyarısı", sonuc, "Evet", "Vazgeç");
                if (cevap)
                {
                    // Kullanıcı evet derse, seçtiği aktif hizmeti listeden çıkarıyoruz (artık seçilemez)
                    var silinecekHizmet = _orijinalHizmetler.FirstOrDefault(h => h.id == _secilenHizmet.id);
                    if (silinecekHizmet != null)
                    {
                        _orijinalHizmetler.Remove(silinecekHizmet);
                    }

                    // Listeyi arayüzde yenile
                    HizmetListesi.ItemsSource = null;
                    HizmetListesi.ItemsSource = _orijinalHizmetler;

                    // Seçilen hizmeti temizle
                    _secilenHizmet = null;
                    SecilenHizmetButonu.Text = "Hizmet Seçiniz";
                    SecilenHizmetButonu.TextColor = Color.FromArgb("#888888");

                    // Hizmet listesi kutusunu otomatik aç ki yeni hizmet seçebilsin
                    HizmetAramaKutusu.IsVisible = true;
                    HizmetAramaBar.Focus();
                }
            }
            else
            {
                await DisplayAlert("Hata Oluştu", sonuc, "Tamam");
                SubmitButton.IsEnabled = true;
                SubmitButton.Text = "TALEBİ OLUŞTUR";

            }

        }
    }

    // Hasarlı Araç Resimleri Ekleme Fonksiyonu
    private async void OnAddPhotoClicked(object sender, EventArgs e)
    {
        if (SecilenFotograflar.Count >= MaksimumFotoSayisi)
        {
            await DisplayAlert("Bilgi", $"En fazla {MaksimumFotoSayisi} adet fotoğraf ekleyebilirsiniz.", "Tamam");
            return;
        }

        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo != null)
            {
                SecilenFotograflar.Add(photo);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Fotoğraf seçilirken bir hata oluştu: " + ex.Message, "Tamam");
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