using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class AdminRequestsView : ContentPage
{
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<ServisTalebi> _orijinalTalepler;

    private List<string> _durumFiltreleri = new List<string> { "Tümü", "Bekliyor", "Onaylandı", "İşlemde", "Tamamlandı", "İptal Edildi" };
    private string _secilenDurum = "Tümü";

    private Border _acikKartKutusu = null;

    public AdminRequestsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DurumListesi.ItemsSource = _durumFiltreleri;
        await VerileriYukle();
    }

    private async Task VerileriYukle()
    {
        _tumHizmetler = await _apiService.HizmetleriGetirAsync();
        _orijinalTalepler = await _apiService.AdminAktifTalepleriGetirAsync();
        var markalar = await _apiService.MarkalariGetirAsync();

        if (_orijinalTalepler != null)
        {
            foreach (var talep in _orijinalTalepler)
            {
                if (_tumHizmetler != null)
                {
                    var h = _tumHizmetler.FirstOrDefault(x => x.id == talep.hizmet_id);
                    if (h != null) talep.hizmet_adi = h.ad;
                }

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
            }
        }

        FiltreleriUygula();
    }

    private void OnDurumSecimButonuClicked(object sender, EventArgs e)
    {
        if (_acikKartKutusu != null)
        {
            _acikKartKutusu.IsVisible = false;
            _acikKartKutusu = null;
        }

        DurumSecimKutusu.IsVisible = !DurumSecimKutusu.IsVisible;
    }

    private void OnDurumSecildi(object sender, SelectionChangedEventArgs e)
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

    private void OnFiltreDegisti(object sender, EventArgs e)
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

    private void OnItemDurumKutusuAc(object sender, EventArgs e)
    {
        DurumSecimKutusu.IsVisible = false;

        var btn = sender as Button;
        var layout = btn?.Parent as VerticalStackLayout;
        if (layout != null && layout.Children.Count > 2)
        {
            var kutu = layout.Children[2] as Border;
            if (kutu != null)
            {
                if (_acikKartKutusu != null && _acikKartKutusu != kutu)
                {
                    _acikKartKutusu.IsVisible = false;
                }

                kutu.IsVisible = !kutu.IsVisible;
                _acikKartKutusu = kutu.IsVisible ? kutu : null;
            }
        }
    }

    private void OnItemDurumSecildi(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn != null)
        {
            var yeniDurum = btn.Text;
            var talep = btn.BindingContext as ServisTalebi;
            if (talep != null)
            {
                talep.durum = yeniDurum;
            }

            var innerStack = btn.Parent as VerticalStackLayout;
            var kutu = innerStack?.Parent as Border;
            var outerStack = kutu?.Parent as VerticalStackLayout;
            if (outerStack != null && outerStack.Children.Count > 1)
            {
                var anaButon = outerStack.Children[1] as Button;
                if (anaButon != null) anaButon.Text = yeniDurum;
            }

            if (kutu != null)
            {
                kutu.IsVisible = false;
                if (_acikKartKutusu == kutu) _acikKartKutusu = null;
            }
        }
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var talep = button?.CommandParameter as ServisTalebi;

        if (talep != null)
        {
            bool basarili = await _apiService.AdminTalepGuncelleAsync(talep.id, talep.durum, talep.tahmini_tutar);

            if (basarili)
            {
                await DisplayAlert("Başarılı", "Talep başarıyla güncellendi.", "Tamam");
                await VerileriYukle();
            }
            else
            {
                await DisplayAlert("Hata", "Güncellenirken bir sorun oluştu, lütfen tekrar deneyin.", "Tamam");
            }
        }
    }
}