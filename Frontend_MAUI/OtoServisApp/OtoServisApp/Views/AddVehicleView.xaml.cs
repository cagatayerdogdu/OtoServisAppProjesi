using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AddVehicleView : ContentPage
{
    private readonly ApiService _apiService;
    private Kullanici _aktifKullanici;

    // Hafıza Değişkenleri
    private List<Marka> _tumMarkalar;
    private List<AracModel> _aktifModeller;
    private List<string> _tumYakitTipleri = new List<string> { "Benzin", "Dizel", "Hibrit", "Elektrik" };

    // Seçim Değişkenleri
    private Marka _secilenMarka;
    private AracModel _secilenModel;
    private string _secilenYakit;

    public AddVehicleView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. AŞAMA: Kullanıcıya donma hissi vermemek için Loading ekranını anında aç
        LoadingOverlay.IsVisible = true;

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek ve Loading animasyonunu başlatması için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (20ms) bekleyip thread'i rahatlatıyoruz.
        await Task.Delay(1);

        try
        {
            // 1. Markaları Yükle
            _tumMarkalar = await _apiService.MarkalariGetirAsync();
            if (_tumMarkalar != null)
            {
                MarkaListesi.ItemsSource = _tumMarkalar;
            }

            // 2. Yakıt Tiplerini Yükle (Sabit Liste)
            YakitListesi.ItemsSource = _tumYakitTipleri;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Listeler yüklenirken bir sorun oluştu.", "Tamam");
            System.Diagnostics.Debug.WriteLine($"Yükleme Hatası: {ex.Message}");
        }
        finally
        {
            // 4. AŞAMA: Veri gelse de, hata da verse Loading ekranını KESİNLİKLE kapat
            LoadingOverlay.IsVisible = false;
        }
    }

    // --- MARKA SEÇİMİ ---
    private void OnMarkaSecimButonuClicked(object sender, EventArgs e)
    {
        MarkaAramaKutusu.IsVisible = !MarkaAramaKutusu.IsVisible;
        if (MarkaAramaKutusu.IsVisible)
        {
            ModelAramaKutusu.IsVisible = false; // Diğerlerini kapat
            YakitSecimKutusu.IsVisible = false;
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

    private void OnMarkaSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as Marka;
        if (secilen != null)
        {
            _secilenMarka = secilen;
            SecilenMarkaButonu.Text = secilen.ad;
            SecilenMarkaButonu.TextColor = Color.FromArgb("#111111");

            MarkaAramaKutusu.IsVisible = false;
            MarkaListesi.SelectedItem = null;

            // KASKAD YAPI: Marka değişince Modeli sıfırla ve yeni modelleri yükle
            _secilenModel = null;
            SecilenModelButonu.Text = "Lütfen model seçin...";
            SecilenModelButonu.TextColor = Color.FromArgb("#888888");

            if (secilen.modeller != null)
            {
                _aktifModeller = secilen.modeller;
                ModelListesi.ItemsSource = _aktifModeller;
                SecilenModelButonu.IsEnabled = true; // Butonun kilidini aç
            }
        }
    }

    // --- MODEL SEÇİMİ ---
    private void OnModelSecimButonuClicked(object sender, EventArgs e)
    {
        ModelAramaKutusu.IsVisible = !ModelAramaKutusu.IsVisible;
        if (ModelAramaKutusu.IsVisible)
        {
            MarkaAramaKutusu.IsVisible = false; // Diğerlerini kapat
            YakitSecimKutusu.IsVisible = false;
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

    // --- YAKIT SEÇİMİ ---
    private void OnYakitSecimButonuClicked(object sender, EventArgs e)
    {
        YakitSecimKutusu.IsVisible = !YakitSecimKutusu.IsVisible;
        if (YakitSecimKutusu.IsVisible)
        {
            MarkaAramaKutusu.IsVisible = false; // Diğerlerini kapat
            ModelAramaKutusu.IsVisible = false;
        }
    }

    private void OnYakitSecildi(object sender, SelectionChangedEventArgs e)
    {
        var secilen = e.CurrentSelection.FirstOrDefault() as string;
        if (secilen != null)
        {
            _secilenYakit = secilen;
            SecilenYakitButonu.Text = secilen;
            SecilenYakitButonu.TextColor = Color.FromArgb("#111111");

            YakitSecimKutusu.IsVisible = false;
            YakitListesi.SelectedItem = null;
        }
    }

    // --- KAYDETME ---
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // MADDE 83: Misafir kullanıcı kontrolü (ID = 0 ise engelle ve yönlendir)
        if (_aktifKullanici.id == 0)
        {
            bool? cevap = await ModernAlertService.ShowAsync("Üyelik Gerekli",
                "Misafir kullanıcı olarak araç kaydedemezsiniz. Avantajlardan yararlanmak ve aracınızı takip edebilmek için lütfen üye olun veya giriş yapın.",
                "EvetIptal");

            if (cevap == true)   // Evet'e tıklanmışsa
            {
                // Kullanıcıyı en başa, yani Login (Giriş) ekranına fırlatıyoruz
                await Navigation.PopToRootAsync();
            }
            return; // İşlemi burada kesiyoruz, API'ye gitmiyoruz.
        }

        if (_secilenMarka == null || _secilenModel == null || _secilenYakit == null ||
            string.IsNullOrEmpty(YearEntry.Text) || string.IsNullOrEmpty(KmEntry.Text))
        {
            //await DisplayAlert("Uyarı", "Lütfen tüm bilgileri eksiksiz doldurun.", "Tamam");
            await ModernAlertService.ShowInfoAsync("Uyarı!", "Lütfen tüm bilgileri eksiksiz doldurun.");
            return;
        }

        SaveButton.IsEnabled = false;
        SaveButton.Text = "KAYDEDİLİYOR...";

        var yeniArac = new Arac
        {
            sahip_id = _aktifKullanici.id,
            marka_id = _secilenMarka.id,
            model_id = _secilenModel.id,
            yil = int.Parse(YearEntry.Text),
            yakit_tipi = _secilenYakit,
            kilometre = int.Parse(KmEntry.Text)
        };

        var eklenenArac = await _apiService.AracEkleAsync(yeniArac);

        try
        {
            if (eklenenArac != null)
            {
                // Kullanıcının lokal listesine de ekliyoruz
                _aktifKullanici.araclar.Add(eklenenArac);

                //await DisplayAlert("Başarılı", "Aracınız başarıyla eklendi.", "Tamam");
                //ModernUyariGoster("Aracınız başarıyla kaydedildi.");
                await ModernAlertService.ShowInfoAsync("Aracınız başarıyla kaydedildi.", "Başarılı");
                await Navigation.PopAsync();
            }
            else
            {
                //await DisplayAlert("Hata", "Araç eklenirken bir sorun oluştu.", "Tamam");
                //ModernUyariGoster("Hata");
                await ModernAlertService.ShowInfoAsync("Hata!", "Araç eklenirken bir sorun oluştu.");
                SaveButton.IsEnabled = true;
                SaveButton.Text = "ARACI KAYDET";
            }
        }
        catch (Exception ex)
        {
            //ModernUyariGoster("Araç eklenirken bir sorun oluştu. " + ex.ToString()); await ModernAlertService.ShowInfoAsync("Araç eklenirken bir sorun oluştu. " + ex.ToString(), "Hata");
            SaveButton.IsEnabled = true;      
            SaveButton.Text = "ARACI KAYDET";
    }

    }
}