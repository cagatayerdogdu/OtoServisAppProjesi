using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class MyServiceRequestsView : ContentPage
{
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;
    private List<Hizmet> _tumHizmetler;
    private List<Marka> _tumMarkalar;

    public MyServiceRequestsView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await VerileriYukle();
    }

    private async Task VerileriYukle()
    {
        _tumHizmetler = await _apiService.HizmetleriGetirAsync();
        _tumMarkalar = await _apiService.MarkalariGetirAsync(); // Araç isimleri için markaları çekiyoruz

        var talepler = await _apiService.ServisTalepleriniGetirAsync(_aktifKullanici.id);

        if (talepler != null)
        {
            foreach (var talep in talepler)
            {
                var hizmet = _tumHizmetler?.FirstOrDefault(h => h.id == talep.hizmet_id);
                if (hizmet != null) talep.hizmet_adi = hizmet.ad;

                // Aracın Aktifler (A) listesinde olup olmadığına bak
                var arac = _aktifKullanici.araclar?.FirstOrDefault(a => a.id == talep.arac_id);

                // Eğer araç listede yoksa (Yani Soft Delete 'X' yapılmışsa) API'den geçmiş kaydını bul!
                if (arac == null)
                {
                    arac = await _apiService.AracGetirAsync(talep.arac_id);
                }

                // Şimdi aracı bulduğumuza göre ismini parçalayıp yazalım
                if (arac != null)
                {
                    var marka = _tumMarkalar?.FirstOrDefault(m => m.id == arac.marka_id);
                    var model = marka?.modeller?.FirstOrDefault(md => md.id == arac.model_id);

                    if (marka != null && model != null)
                        talep.arac_adi = $"{marka.ad} {model.ad}";
                    else
                        talep.arac_adi = $"{arac.ozel_marka} {arac.ozel_model}";
                }
                else
                {
                    talep.arac_adi = "Silinmiş Araç";
                }
            }

            // YENİ KURAL (Madde 16): Talepleri ID'ye (veya tarihe) göre en yeniden en eskiye sırala
            RequestsList.ItemsSource = null;
            RequestsList.ItemsSource = talepler.OrderByDescending(t => t.id).ToList();
        }
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenTalep = buton?.CommandParameter as ServisTalebi;

        if (secilenTalep != null)
        {
            // İleride buraya "EditServiceRequestView" sayfasına yönlendirme koyacağız.
            // await DisplayAlert("Bilgi", "Düzenleme ekranı yakında eklenecek.", "Tamam");
            
            // Yeni oluşturduğumuz sayfaya yönlendir ve seçili talebi beraberinde yolla            
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
                    await VerileriYukle(); // Listeyi yenile
                }
                else
                {
                    await DisplayAlert("Hata", "İşlem sırasında bir hata oluştu.", "Tamam");
                }
            }
        }
    }
}