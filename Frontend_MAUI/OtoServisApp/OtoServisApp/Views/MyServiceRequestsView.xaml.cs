using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class MyServiceRequestsView : ContentPage
{
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<Marka> _tumMarkalar;
    private List<ServisTalebi> _orijinalTalepler;

    // Filtre Değişkenleri
    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde", "Tamamlandı", "İptal Edildi" };
    private string _secilenDurum = "Tümü";

    public MyServiceRequestsView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
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
        _tumHizmetler = await _apiService.HizmetleriGetirAsync();
        _tumMarkalar = await _apiService.MarkalariGetirAsync();

        _orijinalTalepler = await _apiService.ServisTalepleriniGetirAsync(_aktifKullanici.id);

        if (_orijinalTalepler != null)
        {
            foreach (var talep in _orijinalTalepler)
            {
                var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
                if (hizmet != null) talep.hizmet_adi = hizmet.ad;
                // Aracın Aktifler (A) listesinde olup olmadığına bak
                var aracAktif = _aktifKullanici.araclar?.FirstOrDefault(a => a.id == talep.arac_id);

                // Eğer araç listede yoksa (Yani Soft Delete 'X' yapılmışsa) API'den geçmiş kaydını bul!
                if (aracAktif == null)
                {
                    aracAktif = await _apiService.AracGetirAsync(talep.arac_id);
                }

                var arac = await _apiService.AracGetirAsync(talep.arac_id);
                if (arac != null)
                {
                    string gosterimAd = "";
                    if (arac.marka_id != null && arac.model_id != null && _tumMarkalar != null)
                    {
                        var marka = _tumMarkalar.FirstOrDefault(m => m.id == arac.marka_id);
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
                    talep.arac_adi = string.IsNullOrWhiteSpace(gosterimAd) ? $"Araç ID: {arac.id}" : gosterimAd;
                }
            }
            FiltreleriUygula();
        }
    }

    private void OnFiltreAcKapat(object sender, EventArgs e)
    {
        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
    }

    // Arama barı tetikleyicisi
    private void OnFiltreDegisti(object sender, TextChangedEventArgs e)
    {
        FiltreleriUygula();
    }

    private void OnFiltreSecildi(object sender, SelectionChangedEventArgs e)
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

    private void FiltreleriUygula()
    {
        if (_orijinalTalepler == null) return;

        var filtrelenmisListe = _orijinalTalepler.AsEnumerable();

        if (_secilenDurum != "Tümü")
        {
            filtrelenmisListe = filtrelenmisListe.Where(t => t.durum == _secilenDurum);
        }

        // YENİ EKLENEN ARAMA KONTROLÜ
        if (!string.IsNullOrWhiteSpace(AramaBar.Text))
        {
            var kelime = AramaBar.Text.ToLower();
            filtrelenmisListe = filtrelenmisListe.Where(t =>
                (t.hizmet_adi != null && t.hizmet_adi.ToLower().Contains(kelime)) ||
                (t.arac_adi != null && t.arac_adi.ToLower().Contains(kelime))
            );
        }

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
            .ThenByDescending(t => t.id);

        // YENİ KURAL (Madde 16): Talepleri ID'ye (veya tarihe) göre en yeniden en eskiye sırala
        RequestsList.ItemsSource = null;
        RequestsList.ItemsSource = filtrelenmisListe.ToList();
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            if (secilenTalep.durum == "Tamamlandı" || secilenTalep.durum == "İptal Edildi")
            {
                await DisplayAlert("İşlem Engellendi", "Bu talep sonlandığı için üzerinde değişiklik yapılamaz.", "Tamam");
                return;
            }
            await Navigation.PushAsync(new EditServiceRequestView(secilenTalep, _aktifKullanici));
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            // YENİ KURAL: Sadece Bekliyor olanlar iptal edilebilir										
            if (secilenTalep.durum != "Bekliyor")
            {
                await DisplayAlert("İşlem Engellendi", "Sadece 'Bekliyor' durumundaki talepler iptal edilebilir.", "Tamam");
                return;
            }

            bool eminMisin = await DisplayAlert("Onay", "Bu servis talebini iptal etmek (silmek) istediğinize emin misiniz?", "Evet, İptal Et", "Vazgeç");

            if (eminMisin)
            {
                bool basarili = await _apiService.ServisTalebiSilAsync(secilenTalep.id);
                if (basarili)
                {
                    await DisplayAlert("Başarılı", "Talebiniz iptal edildi.", "Tamam");
                    await VerileriYukle();
                }
                else
                {
                    await DisplayAlert("Hata", "Talebiniz iptal edilirken bir sorun oluştu.", "Tamam");
                }
            }
        }
    }
}