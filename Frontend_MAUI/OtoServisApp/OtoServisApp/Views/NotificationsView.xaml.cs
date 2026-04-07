using OtoServisApp.Models;
using OtoServisApp.Services;

namespace OtoServisApp.Views;

public partial class NotificationsView : ContentPage
{
    private readonly ApiService _apiService;

    public NotificationsView()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // YENİ REVİZE: Arayüzün (UI) donmasını ve uygulamanın çökmesini engellemek için 
        // veri çekme işlemine geçmeden önce çok kısa bir süre (100ms) bekleyip thread'i rahatlatıyoruz.
        // await Task.Delay(20);

        // Yükleme işlemini bu rahatlamadan sonra tetikliyoruz.
        await BildirimleriYukle();
    }

    private async Task BildirimleriYukle()
    {
        // NOT: Kendi sistemindeki Kullanıcı ID'yi buraya çek (Örn: Preferences.Get("kullanici_id", 0))
        int aktifKullaniciId = 1;

        var bildirimler = await _apiService.KullaniciBildirimleriniGetirAsync(aktifKullaniciId);
        // UI'ın (Arayüzün) silindikten sonra tazelenmesini (Refresh) zorluyoruz
        NotificationList.ItemsSource = null;
        NotificationList.ItemsSource = bildirimler;
    }

    private async void OnNotificationTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var bildirim = border?.BindingContext as BildirimResponse;

        if (bildirim != null && !bildirim.okundu_mu)
        {
            bool basarili = await _apiService.BildirimOkunduIsaretleAsync(bildirim.id);
            if (basarili)
            {
                bildirim.okundu_mu = true;
                await BildirimleriYukle();
            }
        }
    }

    // Sayfanın en üstüne (sınıfın içine) bu bayrağı ekle
    private bool _isBatchSelecting = false;

    private void OnSelectAllCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _isBatchSelecting = true; // Döngüyü kilitliyoruz ki UI donmasın

        if (e.Value)
        {
            var allItems = NotificationList.ItemsSource as IEnumerable<BildirimResponse>;
            if (allItems != null)
            {
                NotificationList.SelectedItems.Clear();
                foreach (var item in allItems)
                {
                    NotificationList.SelectedItems.Add(item);
                }
            }
        }
        else
        {
            NotificationList.SelectedItems.Clear();
        }

        _isBatchSelecting = false; // Döngü bitti, kilidi açtık
        BtnDeleteSelected.IsVisible = NotificationList.SelectedItems.Count > 0;
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBatchSelecting) return; // Toplu seçim yapılıyorsa burayı atla (ÇÖKMEYİ ENGELLEYEN KOD)

        int seciliSayisi = NotificationList.SelectedItems.Count;
        BtnDeleteSelected.IsVisible = seciliSayisi > 0;

        // Eğer manuel olarak tüm tikleri kaldırdıysa, Hepsini Seç kutusunun da tikini kaldır
        if (seciliSayisi == 0 && ChkSelectAll.IsChecked)
        {
            ChkSelectAll.IsChecked = false;
        }

        var sonSecilen = e.CurrentSelection.FirstOrDefault() as BildirimResponse;
        if (sonSecilen != null && !sonSecilen.okundu_mu)
        {
            bool basarili = await _apiService.BildirimOkunduIsaretleAsync(sonSecilen.id);
            if (basarili)
            {
                sonSecilen.okundu_mu = true;
            }
        }
    }

    private async void OnDeleteSelectedClicked(object sender, EventArgs e)
    {
        var secilenler = NotificationList.SelectedItems.Cast<BildirimResponse>().ToList(); // Modelin adı neyse ona göre cast et
        if (!secilenler.Any()) return;

        bool onay = await DisplayAlert("Onay", $"{secilenler.Count} adet bildirimi silmek istiyor musunuz?", "Evet", "İptal");
        if (!onay) return;

        foreach (var bildirim in secilenler)
        {
            await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
            // ObservableCollection listenden çıkar: BildirimListesi.Remove(bildirim);
        }

        NotificationList.SelectedItems.Clear();
        ChkSelectAll.IsChecked = false;
        await BildirimleriYukle();
        await DisplayAlert("Bilgi", "Seçilen bildirimler silindi.", "Tamam");
    }

    private async void OnSingleDeleteInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItemView;
        // var bildirim = swipeItem?.CommandParameter as BildirimResponse;
        // CommandParameter yerine güvenli olan BindingContext'i kullanıyoruz
        var bildirim = swipeItem?.BindingContext as BildirimResponse;

        if (bildirim != null)
        {
            bool onay = await DisplayAlert("Onay", "Bu bildirimi silmek istiyor musunuz?", "Evet", "Vazgeç");
            if (onay)
            {
                await _apiService.NotificationsDeleteAsync($"bildirimler/{bildirim.id}");
                await BildirimleriYukle(); // Listeyi güncelle
            }
        }
    }

}