using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class EditVehicleView : ContentPage
{
    private Kullanici _aktifKullanici;
    private Arac _duzenlenenArac;
    private readonly ApiService _apiService;

    // Hafıza Değişkenleri
    private List<Marka> _tumMarkalar;
    private List<AracModel> _aktifModeller;

    // Seçim Değişkenleri
    private Marka _secilenMarka;
    private AracModel _secilenModel;
    private bool _sayfaYukleniyor = true;

    public EditVehicleView(Kullanici kullanici, Arac arac)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _duzenlenenArac = arac;
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await VerileriYukleVeKontrolEt();
    }

    private async Task VerileriYukleVeKontrolEt()
    {
        _sayfaYukleniyor = true;

        // 1. Markaları çek ve listeye doldur
        _tumMarkalar = await _apiService.MarkalariGetirAsync();
        if (_tumMarkalar != null)
        {
            MarkaListesi.ItemsSource = _tumMarkalar;
        }

        // 2. Mevcut bilgileri doldur
        YilEntry.Text = _duzenlenenArac.yil.ToString();
        KmEntry.Text = _duzenlenenArac.kilometre.ToString();

        // 3. Aracın kayıtlı markasını ve modelini bul, butonlara yaz
        if (_duzenlenenArac.marka_id != null && _tumMarkalar != null)
        {
            _secilenMarka = _tumMarkalar.FirstOrDefault(m => m.id == _duzenlenenArac.marka_id);
            if (_secilenMarka != null)
            {
                SecilenMarkaButonu.Text = _secilenMarka.ad;
                SecilenMarkaButonu.TextColor = Color.FromArgb("#111111");

                // Modelleri çek
                _aktifModeller = await _apiService.ModelleriGetirAsync(_secilenMarka.id);
                if (_aktifModeller != null)
                {
                    ModelListesi.ItemsSource = _aktifModeller;
                    SecilenModelButonu.IsEnabled = true; // Model butonunu aktif et

                    _secilenModel = _aktifModeller.FirstOrDefault(m => m.id == _duzenlenenArac.model_id);
                    if (_secilenModel != null)
                    {
                        SecilenModelButonu.Text = _secilenModel.ad;
                        SecilenModelButonu.TextColor = Color.FromArgb("#111111");
                    }
                }
            }
        }

        // 4. MADDE 28 KONTROLÜ: Araç serviste mi?
        bool aktifTalepVar = await _apiService.AracAktifTalepVarMiAsync(_duzenlenenArac.id);
        if (aktifTalepVar)
        {
            UyariKutusu.IsVisible = true;
            // İşlemdeyse butonları devre dışı bırak, tıklanıp liste açılmasın!
            SecilenMarkaButonu.IsEnabled = false;
            SecilenModelButonu.IsEnabled = false;
        }

        _sayfaYukleniyor = false;
    }

    // --- MARKA SEÇİMİ (AKILLI KAPANMA & ARAMA) ---
    private void OnMarkaSecimButonuClicked(object sender, EventArgs e)
    {
        MarkaAramaKutusu.IsVisible = !MarkaAramaKutusu.IsVisible;
        if (MarkaAramaKutusu.IsVisible)
        {
            ModelAramaKutusu.IsVisible = false; // Diğerini kapat
            MarkaAramaBar.Focus();
        }
    }

    private void OnMarkaAramaDegisti(object sender, TextChangedEventArgs e)
    {
        if (_tumMarkalar == null) return;
        var aramaMetni = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(aramaMetni))
            MarkaListesi.ItemsSource = _tumMarkalar;
        else
            MarkaListesi.ItemsSource = _tumMarkalar.Where(m => m.ad != null && m.ad.ToLower().Contains(aramaMetni)).ToList();
    }

    private async void OnMarkaSecildi(object sender, SelectionChangedEventArgs e)
    {
        if (_sayfaYukleniyor) return;

        var secilen = e.CurrentSelection.FirstOrDefault() as Marka;
        if (secilen != null)
        {
            _secilenMarka = secilen;
            SecilenMarkaButonu.Text = secilen.ad;
            SecilenMarkaButonu.TextColor = Color.FromArgb("#111111");

            MarkaAramaKutusu.IsVisible = false;
            MarkaListesi.SelectedItem = null;

            // KASKAD YAPI: Marka değişince Modeli sıfırlıyoruz ve API'den yeni modelleri çekiyoruz
            _secilenModel = null;
            SecilenModelButonu.Text = "Lütfen model seçin...";
            SecilenModelButonu.TextColor = Color.FromArgb("#888888");

            _aktifModeller = await _apiService.ModelleriGetirAsync(secilen.id);
            if (_aktifModeller != null)
            {
                ModelListesi.ItemsSource = _aktifModeller;
                SecilenModelButonu.IsEnabled = true;
            }
        }
    }

    // --- MODEL SEÇİMİ (AKILLI KAPANMA & ARAMA) ---
    private void OnModelSecimButonuClicked(object sender, EventArgs e)
    {
        ModelAramaKutusu.IsVisible = !ModelAramaKutusu.IsVisible;
        if (ModelAramaKutusu.IsVisible)
        {
            MarkaAramaKutusu.IsVisible = false; // Diğerini kapat
            ModelAramaBar.Focus();
        }
    }

    private void OnModelAramaDegisti(object sender, TextChangedEventArgs e)
    {
        if (_aktifModeller == null) return;
        var aramaMetni = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(aramaMetni))
            ModelListesi.ItemsSource = _aktifModeller;
        else
            ModelListesi.ItemsSource = _aktifModeller.Where(m => m.ad != null && m.ad.ToLower().Contains(aramaMetni)).ToList();
    }

    private void OnModelSecildi(object sender, SelectionChangedEventArgs e)
    {
        if (_sayfaYukleniyor) return;

        var secilen = e.CurrentSelection.FirstOrDefault() as AracModel;
        if (secilen != null)
        {
            _secilenModel = secilen;
            SecilenModelButonu.Text = secilen.ad;
            SecilenModelButonu.TextColor = Color.FromArgb("#111111");

            ModelAramaKutusu.IsVisible = false;
            ModelListesi.SelectedItem = null;
        }
    }

    // --- GÜNCELLEME İŞLEMİ ---
    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(KmEntry.Text) || string.IsNullOrEmpty(YilEntry.Text))
        {
            await DisplayAlert("Uyarı", "Yıl ve Kilometre boş bırakılamaz.", "Tamam");
            return;
        }

        // Eğer buton aktifse (yani araç serviste değilse) marka/model boş geçilemez
        if (SecilenMarkaButonu.IsEnabled && (_secilenMarka == null || _secilenModel == null))
        {
            await DisplayAlert("Uyarı", "Lütfen Marka ve Model seçiniz.", "Tamam");
            return;
        }

        UpdateButton.IsEnabled = false;
        UpdateButton.Text = "GÜNCELLENİYOR...";

        _duzenlenenArac.kilometre = Convert.ToInt32(KmEntry.Text);
        _duzenlenenArac.yil = Convert.ToInt32(YilEntry.Text);

        // Araç serviste değilse (buton açıksa), seçili marka modeli API'ye gönderilecek objeye yaz
        if (SecilenMarkaButonu.IsEnabled)
        {
            _duzenlenenArac.marka_id = _secilenMarka?.id;
            _duzenlenenArac.model_id = _secilenModel?.id;
        }

        bool basarili = await _apiService.AracGuncelleAsync(_duzenlenenArac.id, _duzenlenenArac);

        if (basarili)
        {
            await DisplayAlert("Başarılı", "Araç bilgileri güncellendi.", "Tamam");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Hata", "Güncellenirken bir sorun oluştu.", "Tamam");
            UpdateButton.IsEnabled = true;
            UpdateButton.Text = "GÜNCELLE";
        }
    }
}