using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class VehiclesView : ContentPage
{
    private Kullanici _aktifKullanici;
    private readonly ApiService _apiService;

    public VehiclesView(Kullanici kullanici)
    {
        InitializeComponent();
        _aktifKullanici = kullanici;
        _apiService = new ApiService();
    }
    /*
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Önce araçları listeye ver
        VehiclesList.ItemsSource = null;
        VehiclesList.ItemsSource = _aktifKullanici.araclar;

        // Arka planda markaları çek
        var markalar = await _apiService.MarkalariGetirAsync();

        if (markalar != null && markalar.Count > 0)
        {
            // Kullanıcının her aracı için marka ve modeli eşleştir
            foreach (var arac in _aktifKullanici.araclar)
            {
                if (arac.marka_id.HasValue && arac.model_id.HasValue)
                {
                    var marka = markalar.FirstOrDefault(m => m.id == arac.marka_id.Value);
                    var model = marka?.modeller?.FirstOrDefault(md => md.id == arac.model_id.Value);

                    if (marka != null && model != null)
                        arac.marka_model_yazi = $"{marka.ad} {model.ad}";
                }
                else
                {
                    arac.marka_model_yazi = $"{arac.ozel_marka} {arac.ozel_model}";
                }
            }

            // İsimler güncellendikten sonra listeyi tetikle
            // İsimler güncellendikten sonra listeyi YENİ bir liste gibi gösterip MAUI'yi çizmeye zorluyoruz
            VehiclesList.ItemsSource = null;
            VehiclesList.ItemsSource = _aktifKullanici.araclar.ToList();
        }
    }
    */
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (100ms) bekleyip thread'i rahatlatıyoruz.
        // await Task.Delay(20);

        // Yükleme işlemini bu rahatlamadan sonra tetikliyoruz.

        // Artık kod kalabalığı yok, sadece yükleme metodunu çağırıyoruz
        await VerileriYukle();
    }

    // Her yerde çağırabileceğimiz eşleştirme (Mapping) metodumuz
    private async Task VerileriYukle()
    {
        // Önce araçları listeye ver
        VehiclesList.ItemsSource = null;
        VehiclesList.ItemsSource = _aktifKullanici.araclar;

        // Arka planda markaları çek
        var markalar = await _apiService.MarkalariGetirAsync();

        if (markalar != null && markalar.Count > 0)
        {
            // Kullanıcının her aracı için marka ve modeli eşleştir
            foreach (var arac in _aktifKullanici.araclar)
            {
                if (arac.marka_id.HasValue && arac.model_id.HasValue)
                {
                    var marka = markalar.FirstOrDefault(m => m.id == arac.marka_id.Value);
                    var model = marka?.modeller?.FirstOrDefault(md => md.id == arac.model_id.Value);

                    if (marka != null && model != null)
                        arac.marka_model_yazi = $"{marka.ad} {model.ad}";
                }
                else
                {
                    arac.marka_model_yazi = $"{arac.ozel_marka} {arac.ozel_model}";
                }
            }

            // İsimler güncellendikten sonra listeyi YENİ bir liste gibi gösterip MAUI'yi çizmeye zorluyoruz
            VehiclesList.ItemsSource = null;
            VehiclesList.ItemsSource = _aktifKullanici.araclar.OrderByDescending(a => a.id).ToList();
        }
    }

    private async void OnAddVehicleClicked(object sender, EventArgs e)
    {
        // await DisplayAlert("Bilgi", "Araç Ekleme ekranına geçilecek.", "Tamam");
        // YENİ SAYFAYA GEÇİŞ
        await Navigation.PushAsync(new AddVehicleView(_aktifKullanici));
    }

    private async void OnEditVehicleClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenArac = buton?.CommandParameter as Arac;

        if (secilenArac != null)
        {
            // Düzenleme sayfasına yönlendirip seçilen aracı da beraberinde gönderiyoruz
            await Navigation.PushAsync(new EditVehicleView(_aktifKullanici, secilenArac));
        }
    }

    private async void OnDeleteVehicleClicked(object sender, EventArgs e)
    {
        var buton = sender as Button;
        var secilenArac = buton?.CommandParameter as Arac;

        if (secilenArac != null)
        {
            // 1. OTO-KONTROL: Bu araca ait servis talebi var mı diye API'den güncel talepleri çek
            var talepler = await _apiService.ServisTalepleriniGetirAsync(_aktifKullanici.id);
            var aracaAitTalepler = talepler?.Where(t => t.arac_id == secilenArac.id).ToList() ?? new List<ServisTalebi>();

            // KURAL 1: "Bekliyor" durumunda talep varsa SİLDİRME!
            if (aracaAitTalepler.Any(t => t.durum == "Bekliyor"))
            {
                await DisplayAlert("İşlem Engellendi", "Bu araca ait 'Bekliyor' durumunda bir servis talebiniz bulunmaktadır. Aracı silebilmek için lütfen önce ilgili servis talebini iptal ediniz.", "Tamam");
                return;
            }

            // KURAL 2: Bekliyor dışında (Geçmiş) talebi varsa ŞIK BİR UYARI VER!
            if (aracaAitTalepler.Any())
            {
                bool devam = await DisplayAlert("Uyarı", "Silmek istediğiniz araca ait geçmiş servis talepleri bulunmaktadır. Tanımlı aracınızı yine de silmek istiyor musunuz?\n\n(Servis Talepleriniz etkilenmeyecektir.)", "Evet, Sil", "Vazgeç");
                if (!devam) return;
            }
            else
            {
                // Hiç talebi yoksa normal standart uyarıyı ver
                bool eminMisin = await DisplayAlert("Onay", $"{secilenArac.marka_model_yazi} aracınızı silmek istediğinize emin misiniz?", "Evet, Sil", "Vazgeç");
                if (!eminMisin) return;
            }

            // KURAL 3: Tüm testleri geçtiyse aracı Pasife (X) çek
            bool basarili = await _apiService.AracSilAsync(secilenArac.id);
            if (basarili)
            {
                await DisplayAlert("Başarılı", "Araç başarıyla silindi.", "Tamam");
                // Kullanıcının YENİ araç listesini (silinmiş olan hariç) API'den çek
                _aktifKullanici.araclar = await _apiService.KullaniciAraclariniGetirAsync(_aktifKullanici.id);
                // Marka ve modelleri isimlerle eşleştirip ekrana bas
                await VerileriYukle();
            }
        }
    }
}